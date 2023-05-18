using System;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;

namespace Tolk.Web.Models
{
    public class EmailModel
    {
        [ColumnDefinitions(IsIdColumn = true, Index = 0, Name = nameof(OutboundEmailId), Visible = false)]
        public int OutboundEmailId { get; set; }

        [Display(Name = "Ämne")]
        [ColumnDefinitions(Index = 3, Name = nameof(Subject), Title = "Ämne")]
        public string Subject { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Innehåll")]
        public string Body { get; set; }

        [Display(Name = "Mottagare")]
        [ColumnDefinitions(Index = 2, Name = nameof(Recipient), Title = "Mottagare")]
        public string Recipient { get; set; }

        [Display(Name = "Skapat")]
        public DateTimeOffset CreatedAt { get; set; }

        [Display(Name = "Skickat")]
        public DateTimeOffset? SentAt { get; set; }

        [Display(Name = "Notifieringstyp")]
        public NotificationType? NotificationType { get; set; }

        [Display(Name = "Detta e-postmeddelande blev omskickat")]
        public DateTimeOffset? ResentAt { get; set; }

        [Display(Name = "Omskickat")]
        public bool IsResent => ReplacingEmailId.HasValue;

        public string ErrorMessage { get; set; }

        public bool DisplayResend { get; set; }

        public int? ReplacingEmailId { get; set; }

        internal static EmailModel GetModelFromOutboundEmail(OutboundEmail mail, bool displayResend, string errormessage = null)
        {
            return new EmailModel
            {
                OutboundEmailId = mail.OutboundEmailId,
                CreatedAt = mail.CreatedAt,
                Subject = mail.Subject,
                Body = mail.PlainBody,
                Recipient = mail.Recipient,
                SentAt = mail.DeliveredAt,
                ReplacingEmailId = mail.ReplacedByEmail?.OutboundEmailId,
                ResentAt = mail.ReplacedByEmail?.CreatedAt,
                ErrorMessage = errormessage,
                DisplayResend = displayResend,
                NotificationType = mail.NotificationType
            };
        }
    }

}
