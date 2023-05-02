using MimeKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class OutboundPeppolMessage : NotificationBase
    {
        public OutboundPeppolMessage(
            string identifier,
            string recipient,
            byte[] payload,
            DateTimeOffset createdAt,
            NotificationType notificationType,
            int? resentByUserId = null,
            int? resentImpersonatorUserId = null,
            int? replacingPeppolMessageId = null)
            : base(createdAt)
        {
            Identifier = identifier;
            Recipient = recipient;
            Payload = payload;
            ResentByUserId = resentByUserId;
            ResentImpersonatorUserId = resentImpersonatorUserId;
            NotificationType = notificationType;
            ReplacingPeppolMessageId = replacingPeppolMessageId;
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OutboundPeppolMessageId { get; set; }

        [Required]
        public string Identifier { get; private set; }

        [Required]
        public string Recipient { get; private set; }

        [Required]
        public byte[] Payload { get; private set; }

        public int? ReplacingPeppolMessageId { get; set; }

        public int? ResentByUserId { get; set; }

        public NotificationType NotificationType { get; private set; }

        [ForeignKey(nameof(ReplacingPeppolMessageId))]
        [InverseProperty(nameof(ReplacedByMessage))]
        public OutboundPeppolMessage ReplacingMessage { get; set; }

        [InverseProperty(nameof(ReplacingMessage))]
        public OutboundPeppolMessage ReplacedByMessage { get; set; }

        [ForeignKey(nameof(ResentByUserId))]
        public AspNetUser ResentByUser { get; set; }

        public int? ResentImpersonatorUserId { get; private set; }

        [ForeignKey(nameof(ResentImpersonatorUserId))]
        public AspNetUser ResentImpersonatorUser { get; set; }

        public PeppolPayload PeppolMessagePayload { get; set; }

        public int FailedTries { get; set; }

        public ICollection<FailedPeppolMessage> FailedCalls { get; set; }
        public bool? HasNotifiedFailure { get; set; }

    }
}
