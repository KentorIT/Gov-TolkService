using System;

namespace Tolk.BusinessLogic.Entities
{
    public class NotificationBase
    {
        public NotificationBase(DateTimeOffset createdAt)
        {
            CreatedAt = createdAt;
        }
        public DateTimeOffset? DeliveredAt { get; set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public bool IsHandling { get; set; }
    }
}
