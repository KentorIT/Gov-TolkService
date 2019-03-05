using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Validation;

namespace Tolk.BusinessLogic.Entities
{
    public class Requisition
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequisitionId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public int CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public AspNetUser CreatedByUser { get; set; }

        public int? ImpersonatingCreatedBy { get; set; }

        [ForeignKey(nameof(ImpersonatingCreatedBy))]
        public AspNetUser CreatedByImpersonator { get; set; }

        public RequisitionStatus Status { get; set; }

        public DateTimeOffset SessionStartedAt { get; set; }

        private DateTimeOffset _sessionEndedAt;

        public DateTimeOffset SessionEndedAt
        {
            get
            {
                return _sessionEndedAt;
            }
            set
            {
                Validate.Ensure(value > SessionStartedAt, $"{nameof(SessionEndedAt)} cannot occur after {nameof(SessionStartedAt)}");
                _sessionEndedAt = value;
            }
        }

        public int? TimeWasteTotalTime { get => (TimeWasteNormalTime ?? 0) + (TimeWasteIWHTime ?? 0); }

        public int? TimeWasteNormalTime { get; set; }

        public int? TimeWasteIWHTime { get; set; }

        [MaxLength(1000)]
        [Required]
        public string Message { get; set; }

        [MaxLength(255)]
        public string DenyMessage { get; set; }

        public int? ReplacedByRequisitionId { get; set; }

        [ForeignKey(nameof(ReplacedByRequisitionId))]
        public Requisition ReplacedByRequisition { get; set; }

        public int RequestId { get; set; }

        [ForeignKey(nameof(RequestId))]
        public Request Request { get; set; }

        public DateTimeOffset? ProcessedAt { get; set; }

        public int? ProcessedBy { get; set; }

        public bool RequestOrReplacingOrderPeriodUsed { get; set; }

        [ForeignKey(nameof(ProcessedBy))]
        public AspNetUser ProcessedUser { get; set; }

        public int? ImpersonatingProcessedBy { get; set; }

        [ForeignKey(nameof(ImpersonatingProcessedBy))]
        public AspNetUser ProcessedByImpersonator { get; set; }

        public TaxCard? InterpretersTaxCard { get; set; }

        public List<RequisitionPriceRow> PriceRows { get; set; }

        public List<RequisitionAttachment> Attachments { get; set; }

        public List<MealBreak> MealBreaks { get; set; }

        #region methods

        public void Review(DateTimeOffset approveTime, int userId, int? impersonatorId)
        {
            if (Status != RequisitionStatus.Created)
            {
                throw new InvalidOperationException($"Requisition {RequisitionId} is {Status}. Only unprocessed requisitions can be approved");
            }

            Status = RequisitionStatus.Reviewed;
            ProcessedAt = approveTime;
            ProcessedBy = userId;
            ImpersonatingProcessedBy = impersonatorId;
        }

        public bool ProcessAllowed
        {
            get { return Status == RequisitionStatus.Created; }
        }

        public void Comment(DateTimeOffset denyTime, int userId, int? impersonatorId, string message)
        {
            if (Status != RequisitionStatus.Created)
            {
                throw new InvalidOperationException($"Requisition {RequisitionId} is {Status}. Only unprocessed requisitions can be denied");
            }

            Status = RequisitionStatus.Commented;
            ProcessedAt = denyTime;
            ProcessedBy = userId;
            ImpersonatingProcessedBy = impersonatorId;
            DenyMessage = message;
        }

        #endregion
    }
}
