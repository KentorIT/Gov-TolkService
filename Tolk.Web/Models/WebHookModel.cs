using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class WebHookModel
    {
        [Display(Name = "Id")]
        public int OutboundWebHookCallId { get; set; }
        [Display(Name = "Mottagande url")]
        public string RecipientUrl { get; set; }
        [Display(Name = "Innehåll")]
        public string Payload { get; set; }
        [Display(Name = "Notifikationstyp")]
        public NotificationType NotificationType { get; set; }
        [Display(Name = "Skapad")]
        public DateTimeOffset CreatedAt { get; set; }
        [Display(Name = "Skickad")]
        public DateTimeOffset? DeliveredAt { get; set; }
        public IEnumerable<FailedTryModel> FailedTries { get; set; }
        [Display(Name = "Ersatt av")]
        public int? ReplacedBy { get; set; }
        [Display(Name = "Ersätter")]
        public int? Replaces { get; set; }
        [Display(Name = "Förmedling")]
        public string BrokerName { get; set; }

        public bool ShowBroker { get; set; }
        public bool AllowResend { get; set; }
    }
}
