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
    public class EmailService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<EmailService> _logger;
        private readonly TolkBaseOptions.SmtpSettings _options;
        private readonly ISwedishClock _clock;
        private readonly string _senderPrepend;
        private readonly string _secondLineSupportMail;

        public EmailService(
            TolkDbContext dbContext,
            ILogger<EmailService> logger,
            ITolkBaseOptions options,
            ISwedishClock clock)
        {
            _dbContext = dbContext;
            _logger = logger;
            _options = options.Smtp;
            _clock = clock;
            _senderPrepend = !string.IsNullOrWhiteSpace(options.Env.DisplayName) ? $"{options.Env.DisplayName} " : string.Empty;
            _secondLineSupportMail = options.Support.SecondLineEmail;
        }

        public async Task SendEmails()
        {
            var emailIds = await _dbContext.OutboundEmails
                .Where(e => e.DeliveredAt == null)
                .Select(e => e.OutboundEmailId)
                .ToListAsync();

            _logger.LogInformation("Found {count} emails to send: {emailIds}",
                emailIds.Count, string.Join(", ", emailIds));

            if (emailIds.Any())
            {
                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_options.UserName, _options.Password);

                    var from = new MailboxAddress(_senderPrepend + Constants.SystemName, _options.FromAddress);

                    foreach (var emailId in emailIds)
                    {
                        using (var trn = _dbContext.Database.BeginTransaction(IsolationLevel.Serializable))
                        {
                            try
                            {
                                var email = await _dbContext.OutboundEmails
                                    .SingleOrDefaultAsync(e => e.OutboundEmailId == emailId);

                                if (email == null)
                                {
                                    _logger.LogInformation("Email {emailId} was in list to be sent, but now appears to have been sent.", emailId);
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
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failure sending e-mail {emailId}");
                                trn.Rollback();
                            }
                        }
                    }
                }
            }
        }

        public async Task SendSupportErrorEmail(string classname, string methodname, Exception ex)
        {
            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_options.UserName, _options.Password);
                var from = new MailboxAddress(_senderPrepend + Constants.SystemName, _options.FromAddress);
                var message = new MimeMessage();

                message.From.Add(from);
                message.To.Add(new MailboxAddress(_secondLineSupportMail));
                message.Subject = $"Exception in {classname} method {methodname}";
                var builder = new BodyBuilder
                {
                    TextBody = $"Exception message:\n{ex.Message}\n\nException info:\n{ex.ToString()}\n\nStackTrace:\n{ex.StackTrace}"
                };
                message.Body = builder.ToMessageBody();
                try
                {
                    _logger.LogInformation("Sending email to {recipient}", _secondLineSupportMail);
                    await client.SendAsync(message);

                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failure sending e-mail to {recipient}", _secondLineSupportMail);
                }
            }
        }
    }
}