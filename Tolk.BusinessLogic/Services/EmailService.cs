using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Helpers;

namespace Tolk.BusinessLogic.Services
{
    public class EmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly TolkOptions _options;
        private readonly ISwedishClock _clock;
        private readonly string _senderPrepend;
        private readonly string _secondLineSupportMail;

        public EmailService(
            ILogger<EmailService> logger,
            IOptions<TolkOptions> options,
            ISwedishClock clock)
        {
            _logger = logger;
            _options = options?.Value;
            _clock = clock;
            _senderPrepend = !string.IsNullOrWhiteSpace(_options.Env.DisplayName) ? $"{_options.Env.DisplayName} " : string.Empty;
            _secondLineSupportMail = _options.Support?.SecondLineEmail;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Must not stop, any errors must be swollowed")]
        public async Task SendEmails()
        {
            using (TolkDbContext context = _options.GetContext())
            {
                var emailIds = await context.OutboundEmails
                .Where(e => e.DeliveredAt == null && !e.IsHandling)
                .Select(e => e.OutboundEmailId)
                .ToListAsync();

                _logger.LogInformation("Found {count} emails to send: {emailIds}",
                    emailIds.Count, string.Join(", ", emailIds));

                if (emailIds.Any())
                {
                    try
                    {
                        var emails = context.OutboundEmails
                            .Where(e => emailIds.Contains(e.OutboundEmailId) && e.IsHandling == false)
                            .Select(c => c);
                        await emails.ForEachAsync(c => c.IsHandling = true);
                        await context.SaveChangesAsync();

                        using (var client = new SmtpClient())
                        {
                            await client.ConnectAsync(_options.Smtp.Host, _options.Smtp.Port, _options.Smtp.UseAuthentication ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
                            if (_options.Smtp.UseAuthentication)
                            {
                                await client.AuthenticateAsync(_options.Smtp.UserName, _options.Smtp.Password);
                            }
                            var from = new MailboxAddress(_senderPrepend + Constants.SystemName, _options.Smtp.FromAddress);

                            foreach (var emailId in emailIds)
                            {
                                var email = await context.OutboundEmails
                                    .SingleOrDefaultAsync(e => e.OutboundEmailId == emailId && e.DeliveredAt == null);
                                try
                                {
                                    if (email == null)
                                    {
                                        _logger.LogInformation("Email {emailId} was in list to be sent, but now appears to have been sent.", emailId);
                                    }
                                    else
                                    {
                                        email.IsHandling = true;
                                        await context.SaveChangesAsync();
                                        _logger.LogInformation("Sending email {emailId} to {recipient}", emailId, email.Recipient);

                                        await client.SendAsync(email.ToMimeKitMessage(from));
                                        email.DeliveredAt = _clock.SwedenNow;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failure sending e-mail {emailId}");
                                }
                                finally
                                {
                                    email.IsHandling = false;
                                    await context.SaveChangesAsync();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Something went wrong when sending emails");
                    }
                    finally
                    {
                        //Making sure no emails are left hanging
                        var emails = context.OutboundEmails
                            .Where(e => emailIds.Contains(e.OutboundEmailId) && e.IsHandling == true)
                            .Select(c => c);
                        await emails.ForEachAsync(c => c.IsHandling = false);
                        await context.SaveChangesAsync();
                    }
                }
            }
        }

        public async Task SendErrorEmail(string classname, string methodname, Exception ex)
        {
            await SendApplicationManagementEmail($"Exception in {classname} method {methodname}",
                $"Exception message:\n{ex?.Message}\n\nException info:\n{ex?.ToString()}\n\nStackTrace:\n{ex?.StackTrace}");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Must not stop, any errors must be swollowed")]
        public async Task SendApplicationManagementEmail(string subject, string messageBody)
        {
            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_options.Smtp.Host, _options.Smtp.Port, _options.Smtp.UseAuthentication ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
                if (_options.Smtp.UseAuthentication)
                {
                    await client.AuthenticateAsync(_options.Smtp.UserName, _options.Smtp.Password);
                }
                var from = new MailboxAddress(_senderPrepend + Constants.SystemName, _options.Smtp.FromAddress);
                var message = new MimeMessage();

                message.From.Add(from);
                message.To.Add(MailboxAddress.Parse(_secondLineSupportMail));
                message.Subject = subject;
                var builder = new BodyBuilder
                {
                    TextBody = messageBody
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