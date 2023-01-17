using System;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public interface INotifyableEntity
    {
        public void AddNotification(NotificationType notificationType, DateTimeOffset createdAt);
    }
}
