using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;

namespace Tolk.BusinessLogic.Services
{
    public class ErrorNotificationService
    {
        private readonly ILogger<ErrorNotificationService> _logger;
        private readonly INotificationService _notificationService;
        private readonly TolkDbContext _tolkDbContext;

        public ErrorNotificationService(
            ILogger<ErrorNotificationService> logger,
            INotificationService notificationService,
            TolkDbContext tolkDbContext
        )
        {
            _logger = logger;
            _notificationService = notificationService;
            _tolkDbContext = tolkDbContext;
        }

        public async Task CheckForFailedWebHookCallsToReport()
        {
            var callIds = await _tolkDbContext.OutboundWebHookCalls
            .Where(e => e.DeliveredAt == null && e.HasNotifiedFailure == false)
            .Select(e => e.OutboundWebHookCallId)
            .ToListAsync();

            _logger.LogInformation("Found {count} failed web hooks to report: {callIds}",
                callIds.Count, string.Join(", ", callIds));

            if (callIds.Any())
            {
                foreach (var callId in callIds)
                {
                    try
                    {
                        var call = await _tolkDbContext.OutboundWebHookCalls
                            .SingleOrDefaultAsync(e => e.OutboundWebHookCallId == callId && e.DeliveredAt == null && e.HasNotifiedFailure == false);

                        if (call == null)
                        {
                            _logger.LogInformation("Call {callId} was in list to be notifed as a failure, but now appears to have been handled.", callId);
                        }
                        else
                        {
                            _logger.LogInformation("Notifying failure on {callId} of type {notificationType}", callId, call.NotificationType);
                            await _notificationService.NotifyOnFailedWebHook(callId);
                            call.HasNotifiedFailure = true;
                            await _tolkDbContext.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failure Notifying failure on {callId}", callId);
                    }
                }
            }
        }
        public async Task CheckForFailedPeppolMessagesToReport()
        {
            var messageIds = await _tolkDbContext.OutboundPeppolMessages
            .Where(e => e.DeliveredAt == null && e.HasNotifiedFailure == false)
            .Select(e => e.OutboundPeppolMessageId)
            .ToListAsync();

            _logger.LogInformation("Found {count} peppol messages to send: {messageIds}",
                messageIds.Count, string.Join(", ", messageIds));

            if (messageIds.Any())
            {
                foreach (var messageId in messageIds)
                {
                    try
                    {
                        var message = await _tolkDbContext.OutboundPeppolMessages
                            .SingleOrDefaultAsync(e => e.OutboundPeppolMessageId == messageId && e.DeliveredAt == null && e.HasNotifiedFailure == false);

                        if (message == null)
                        {
                            _logger.LogInformation("Message {messageId} was in list to be notifed as a failure, but now appears to have been handled.", messageId);
                        }
                        else
                        {
                            _logger.LogInformation("Notifying failure on {messageId} of type {notificationType}", messageId, message.NotificationType);
                            await _notificationService.NotifyOnFailedPeppolMessage(messageId);
                            message.HasNotifiedFailure = true;
                            await _tolkDbContext.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failure notifying failure on {messageId}", messageId);
                    }
                }
            }
        }
    }
}
