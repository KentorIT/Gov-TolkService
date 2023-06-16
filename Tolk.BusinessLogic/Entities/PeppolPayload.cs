using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class PeppolPayload
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PeppolPayloadId { get; set; }
        [MaxLength(50)]
        public string IdentificationNumber { get; set; }
        public int RequestId { get; set; }
        public int OrderId { get; set; }
        [Required]
        public byte[] Payload { get; set; }
        public int? RequisitionId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public int? ReplacedById { get; set; }

        [ForeignKey(nameof(ReplacedById))]
        public PeppolPayload ReplacedByPayload { get; set; }

        [InverseProperty(nameof(ReplacedByPayload))]
        public PeppolPayload ReplacingPayload { get; set; }
        public PeppolMessageType PeppolMessageType { get; set; }

        public int? OutboundPeppolMessageId { get; set; }

        #region Foreign keys

        [ForeignKey(nameof(RequisitionId))]
        public Requisition BasedOnRequisition { get; set; }

        [ForeignKey(nameof(RequestId))]
        public Request Request { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }

        [ForeignKey(nameof(OutboundPeppolMessageId))]
        public OutboundPeppolMessage OutboundPeppolMessage { get; set; }

        #endregion

        public PeppolPayload()
        {
        }

        public PeppolPayload(Request request, PeppolMessageType type, byte[] payload, DateTimeOffset createdAt, string documentId)
        {
            RequestId = request.RequestId;
            RequisitionId = request.CurrentlyActiveRequisition?.RequisitionId;
            OrderId = request.Order.OrderId;
            PeppolMessageType = type;
            Payload = payload;
            CreatedAt = createdAt;
            IdentificationNumber = documentId;
        }
    }
}