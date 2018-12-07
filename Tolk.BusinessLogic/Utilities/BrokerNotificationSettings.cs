using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Utilities
{
    public class BrokerNotificationSettings
    {
        public NotificationType NotificationType { get; set; }
        public NotificationChannel NotificationChannel { get; set; }
        public string ContactInformation { get; set; }

        public int RecipientUserId { get; set; }
        
        public int BrokerId { get; set; }
    }
}
