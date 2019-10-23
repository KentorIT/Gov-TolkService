using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class WebHookService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<WebHookService> _logger;
        private readonly TolkOptions _options;
        private readonly ISwedishClock _clock;
        private readonly INotificationService _notificationService;
        private static readonly HttpClient client = new HttpClient();
        private const int NumberOfTries = 5;

        public WebHookService(
            TolkDbContext dbContext,
            ILogger<WebHookService> logger,
            IOptions<TolkOptions> options,
            ISwedishClock clock,
            INotificationService notificationService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _options = options?.Value;
            _clock = clock;
            _notificationService = notificationService;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Must not stop, any errors must be swollowed")]
        public async Task CallWebHooks()
        {
            var callIds = await _dbContext.OutboundWebHookCalls
                .Where(e => e.DeliveredAt == null && e.FailedTries < NumberOfTries)
                .Select(e => e.OutboundWebHookCallId)
                .ToListAsync();

            _logger.LogInformation("Found {count} outbound web hook calls to send: {callIds}",
                callIds.Count, string.Join(", ", callIds));

            string errorMessage = string.Empty;

            if (callIds.Any())
            {
                foreach (var callId in callIds)
                {
                    bool success = false;
                    var call = await _dbContext.OutboundWebHookCalls
                        .Include(c => c.RecipientUser).ThenInclude(u => u.Claims)
                        .SingleOrDefaultAsync(e => e.OutboundWebHookCallId == callId && e.DeliveredAt == null && e.FailedTries < NumberOfTries);
                    try
                    {
                        if (call == null)
                        {
                            _logger.LogInformation("Call {callId} was in list to be handled, but seems to have been handled already.", callId);
                        }
                        else
                        {
                            client.DefaultRequestHeaders.Clear();
                            client.DefaultRequestHeaders.Add("X-Kammarkollegiet-InterpreterService-Event", call.NotificationType.GetCustomName());
                            client.DefaultRequestHeaders.Add("X-Kammarkollegiet-InterpreterService-Delivery", callId.ToSwedishString());
                            string encryptedCallbackKey = call.RecipientUser?.Claims.SingleOrDefault(c => c.ClaimType == "CallbackApiKey")?.ClaimValue;
                            if (!string.IsNullOrWhiteSpace(encryptedCallbackKey))
                            {
                                client.DefaultRequestHeaders.Add("X-Kammarkollegiet-InterpreterService-ApiKey", EncryptHelper.Decrypt(encryptedCallbackKey, _options.PublicOrigin, call.RecipientUser.UserName));
                            }
                            //Also add cert to call
                            _logger.LogInformation("Calling web hook {recipientUrl} with message {callId}", call.RecipientUrl, callId);
                            using (var content = new StringContent(call.Payload, Encoding.UTF8, "application/json"))
                            {
                                var response = await client.PostAsync(call.RecipientUrl, content);

                                success = response.IsSuccessStatusCode;
                                if (!success)
                                {
                                    _logger.LogWarning("Call {callId} failed with the following status code: {statusCode}, try number {tries}", callId, response.StatusCode, call.FailedTries + 1);
                                    errorMessage = $"Call {callId} failed with the following status code: {response.StatusCode}, try number {call.FailedTries + 1}";
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error on this side when calling web hook {callId}, try number {tries}", callId, call.FailedTries + 1);
                        errorMessage = $"Error on the \"{Constants.SystemName}\" server side for call {callId}. Contact {_options.Support.FirstLineEmail} for more information.";
                    }
                    finally
                    {
                        if (success)
                        {
                            call.DeliveredAt = _clock.SwedenNow;
                        }
                        else
                        {
                            call.FailedTries++;
                            FailedWebHookCall failedCall = new FailedWebHookCall { OutboundWebHookCallId = callId, ErrorMessage = errorMessage, FailedAt = _clock.SwedenNow };
                            _dbContext.FailedWebHookCalls.Add(failedCall);
                            if (call.FailedTries == NumberOfTries && call.NotificationType != Enums.NotificationType.ErrorNotification)
                            {
                                _notificationService.NotifyOnFailure(callId);
                            }
                        }
                        await _dbContext.SaveChangesAsync();
                    }
                }
            }
        }
    }
}