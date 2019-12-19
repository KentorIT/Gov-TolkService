using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class ErrorNotificationService
    {
        private readonly ILogger<ErrorNotificationService> _logger;
        private readonly TolkOptions _options;
        private readonly ISwedishClock _clock;
        private readonly INotificationService _notificationService;
        private readonly TolkDbContext _tolkDbContext;

        public ErrorNotificationService(
            ILogger<ErrorNotificationService> logger,
            IOptions<TolkOptions> options,
            ISwedishClock clock,
            INotificationService notificationService,
            TolkDbContext tolkDbContext
        )
        {
            _logger = logger;
            _options = options?.Value;
            _clock = clock;
            _notificationService = notificationService;
            _tolkDbContext = tolkDbContext;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Must not stop, any errors must be swollowed")]
        public async Task CheckForFailuresToReport()
        {
            var callIds = await _tolkDbContext.OutboundWebHookCalls
            .Where(e => e.DeliveredAt == null && e.HasNotifiedFailure == false)
            .Select(e => e.OutboundWebHookCallId)
            .ToListAsync();

            _logger.LogInformation("Found {count} emails to send: {emailIds}",
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
                            _notificationService.NotifyOnFailure(callId);
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
    }
}
