using System;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Models.Notification
{
    public class NotificationDisplayModel
    {
        public NotificationDisplayModel()
        {
        }

        public bool IsChecked { get; set; }

        public NotificationType NotificationType { get; set; }

        public int Count { get; set; }

        public string Description => $"{NotificationType.GetDescription()} ({Count})";
    }

}
