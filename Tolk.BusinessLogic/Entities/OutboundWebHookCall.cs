using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class OutboundWebHookCall : NotificationBase
    {
        public OutboundWebHookCall(
            string recipientUrl,
            string payload,
            NotificationType notificationType,
            DateTimeOffset createdAt,
            int recipientUserId,
            int? resentUserId = null,
            int? resentImpersonatorUserId = null)
            : base(createdAt)
        {
            RecipientUrl = recipientUrl;
            Payload = payload;
            NotificationType = notificationType;
            RecipientUserId = recipientUserId;
            ResentUserId = resentUserId;
            ResentImpersonatorUserId = resentImpersonatorUserId;
        }
        public int OutboundWebHookCallId { get; private set; }

        [Required]
        public string RecipientUrl { get; private set; }

        [Required]
        public string Payload { get; private set; }

        public NotificationType NotificationType { get; private set; }

        public int RecipientUserId { get; private set; }

        [ForeignKey(nameof(RecipientUserId))]
        public AspNetUser RecipientUser { get; set; }

        public int? ResentUserId { get; private set; }

        [ForeignKey(nameof(ResentUserId))]
        public AspNetUser ResentUser { get; set; }

        public int? ResentImpersonatorUserId { get; private set; }

        [ForeignKey(nameof(ResentImpersonatorUserId))]
        public AspNetUser ResentImpersonatorUser { get; set; }

        public int FailedTries { get; set; }

        public int? ResentHookId { get; set; }
        [ForeignKey(nameof(ResentHookId))]
        public OutboundWebHookCall ResentHook { get; set; }

        [InverseProperty(nameof(ResentHook))]
        public OutboundWebHookCall ReplacingWebHook { get; set; }

        public ICollection<FailedWebHookCall> FailedCalls { get; set; }
        public bool? HasNotifiedFailure { get; set; }
    }
}
