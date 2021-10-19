using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class NotificationSettingsDetailsModel
    {
        public NotificationType Type { get; set; }

        public NotificationChannel Channel { get; set; }

        public string ContactInformation { get; set; }
    }
}
