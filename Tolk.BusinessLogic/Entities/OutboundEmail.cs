using MimeKit;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class OutboundEmail : NotificationBase
    {
        public OutboundEmail(
            string recipient,
            string subject,
            string plainBody,
            string htmlBody,
            DateTimeOffset createdAt,
            int? replacingEmailId = null,
            int? resentByUserId = null)
            : base(createdAt)
        {
            Recipient = recipient;
            Subject = subject;
            PlainBody = plainBody;
            HtmlBody = htmlBody;
            ReplacingEmailId = replacingEmailId;
            ResentByUserId = resentByUserId;
        }

        public int OutboundEmailId { get; private set; }

        [Required]
        public string Recipient { get; private set; }

        [Required]
        public string Subject { get; private set; }

        [Required]
        public string PlainBody { get; private set; }

        [Required]
        public string HtmlBody { get; private set; }

        public int? ReplacingEmailId { get; set; }

        public int? ResentByUserId { get; set; }

        [ForeignKey(nameof(ReplacingEmailId))]
        [InverseProperty(nameof(ReplacedByEmail))]
        public OutboundEmail ReplacingEmail { get; set; }

        [InverseProperty(nameof(ReplacingEmail))]
        public OutboundEmail ReplacedByEmail { get; set; }

        [ForeignKey(nameof(ResentByUserId))]
        public AspNetUser ResentByUser { get; set; }

        public MimeMessage ToMimeKitMessage(InternetAddress from)
        {
            var message = new MimeMessage();
            var builder = new BodyBuilder();

            message.From.Add(from);
            message.To.Add(new MailboxAddress(Recipient));
            message.Subject = Subject;

            builder.TextBody = PlainBody;
            builder.HtmlBody = ApplyHtmlTemplate(HtmlBody);

            message.Body = builder.ToMessageBody();

            return message;
        }

        private static string ApplyHtmlTemplate(string htmlMessage)
        {
            return $@"{htmlMessage}";
        }
    }
}
