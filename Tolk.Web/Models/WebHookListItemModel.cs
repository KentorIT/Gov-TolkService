using System;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class WebHookListItemModel
    {
        public int OutboundWebHookCallId { get; set; }
        [Display(Name = "Skapad")]
        public DateTimeOffset CreatedAt { get; set; }
        [Display(Name = "Skickad")]
        public DateTimeOffset? DeliveredAt { get; set; }
        [Display(Name = "Misslyckade försök")]
        public int FailedTries { get; set; }
        [Display(Name = "Notifieringstyp")]
        public NotificationType NotificationType { get; set; }
        [Display(Name = "Är omskickad")]
        public bool HasBeenResent { get; set; }
        public string ListColor { get; set; }
    }
}
