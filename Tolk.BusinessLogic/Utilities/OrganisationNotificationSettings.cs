using System;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Utilities
{
    [Serializable]
    public class OrganisationNotificationSettings
    {
        public NotificationType NotificationType { get; set; }
        public NotificationChannel NotificationChannel { get; set; }
        public string ContactInformation { get; set; }

        public NotificationConsumerType NotificationConsumerType { get; set; }

        public int RecipientUserId { get; set; }

        public int ReceivingOrganisationId { get; set; }

        public DateTime? StartUsingNotificationAt { get; set; }
    }
}
