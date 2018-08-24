using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class Complaint
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ComplaintId { get; set; }

        public ComplaintStatus Status { get; set; }

        public ComplaintType ComplaintType { get; set; }

        public int RequestId { get; set; }

        [ForeignKey(nameof(RequestId))]
        public Request Request { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public int CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public AspNetUser CreatedByUser { get; set; }

        public int? ImpersonatingCreatedBy { get; set; }

        [ForeignKey(nameof(ImpersonatingCreatedBy))]
        public AspNetUser CreatedByImpersonator { get; set; }

        [MaxLength(1000)]
        [Required]
        public string ComplaintMessage { get; set; }

        public DateTimeOffset? AnsweredAt { get; set; }

        public int? AnsweredBy { get; set; }

        [ForeignKey(nameof(AnsweredBy))]
        public AspNetUser AnsweringUser { get; set; }

        public int? ImpersonatingAnsweredBy { get; set; }

        [ForeignKey(nameof(ImpersonatingAnsweredBy))]
        public AspNetUser AnsweredByImpersonator { get; set; }

        [MaxLength(1000)]
        public string AnswerMessage { get; set; }

        public DateTimeOffset? AnswerDisputedAt { get; set; }

        public int? AnswerDisputedBy { get; set; }

        [ForeignKey(nameof(AnswerDisputedBy))]
        public AspNetUser AnswerDisputingUser { get; set; }

        public int? ImpersonatingAnswerDisputedBy { get; set; }

        [ForeignKey(nameof(ImpersonatingAnswerDisputedBy))]
        public AspNetUser AnswerDisputedByImpersonator { get; set; }

        [MaxLength(1000)]
        public string AnswerDisputedMessage { get; set; }

        public DateTimeOffset? TerminatedAt { get; set; }

        public int? TerminatedBy { get; set; }

        [ForeignKey(nameof(TerminatedBy))]
        public AspNetUser TerminatingUser { get; set; }

        public int? ImpersonatingTerminatedBy { get; set; }

        [ForeignKey(nameof(ImpersonatingTerminatedBy))]
        public AspNetUser TerminatedByImpersonator { get; set; }

        [MaxLength(1000)]
        public string TerminationMessage { get; set; }

        public void Accept(DateTimeOffset acceptedAt, int userId, int? impersonatorId)
        {
            if (Status != ComplaintStatus.Created)
            {
                throw new InvalidOperationException($"Complaint {ComplaintId} is {Status}. Only Created complaints can be accepted.");
            }

            Status = ComplaintStatus.Confirmed;
            AnswerDisputedAt = acceptedAt;
            AnsweredAt = acceptedAt;
            AnsweredBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
        }

        public void Dispute(DateTimeOffset disputedAt, int userId, int? impersonatorId, string message)
        {
            if (Status != ComplaintStatus.Created)
            {
                throw new InvalidOperationException($"Complaint {ComplaintId} is {Status}. Only Created complaints can be Disputed.");
            }
            Status = ComplaintStatus.Disputed;
            AnsweredAt = disputedAt;
            AnsweredBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
            AnswerMessage = message;
        }

        public void AcceptDispute(DateTimeOffset acceptedAt, int userId, int? impersonatorId)
        {
            if (Status != ComplaintStatus.Disputed)
            {
                throw new InvalidOperationException($"Complaint {ComplaintId} is {Status}. Only Disputed complaints can be terminated as accepted.");
            }

            Status = ComplaintStatus.TerminatedAsDisputeAccepted;
            AnswerDisputedAt = acceptedAt;
            AnswerDisputedBy = userId;
            ImpersonatingAnswerDisputedBy = impersonatorId;
        }

        public void Refute(DateTimeOffset refutedAt, int userId, int? impersonatorId, string message)
        {
            if (Status != ComplaintStatus.Disputed)
            {
                throw new InvalidOperationException($"Complaint {ComplaintId} is {Status}. Only Disputed complaints can be refuted.");
            }
            Status = ComplaintStatus.DisputePendingTrial;
            AnswerDisputedAt = refutedAt;
            AnswerDisputedBy = userId;
            ImpersonatingAnswerDisputedBy = impersonatorId;
            AnswerDisputedMessage = message;
        }

    }
}
