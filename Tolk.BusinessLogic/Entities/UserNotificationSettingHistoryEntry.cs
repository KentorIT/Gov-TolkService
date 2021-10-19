using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Entities
{
    public class UserNotificationSettingHistoryEntry
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserNotificationSettingHistoryEntryId { get; set; }

        public int UserAuditLogEntryId { get; set; }

        public NotificationChannel NotificationChannel { get; set; }

        public NotificationType NotificationType { get; set; }

        public string ConnectionInformation { get; set; }

        [ForeignKey(nameof(UserAuditLogEntryId))]
        public UserAuditLogEntry UserAuditLogEntry { get; set; }
    }
}

