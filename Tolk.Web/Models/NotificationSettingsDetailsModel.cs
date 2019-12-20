using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class NotificationSettingsDetailsModel
    {
        public NotificationType Type { get; set; }

        public NotificationChannel Channel { get; set; }

        public string ContactInformation { get; set; }
    }
}
