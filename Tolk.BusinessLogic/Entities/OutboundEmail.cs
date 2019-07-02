using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MimeKit;

namespace Tolk.BusinessLogic.Entities
{
    public class OutboundEmail
    {
        public OutboundEmail(
            string recipient,
            string subject,
            string plainBody,
            string htmlBody,
            DateTimeOffset createdAt,
            int? replacingEmailId = null)
        {
            Recipient = recipient;
            Subject = subject;
            PlainBody = plainBody;
            HtmlBody = htmlBody;
            CreatedAt = createdAt;
            ReplacingEmailId = replacingEmailId;
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

        public DateTimeOffset CreatedAt { get; private set; }

        public DateTimeOffset? DeliveredAt { get; set; }

        public int? ReplacingEmailId { get; set; }

        [ForeignKey(nameof(ReplacingEmailId))]
        [InverseProperty(nameof(ReplacedByEmail))]
        public OutboundEmail ReplacingEmail { get; set; }

        [InverseProperty(nameof(ReplacingEmail))]
        public OutboundEmail ReplacedByEmail { get; set; }
               
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
