using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class WebHookService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<WebHookService> _logger;
        private readonly TolkOptions.SmtpSettings _options;
        private readonly ISwedishClock _clock;

        public WebHookService(
            TolkDbContext dbContext,
            ILogger<WebHookService> logger,
            IOptions<TolkOptions> options,
            ISwedishClock clock)
        {
            _dbContext = dbContext;
            _logger = logger;
            _options = options.Value.Smtp;
            _clock = clock;
        }

        public async Task CallWebHooks()
        {
            var callIds = await _dbContext.OutboundWebHookCalls
                .Where(e => e.DeliveredAt == null && e.FailedTries < 5)
                .Select(e => e.OutboundWebHookCallId)
                .ToListAsync();

            _logger.LogDebug("Found {count} outbound web hook calls to send: {callIds}",
                callIds.Count, string.Join(", ", callIds));

            if(callIds.Any())
            {
                //Need app settings: UseCertFile, Cert.FilePath, CertPublicKey

                //var handler = new HttpClientHandler();
                //handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                //handler.SslProtocols = SslProtocols.Tls12;
                //handler.ClientCertificates.Add(new X509Certificate2("cert.crt"));
                using (var client = new HttpClient()) //new HttpClient(handler) 
                {
                    foreach(var callId in callIds)
                    {
                        using (var trn = _dbContext.Database.BeginTransaction(IsolationLevel.Serializable))
                        {
                            try
                            {
                                var call = await _dbContext.OutboundWebHookCalls
                                    .SingleOrDefaultAsync(e => e.OutboundWebHookCallId == callId);

                                if(call == null)
                                {
                                    _logger.LogDebug("Call {callId} was in list to be handled, but seems to have been handled already.", callId);
                                }
                                else
                                {
                                    client.DefaultRequestHeaders.Accept.Clear();
                                    client.DefaultRequestHeaders.Add("X-Kammarkollegiet-InterperterService-Event", call.NotificationType.GetCustomName());
                                    client.DefaultRequestHeaders.Add("X-Kammarkollegiet-InterperterService-Delivery", callId.ToString());
                                    //Also add cert to call
                                    _logger.LogInformation("Calling web hook {recipientUrl} with message {callId}", call.RecipientUrl, callId);
                                    var content = new StringContent(call.Payload, Encoding.UTF8, "application/json");
                                    var response = await client.PostAsync(call.RecipientUrl, content);

                                    if (response.IsSuccessStatusCode)
                                    {
                                        call.DeliveredAt = _clock.SwedenNow;
                                    }
                                    else
                                    {
                                        call.FailedTries++;
                                        _logger.LogWarning("Call {callId} failed with the following status code: {statusCode}", callId, response.StatusCode);
                                    }
                                    //else set failed tries, and possibly write something somewhere
                                    _dbContext.SaveChanges();
                                    trn.Commit();
                                }
                            }
                            catch(Exception ex)
                            {
                                _logger.LogError(ex, "Failure calling web hook {callId}", callId);
                            }
                        }
                    }
                }
            }
        }
    }
}