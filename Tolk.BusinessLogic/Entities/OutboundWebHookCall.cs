using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class OutboundWebHookCall
    {
        public OutboundWebHookCall(
            string recipientUrl,
            string payload,
            NotificationType notificationType,
            DateTimeOffset createdAt,
            int recipientUserId)
        {
            RecipientUrl = recipientUrl;
            Payload = payload;
            CreatedAt = createdAt;
            NotificationType = notificationType;
            RecipientUserId = recipientUserId;
        }
        public int OutboundWebHookCallId { get; private set; }

        [Required]
        public string RecipientUrl { get; private set; }

        [Required]
        public string Payload { get; private set; }

        public NotificationType NotificationType { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public int RecipientUserId { get; private set; }

        [ForeignKey(nameof(RecipientUserId))]
        public AspNetUser RecipientUser { get; set; }

        public DateTimeOffset? DeliveredAt { get; set; }

        public int FailedTries { get; set; }

        public int? ResentHookId { get; set; }
        [ForeignKey(nameof(ResentHookId))]
        public OutboundWebHookCall ResentHook { get; set; }

        [InverseProperty(nameof(ResentHook))]
        public OutboundWebHookCall ReplacingWebHook { get; set; }

        public ICollection<FailedWebHookCall> FailedCalls { get; set; }
    }
}
