using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestNotification : NotifyableEntityBase
    {
        private RequestNotification() { }

        public RequestNotification(NotificationType notificationType, DateTimeOffset createdAt)
            : base(notificationType, createdAt)
        { }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestNotificationId { get; set; }

        public int RequestId { get; set; }

        [ForeignKey(nameof(RequestId))]
        public Request Request { get; set; }

        public List<RequestNotificationEmail> Emails { get; set; }

        public List<RequestNotificationWebhook> Webhooks { get; set; }
    }
}
