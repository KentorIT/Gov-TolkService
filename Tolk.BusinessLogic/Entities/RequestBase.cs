using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestBase
    {
        public int RankingId { get; set; }

        public virtual RequestStatus Status { get; set; }

        public Ranking Ranking { get; set; }

        /// <summary>
        /// The time (inclusive) when the request is expired. If null, expiry must be manually defined
        /// </summary>
        public DateTimeOffset? ExpiresAt { get; set; }

        /// <summary>
        /// The time can be set by broker and the request will expire for broker if customer not answers the request group within the set time
        /// </summary>
        public DateTimeOffset? LatestAnswerTimeForCustomer { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        [MaxLength(1000)]
        public string BrokerMessage { get; set; }

        [MaxLength(1000)]
        public string DenyMessage { get; set; }

        public DateTimeOffset? RecievedAt { get; set; }

        public int? ReceivedBy { get; set; }

        [ForeignKey(nameof(ReceivedBy))]
        public AspNetUser ReceivedByUser { get; set; }

        public int? ImpersonatingReceivedBy { get; set; }

        [ForeignKey(nameof(ImpersonatingReceivedBy))]
        public AspNetUser ReceivedByImpersonator { get; set; }

        public DateTimeOffset? AnswerDate { get; set; }

        public int? AnsweredBy { get; set; }

        [ForeignKey(nameof(AnsweredBy))]
        public AspNetUser AnsweringUser { get; set; }

        public int? ImpersonatingAnsweredBy { get; set; }

        [ForeignKey(nameof(ImpersonatingAnsweredBy))]
        public AspNetUser AnsweredByImpersonator { get; set; }

        public DateTimeOffset? AnswerProcessedAt { get; set; }

        public int? AnswerProcessedBy { get; set; }

        [ForeignKey(nameof(AnswerProcessedBy))]
        public AspNetUser ProcessingUser { get; private set; }

        public int? ImpersonatingAnswerProcessedBy { get; set; }

        [ForeignKey(nameof(ImpersonatingAnswerProcessedBy))]
        public AspNetUser AnswerProcessedByImpersonator { get; private set; }

        public DateTimeOffset? CancelledAt { get; set; }

        public int? CancelledBy { get; set; }

        [ForeignKey(nameof(CancelledBy))]
        public AspNetUser CancelledByUser { get; set; }

        public int? ImpersonatingCanceller { get; set; }

        [ForeignKey(nameof(ImpersonatingCanceller))]
        public AspNetUser CancelledByImpersonator { get; set; }

        [MaxLength(1000)]
        public string CancelMessage { get; set; }

        /// <summary>
        /// If true, this Request will not be followed by another to the next broker.
        /// </summary>
        public bool IsTerminalRequest { get; set; }

        public int? QuarantineId { get; set; }

        [ForeignKey(nameof(QuarantineId))]
        public Quarantine Quarantine { get; set; }

        [MaxLength(100)]
        public string BrokerReferenceNumber { get; set; }

        public RequestAnswerRuleType RequestAnswerRuleType { get; set; }

        /// <summary>
        /// The time (inclusive) when the request needs to be accepted. Only set if <see cref="RequestAnswerRuleType"/> is 1 or 2
        /// </summary>
        public DateTimeOffset? LastAcceptAt { get; set; }

        public DateTimeOffset? AcceptedAt { get; set; }

        public int? AcceptedBy { get; set; }

        [ForeignKey(nameof(AcceptedBy))]
        public AspNetUser AcceptingUser { get; set; }

        public int? ImpersonatingAcceptedBy { get; set; }

        [ForeignKey(nameof(ImpersonatingAcceptedBy))]
        public AspNetUser ImpersonatingAcceptingUser { get; set; }

        #region Status checks

        public bool IsAcceptedOrApproved 
            => IsAwaitingApproval || Status == RequestStatus.Approved || Status == RequestStatus.AcceptedAwaitingInterpreter;        

        public bool IsAwaitingApproval
            => Status == RequestStatus.AnsweredAwaitingApproval || Status == RequestStatus.AcceptedNewInterpreterAppointed;

        public bool CanDecline => IsToBeProcessedByBroker;

        public bool CanAccept => LastAcceptAt.HasValue && 
            EnumHelper.Parent<RequestAnswerRuleType, RequiredAnswerLevel>(RequestAnswerRuleType) == RequiredAnswerLevel.Acceptance && 
            (Status == RequestStatus.Created || Status == RequestStatus.Received);

        public bool CanApprove => IsAwaitingApproval;

        public bool CanPrint => Status == RequestStatus.Approved || Status == RequestStatus.Delivered;

        public bool CanDeny  => IsAwaitingApproval;

        public bool IsToBeProcessedByBroker 
            => Status == RequestStatus.Created || Status == RequestStatus.Received || Status == RequestStatus.AcceptedAwaitingInterpreter;
        #endregion

        #region status-changing methods

        internal virtual void Received(DateTimeOffset receiveTime, int userId, int? impersonatorId = null)
        {
            Status = RequestStatus.Received;
            RecievedAt = receiveTime;
            ReceivedBy = userId;
            ImpersonatingReceivedBy = impersonatorId;
        }

        public virtual void Decline(
            DateTimeOffset declinedAt,
            int userId,
            int? impersonatorId,
            string message)
        {
            Status = RequestStatus.DeclinedByBroker;
            AnswerDate = declinedAt;
            AnsweredBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
            DenyMessage = message;
        }

        internal virtual void Deny(DateTimeOffset denyTime, int userId, int? impersonatorId, string message)
        {
            Status = RequestStatus.DeniedByCreator;
            AnswerProcessedAt = denyTime;
            AnswerProcessedBy = userId;
            ImpersonatingAnswerProcessedBy = impersonatorId;
            DenyMessage = message;
        }

        internal virtual void Approve(DateTimeOffset approveTime, int userId, int? impersonatorId)
        {
            Status = RequestStatus.Approved;
            AnswerProcessedAt = approveTime;
            AnswerProcessedBy = userId;
            ImpersonatingAnswerProcessedBy = impersonatorId;
        }

        #endregion
    }
}
