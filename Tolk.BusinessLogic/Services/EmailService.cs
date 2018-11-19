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
using System.Text;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Helpers;

namespace Tolk.BusinessLogic.Services
{
    public class EmailService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<EmailService> _logger;
        private readonly TolkOptions.SmtpSettings _options;
        private readonly ISwedishClock _clock;

        public EmailService(
            TolkDbContext dbContext,
            ILogger<EmailService> logger,
            IOptions<TolkOptions> options,
            ISwedishClock clock)
        {
            _dbContext = dbContext;
            _logger = logger;
            _options = options.Value.Smtp;
            _clock = clock;
        }

        public async Task SendEmails()
        {
            var emailIds = await _dbContext.OutboundEmails
                .Where(e => e.DeliveredAt == null)
                .Select(e => e.OutboundEmailId)
                .ToListAsync();

            _logger.LogDebug("Found {count} emails to send: {emailIds}",
                emailIds.Count, string.Join(", ", emailIds));

            if(emailIds.Any())
            {
                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_options.UserName, _options.Password);

                    var from = new MailboxAddress(Constants.SystemName, _options.FromAddress);

                    foreach(var emailId in emailIds)
                    {
                        using (var trn = _dbContext.Database.BeginTransaction(IsolationLevel.Serializable))
                        {
                            try
                            {
                                var email = await _dbContext.OutboundEmails
                                    .SingleOrDefaultAsync(e => e.OutboundEmailId == emailId);

                                if(email == null)
                                {
                                    _logger.LogDebug("Email {emailId} was in list to be sent, but now appears to have been sent.", emailId);
                                }
                                else
                                {
                                    _logger.LogInformation("Sending email {emailId} to {recipient}", emailId, email.Recipient);

                                    await client.SendAsync(email.ToMimeKitMessage(from));

                                    email.DeliveredAt = _clock.SwedenNow;

                                    _dbContext.SaveChanges();

                                    trn.Commit();
                                }
                            }
                            catch(Exception ex)
                            {
                                _logger.LogError(ex, "Failure sending e-mail {emailId}");
                                trn.Rollback();
                            }
                        }
                    }
                }
            }
        }
    }
}
;