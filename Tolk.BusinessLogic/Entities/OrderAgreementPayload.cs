using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderAgreementPayload
    {
        public int OrderId { get; set; }

        public int Index { get; set; }

        [Required]
        public string Payload { get; private set; }

        public int? RequisitionId { get; set; }

        public int? RequestId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        //This will be null if the payload was created automatically by the system
        public int? CreatedBy { get; set; }

        public int? ImpersonatingCreatedBy { get; set; }

        #region Foreign keys

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }

        [ForeignKey(nameof(RequisitionId))]
        public Requisition BasedOnRequisition { get; set; }

        [ForeignKey(nameof(RequestId))]
        public Request BasedOnReqest { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public AspNetUser CreatedByUser { get; set; }

        [ForeignKey(nameof(ImpersonatingCreatedBy))]
        public AspNetUser CreatedByImpersonator { get; set; }

        #endregion
    }
}
