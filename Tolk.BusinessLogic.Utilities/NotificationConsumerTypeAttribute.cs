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
        public NotificationConsumerTypeAttribute(NotificationConsumerType notificationConsumerType)
        {
            NotificationConsumerType = notificationConsumerType;
        }

        /// <summary>
        /// Channel
        /// </summary>
        public NotificationConsumerType NotificationConsumerType { get; private set; }
    }
}
