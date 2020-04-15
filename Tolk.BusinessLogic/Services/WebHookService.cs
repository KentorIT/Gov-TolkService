using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class WebHookService
    {
        private readonly ILogger<WebHookService> _logger;
        private readonly TolkOptions _options;
        private readonly ISwedishClock _clock;
        private static readonly HttpClient client = new HttpClient();
        private const int NumberOfTries = 5;

        public WebHookService(
            ILogger<WebHookService> logger,
            IOptions<TolkOptions> options,
            ISwedishClock clock)
        {
            _logger = logger;
            _options = options?.Value;
            _clock = clock;
        }

        public async Task CallWebHooks()
        {
            List<int> callIds = null;
            using (TolkDbContext context = _options.GetContext())
            {
                callIds = await context.OutboundWebHookCalls
                    .Where(e => e.DeliveredAt == null && e.FailedTries < NumberOfTries && !e.IsHandling)
                    .Select(e => e.OutboundWebHookCallId)
                    .ToListAsync();
                if (callIds.Any())
                {
                    var calls = context.OutboundWebHookCalls
                        .Where(e => callIds.Contains(e.OutboundWebHookCallId) && e.IsHandling == false)
                        .Select(c => c);
                    await calls.ForEachAsync(c => c.IsHandling = true);
                    await context.SaveChangesAsync();
                }
            }

            _logger.LogInformation("Found {count} outbound web hook calls to send: {callIds}",
                callIds.Count, string.Join(", ", callIds));

            if (callIds.Any())
            {
                var tasks = new List<Task>();
                foreach (var callId in callIds)
                {
                    tasks.Add(Task.Factory.StartNew(() => CallWebhook(callId), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current));
                }
                await Task.Factory.ContinueWhenAny(tasks.ToArray(), r => { });
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Must not stop, any errors must be swollowed")]
        private async Task CallWebhook(int callId)
        {
            using (TolkDbContext context = _options.GetContext())
            {
#warning include-fest
                var call = await context.OutboundWebHookCalls
               .Include(c => c.RecipientUser).ThenInclude(u => u.Claims)
               .SingleOrDefaultAsync(e => e.OutboundWebHookCallId == callId && e.DeliveredAt == null && e.FailedTries < NumberOfTries);

                bool success = false;
                string errorMessage = string.Empty;
                try
                {
                    if (call == null)
                    {
                        _logger.LogInformation("Call {callId} was in list to be handled, but seems to have been handled already.", call.OutboundWebHookCallId);
                    }
                    else
                    {
                        using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, call.RecipientUrl))
                        {
                            requestMessage.Headers.Add("X-Kammarkollegiet-InterpreterService-Event", call.NotificationType.GetCustomName());
                            requestMessage.Headers.Add("X-Kammarkollegiet-InterpreterService-Delivery", call.OutboundWebHookCallId.ToSwedishString());
                            string encryptedCallbackKey = call.RecipientUser?.Claims.SingleOrDefault(c => c.ClaimType == "CallbackApiKey")?.ClaimValue;
                            if (!string.IsNullOrWhiteSpace(encryptedCallbackKey))
                            {
                                requestMessage.Headers.Add("X-Kammarkollegiet-InterpreterService-ApiKey", EncryptHelper.Decrypt(encryptedCallbackKey, _options.PublicOrigin, call.RecipientUser.UserName));
                            }
                            //Also add cert to call
                            _logger.LogInformation("Calling web hook {recipientUrl} with message {callId}", call.RecipientUrl, call.OutboundWebHookCallId);

                            using (var content = new StringContent(call.Payload, Encoding.UTF8, "application/json"))
                            {
                                requestMessage.Content = content;
                                var response = await client.SendAsync(requestMessage);

                                success = response.IsSuccessStatusCode;
                                if (!success)
                                {
                                    _logger.LogWarning("Call {callId} failed with the following status code: {statusCode}, try number {tries}", call.OutboundWebHookCallId, response.StatusCode, call.FailedTries + 1);
                                    errorMessage = $"Call {call.OutboundWebHookCallId} failed with the following status code: {response.StatusCode}, try number {call.FailedTries + 1}";
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error on this side when calling web hook {callId}, try number {tries}", call.OutboundWebHookCallId, call.FailedTries + 1);
                    errorMessage = $"Error on the \"{Constants.SystemName}\" server side for call {call.OutboundWebHookCallId}. Contact {_options.Support.FirstLineEmail} for more information.";
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
                        FailedWebHookCall failedCall = new FailedWebHookCall { OutboundWebHookCallId = call.OutboundWebHookCallId, ErrorMessage = errorMessage, FailedAt = _clock.SwedenNow };
                        context.FailedWebHookCalls.Add(failedCall);
                        if (call.FailedTries == NumberOfTries && call.NotificationType != Enums.NotificationType.ErrorNotification)
                        {
                            call.HasNotifiedFailure = false;
                        }
                    }
                    call.IsHandling = false;
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}