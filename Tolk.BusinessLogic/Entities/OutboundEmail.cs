using MimeKit;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

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
            NotificationType notificationType,            
            int? replacingEmailId = null,
            int? resentByUserId = null,
            int? recipientUserId = null,
            int? recipientCustomerUnitId = null
            )
            : base(createdAt)
        {
            Recipient = recipient;
            Subject = subject;
            PlainBody = plainBody;
            HtmlBody = htmlBody;
            ReplacingEmailId = replacingEmailId;
            ResentByUserId = resentByUserId;
            NotificationType = notificationType;
            RecipientUserId = recipientUserId;
            RecipientCustomerUnitId = recipientCustomerUnitId;
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

        public NotificationType NotificationType { get; private set; }
        
        public int? RecipientUserId { get; private set; }

        [ForeignKey(nameof(RecipientUserId))]
        public AspNetUser RecipientUser { get; set; }

        public int? RecipientCustomerUnitId { get; private set; }

        [ForeignKey(nameof(RecipientCustomerUnitId))]
        public CustomerUnit RecipientCustomerUnit { get; set; }

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
            message.To.Add(MailboxAddress.Parse(Recipient));
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
