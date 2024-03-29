﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
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

        public int? CarCompensation { get; set; }

        [MaxLength(1000)]
        public string PerDiem { get; set; }

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
        public string CustomerComment { get; set; }

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

        public TaxCardType? InterpretersTaxCard { get; set; }

        public List<RequisitionPriceRow> PriceRows { get; set; }

        public List<RequisitionAttachment> Attachments { get; set; }

        public List<MealBreak> MealBreaks { get; set; }

        public List<RequisitionStatusConfirmation> RequisitionStatusConfirmations { get; set; }
        
        public PeppolPayload PeppolPayload { get; set; }

        #region methods

        public void Review(DateTimeOffset approveTime, int userId, int? impersonatorId)
        {
            if (!ProcessAllowed)
            {
                throw new InvalidOperationException("Rekvisitionen har inte rätt status för att granskas.");
            }
            Status = RequisitionStatus.Reviewed;
            ProcessedAt = approveTime;
            ProcessedBy = userId;
            ImpersonatingProcessedBy = impersonatorId;
        }

        public void CofirmNoReview(DateTimeOffset confirmedAt, int userId, int? impersonatorId)
        {
            if (!CofirmNoReviewAllowed)
            {
                throw new InvalidOperationException($"Rekvisitionen är inte i rätt tillstånd för att arkiveras, förmodligen har den redan arkiverats.");
            }
            RequisitionStatusConfirmations.Add(new RequisitionStatusConfirmation { ConfirmedBy = userId, ImpersonatingConfirmedBy = impersonatorId, RequisitionStatus = Status, ConfirmedAt = confirmedAt });
        }

        public bool ProcessAllowed
        {
            get { return Status == RequisitionStatus.Created; }
        }

        public bool CofirmNoReviewAllowed
        {
            get { return Status == RequisitionStatus.Created && !RequisitionStatusConfirmations.Any(r => r.RequisitionStatus == RequisitionStatus.Created); }
        }

        public void Comment(DateTimeOffset denyTime, int userId, int? impersonatorId, string comment)
        {
            if (!ProcessAllowed)
            {
                throw new InvalidOperationException("Rekvisitionen har inte rätt status för att kommenteras.");
            }
            Status = RequisitionStatus.Commented;
            ProcessedAt = denyTime;
            ProcessedBy = userId;
            ImpersonatingProcessedBy = impersonatorId;
            CustomerComment = comment;
        }

        #endregion
    }
}
