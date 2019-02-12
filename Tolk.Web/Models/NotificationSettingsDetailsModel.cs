using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class NotificationSettingsDetailsModel
    {
        public NotificationType Type { get; set; }

        public NotificationChannel Channel { get; set; }

        public string ContactInformation { get; set; }
    }
}
