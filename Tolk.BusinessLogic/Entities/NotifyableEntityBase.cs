using System;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class NotifyableEntityBase
    {
        internal NotifyableEntityBase() { }

        public NotifyableEntityBase(NotificationType notificationType, DateTimeOffset createdAt)
        {
            CreatedAt = createdAt;
            NotificationType = notificationType;
            Status = NotificationStatus.New;
        }

        [Required]
        public NotificationType NotificationType { get; set; }

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        [Required]
        public NotificationStatus Status { get; set; }
    }
}
