using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

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

    //NOTE: ADD TO ENUMS NAMESPACE
    public enum NotificationStatus
    {
        New = 1,
        CreatingPayload = 2,
        PayloadCreated = 3,
        FailedToCreatePayload = 4,
        NoRecipientsFound = 5
    }

    //Create an interfaces namespace, end move this there
    public interface INotifyableEntity
    {
        public void AddNotification(NotificationType notificationType, DateTimeOffset createdAt);
    }

    public class DeliverableNotificationModel
    {
        public int NotificationId { get; set; }
        public NotificationChannel NotificationChannel { get; set; }
    }

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
        //public List<OrderAttachment> PeppolMessages { get; set; }
    }
    public class RequestNotificationEmail
    {
        public int RequestNotificationId { get; set; }

        [ForeignKey(nameof(RequestNotificationId))]
        public RequestNotification RequestNotification { get; set; }

        public int OutboundEmailId { get; set; }

        [ForeignKey(nameof(OutboundEmailId))]
        public OutboundEmail OutboundEmail { get; set; }
    }

    public class RequestNotificationWebhook
    {
        public int RequestNotificationId { get; set; }

        [ForeignKey(nameof(RequestNotificationId))]
        public RequestNotification RequestNotification { get; set; }

        public int OutboundWebhookId { get; set; }

        [ForeignKey(nameof(OutboundWebhookId))]
        public OutboundWebHookCall OutboundWebhook { get; set; }
    }
    public class EmailTexts
    { 
        public string Subject { get; set; }

        public string BodyPlain { get; set; }
        
        public string BodyHtml { get; set; }

        public string FrameworkAgreementNumber { get; set; }
    }
}
