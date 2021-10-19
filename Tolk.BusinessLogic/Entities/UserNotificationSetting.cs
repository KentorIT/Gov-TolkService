using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Entities
{
    public class UserNotificationSetting
    {
        public int UserId { get; set; }

        public NotificationChannel NotificationChannel { get; set; }

        public NotificationType NotificationType { get; set; }

        public string ConnectionInformation { get; set; }

        #region foreign keys

        [ForeignKey(nameof(UserId))]
        public AspNetUser User { get; set; }

        #endregion
    }
}
