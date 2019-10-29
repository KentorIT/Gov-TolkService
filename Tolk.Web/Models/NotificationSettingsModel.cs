using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class NotificationSettingsModel
    {
        [NoDisplayName]
        public NotificationType Type { get; set; }

        [Display(Name = "Webhook")]
        public bool UseWebHook { get; set; }

        [ClientRequired]
        [Display(Name = "Webhook url")]
        [Url]
        public string WebHookReceipentAddress { get; set; }

        [Display(Name = "E-post")]
        public bool UseEmail { get; set; }

        [RegularExpression(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*@((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$", ErrorMessage = "Felaktig e-postadress")]
        [Display(Name = "Specifik e-postadress", Description = "Egen e-post för just denna notifiering. Annars går den till er default.")]
        [StringLength(255)]
        public string SpecificEmail { get; set; }
    }
}
