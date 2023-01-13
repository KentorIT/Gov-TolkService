using System;

namespace Tolk.BusinessLogic.Utilities
{
    /// <summary>
    /// Used to set which channels a notification type is avalable for
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class NotificationConsumerTypeAttribute : Attribute
    {
        /// <summary>
        /// ctor
        /// </summary>
        public NotificationConsumerTypeAttribute(NotificationConsumerType notificationConsumerType, bool notifyContactPerson = false)
        {
            NotificationConsumerType = notificationConsumerType;
            NotifyContactPerson = notifyContactPerson;
        }

        /// <summary>
        /// Consumer
        /// </summary>
        public NotificationConsumerType NotificationConsumerType { get; private set; }

        /// <summary>
        /// States if contact person should be added to the notification list for specified Notification type, if applicable.
        /// </summary>
        public bool NotifyContactPerson { get; private set; }
    }
}
