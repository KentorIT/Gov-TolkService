using System;

namespace Tolk.BusinessLogic.Utilities
{
    /// <summary>
    /// Used to set which channels a notification type is avalable for
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class AvailableNotificationChannelAttribute : Attribute
    {
        /// <summary>
        /// ctor
        /// </summary>
        public AvailableNotificationChannelAttribute(NotificationChannel notificationChannel)
        {
            NotificationChannel = notificationChannel;
        }

        /// <summary>
        /// Channel
        /// </summary>
        public NotificationChannel NotificationChannel { get; private set; }
    }
}
