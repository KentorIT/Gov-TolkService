﻿using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Data.Migrations;
using System.Linq;
using Tolk.BusinessLogic.Enums;

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

        [Column(TypeName = "decimal(10, 2)")]
        public decimal TravelCosts { get; set; } = 0;

        public RequisitionStatus Status { get; set; }

        public DateTimeOffset? TimeWasteBeforeStartedAt { get; set; }

        public DateTimeOffset SessionStartedAt { get; set; }

        public DateTimeOffset SessionEndedAt { get; set; }

        public DateTimeOffset? TimeWasteAfterEndedAt { get; set; }

        [MaxLength(1000)]
        [Required]
        public string Message { get; set; }

        [MaxLength(255)]
        public string DenyMessage { get; set; }

        public int RequestId { get; set; }

        [ForeignKey(nameof(RequestId))]
        public Request Request { get; set; }

        public DateTimeOffset? ProcessedAt { get; set; }

        public int? ProcessedBy { get; set; }

        [ForeignKey(nameof(ProcessedBy))]
        public AspNetUser ProcessedUser { get; set; }

        public int? ImpersonatingProcessedBy { get; set; }

        [ForeignKey(nameof(ImpersonatingProcessedBy))]
        public AspNetUser ProcessedByImpersonator { get; set; }

        public void Approve(DateTimeOffset approveTime, int userId, int? impersonatorId)
        {
        }

        public void Deny(DateTimeOffset denyTime, int userId, int? impersonatorId, string message)
        {
        }
    }
}
