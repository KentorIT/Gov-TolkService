using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class NotificationSettingsModel
    {
        public NotificationType Type { get; set; }
        public bool UseWebHook { get; set; }
        //[Url]
        public string WebHookUrl { get; set; }
        public bool UseEmail { get; set; }
        //[EmailAddress]
        public string SpecificEmail { get; set; }
    }
}
