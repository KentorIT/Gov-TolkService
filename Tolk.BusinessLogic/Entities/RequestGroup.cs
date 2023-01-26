using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestGroup : RequestBase
    {
        #region constructors

        internal RequestGroup() { }

        internal RequestGroup(Ranking ranking, RequestExpiryResponse newRequestExpiry, DateTimeOffset creationTime, List<Request> requests, bool isTerminalRequest = false, bool isAReplacingRequestGroup = false)
        {
            requests.ForEach(r => r.RequestGroup = this);
            Ranking = ranking;
            Status = RequestStatus.Created;
            ExpiresAt = newRequestExpiry.ExpiryAt;
            LastAcceptAt = newRequestExpiry.LastAcceptedAt;
            RequestAnswerRuleType = newRequestExpiry.RequestAnswerRuleType;
            CreatedAt = creationTime;
            IsTerminalRequest = isTerminalRequest;
            Requests = requests;
        }

        internal RequestGroup(Ranking ranking, DateTimeOffset creationTime, List<Request> requests, Quarantine quarantine)
        {
            requests.ForEach(r => r.RequestGroup = this);
            Ranking = ranking;
            Status = RequestStatus.LostDueToQuarantine;
            CreatedAt = creationTime;
            Quarantine = quarantine;
            QuarantineId = quarantine.QuarantineId;
        }

        #endregion

        #region properties

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestGroupId { get; set; }

        public int OrderGroupId { get; set; }

        [ForeignKey(nameof(OrderGroupId))]
        public OrderGroup OrderGroup { get; set; }

        public int? ReplacingRequestGroupId { get; set; }

        [ForeignKey(nameof(ReplacingRequestGroupId))]
        public RequestGroup ReplacingRequestGroup { get; set; }

        #endregion

        #region navigation

        public List<Request> Requests { get; set; }

        public List<RequestGroupStatusConfirmation> StatusConfirmations { get; set; }

        public List<RequestGroupView> Views { get; set; }

        public List<RequestGroupAttachment> Attachments { get; set; }

        public RequestGroupUpdateLatestAnswerTime RequestGroupUpdateLatestAnswerTime { get; set; }

        [InverseProperty(nameof(ReplacingRequestGroup))]
        public RequestGroup ReplacedByRequestGroup { get; set; }

        #endregion

        #region Methods

        public Request FirstRequestForFirstInterpreter => Requests.First(r => r.Order.IsExtraInterpreterForOrderId == null);

        public Request FirstRequestForExtraInterpreter => Requests.First(r => r.Order.IsExtraInterpreterForOrderId != null);

        internal void SetStatus(RequestStatus status, bool updateRequests = true)
        {
            Status = status;
            if (updateRequests)
            {
                Requests.ForEach(r => r.Status = status);
            }
        }

        public override RequestStatus Status
        {
            get => base.Status;
            set
            {
                if (value == RequestStatus.CancelledByBroker)
                {
                    throw new InvalidOperationException($"A {nameof(RequestGroup)} cannot be set to {nameof(RequestStatus.CancelledByBroker)}");
                }
                if (value == RequestStatus.AcceptedNewInterpreterAppointed)
                {
                    throw new InvalidOperationException($"A {nameof(RequestGroup)} cannot be set to {nameof(RequestStatus.AcceptedNewInterpreterAppointed)}");
                }
                if (value == RequestStatus.InterpreterReplaced)
                {
                    throw new InvalidOperationException($"A {nameof(RequestGroup)} cannot be set to {nameof(RequestStatus.InterpreterReplaced)}");
                }
                base.Status = value;
            }
        }

        public bool HasExtraInterpreter => OrderGroup.Orders.Any(o => o.IsExtraInterpreterForOrderId != null);

        public bool RequiresApproval(bool hasTravelCosts)
        {
            return hasTravelCosts &&
                OrderGroup.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved &&
                Requests.Any(r => r.InterpreterLocation.Value == (int)Enums.InterpreterLocation.OffSiteDesignatedLocation ||
                    r.InterpreterLocation.Value == (int)Enums.InterpreterLocation.OnSite);
        }

        public override void Decline(
            DateTimeOffset declinedAt,
            int userId,
            int? impersonatorId,
            string message)
        {
            if (!CanDecline)
            {
                throw new InvalidOperationException($"Det gick inte att tacka nej till den sammanhållna bokningen med boknings-id {OrderGroup.OrderGroupNumber}, den har redan blivit besvarad");
            }
            base.Decline(declinedAt, userId, impersonatorId, message);
            Requests.ForEach(r => r.DeclineInGroup(declinedAt, userId, impersonatorId, message));
            SetStatus(RequestStatus.DeclinedByBroker, false);
            OrderGroup.SetStatus(OrderStatus.Requested, false);
        }

        internal override void Deny(DateTimeOffset denyTime, int userId, int? impersonatorId, string message)
        {
            if (!CanDeny)
            {
                throw new InvalidOperationException($"RequestGRoup {RequestGroupId} is {Status}. Only Accepted request groups can be denied.");
            }
            base.Deny(denyTime, userId, impersonatorId, message);
            Requests.ForEach(r => r.Deny(denyTime, userId, impersonatorId, message));
            OrderGroup.SetStatus(OrderStatus.Requested, false);
        }

        internal override void Received(DateTimeOffset receiveTime, int userId, int? impersonatorId = null)
        {
            if (Status != RequestStatus.Created)
            {
                throw new InvalidOperationException($"Tried to mark request as received by {userId}({impersonatorId}) but it is already {Status}");
            }
            base.Received(receiveTime, userId, impersonatorId);
            Requests.Where(r => r.Status == RequestStatus.Created).ToList().ForEach(r => r.ReceivedInGroup(receiveTime, userId, impersonatorId));
        }

        public void ConfirmDenial(DateTimeOffset confirmedAt, int userId, int? impersonatorId)
        {
            if (Status != RequestStatus.DeniedByCreator)
            {
                throw new InvalidOperationException($"Förfrågan med boknings-id {OrderGroup.OrderGroupNumber} är inte i rätt status för att kunna konfirmera avböjande.");
            }
            AddStatusConfirmations(confirmedAt, userId, impersonatorId);
        }

        public void ConfirmNoAnswer(DateTimeOffset confirmedAt, int userId, int? impersonatorId)
        {
            if (Status != RequestStatus.ResponseNotAnsweredByCreator)
            {
                throw new InvalidOperationException($"Förfrågan med boknings-id {OrderGroup.OrderGroupNumber} är inte i rätt status för att kunna konfirmera obesvarad.");
            }
            AddStatusConfirmations(confirmedAt, userId, impersonatorId);
        }

        public void ConfirmCancellation(DateTimeOffset confirmedAt, int userId, int? impersonatorId)
        {
            if (Status != RequestStatus.CancelledByCreator)
            {
                throw new InvalidOperationException($"Förfrågan med boknings-id {OrderGroup.OrderGroupNumber} är inte i rätt status för att kunna konfirmera avbokad.");
            }
            AddStatusConfirmations(confirmedAt, userId, impersonatorId);
        }

        private void AddStatusConfirmations(DateTimeOffset confirmedAt, int userId, int? impersonatorId)
        {
            Requests.Where(r => !r.RequestStatusConfirmations.Any(rs => rs.RequestStatus == Status) && r.Status == Status).ToList().ForEach(r => r.RequestStatusConfirmations.Add(new RequestStatusConfirmation { ConfirmedBy = userId, ImpersonatingConfirmedBy = impersonatorId, RequestStatus = Status, ConfirmedAt = confirmedAt }));
            StatusConfirmations.Add(new RequestGroupStatusConfirmation { ConfirmedBy = userId, ImpersonatingConfirmedBy = impersonatorId, RequestStatus = Status, ConfirmedAt = confirmedAt });
        }

        internal override void Approve(DateTimeOffset approveTime, int userId, int? impersonatorId)
        {
            if (!IsAwaitingApproval)
            {
                throw new InvalidOperationException($"RequestGroup {RequestGroupId} is {Status}. Only Accepted RequestGroups can be approved");
            }
            base.Approve(approveTime, userId, impersonatorId);
            Requests.ForEach(r => r.Approve(approveTime, userId, impersonatorId));
            OrderGroup.SetStatus(OrderStatus.ResponseAccepted, false);
        }

        internal void Cancel(DateTimeOffset cancelledAt, int userId, int? impersonatorId, string message)
        {
            if (!IsToBeProcessedByBroker)
            {
                throw new InvalidOperationException($"RequestGroup {RequestGroupId} is {Status}. Only Received and Created RequestGroups can be cancelled");
            }
            Requests.ForEach(r => r.Cancel(cancelledAt, userId, impersonatorId, message, isCancelledFromGroup: true));
            Status = RequestStatus.CancelledByCreator;
            CancelledAt = cancelledAt;
            CancelledBy = userId;
            ImpersonatingCanceller = impersonatorId;
            CancelMessage = message;
            OrderGroup.Status = OrderStatus.CancelledByCreator;
        }

        internal void TerminateDueToEndedFrameworkAgreement(DateTimeOffset terminatedAt, string terminationMessage, IEnumerable<RequestStatus> terminatableStatuses)
        {
            if (!terminatableStatuses.Contains(Status))
            {
                throw new InvalidOperationException($"Request group {RequestGroupId} is {Status}. Only reuquests under negotiation can be terminated due to ended framework agreement");
            }
            Requests.ForEach(r => r.TerminateDueToEndedFrameworkAgreement(terminatedAt, terminationMessage, terminatableStatuses));
            Status = NewStatusWhenRequestIsTerminatedDueToEndedFrameworkAgreement;  
            CancelledAt = terminatedAt;
            CancelMessage = terminationMessage;
            OrderGroup.Status = OrderStatus.TerminatedDueToTerminatedFrameworkAgreement;
        }

        public void Answer(DateTimeOffset answerTime, 
            int userId, 
            int? impersonatorId, 
            List<RequestGroupAttachment> attachedFiles, 
            bool hasTravelCosts, 
            bool partialAnswer, 
            DateTimeOffset? latestAnswerTimeForCustomer, 
            string brokerReferenceNumber)
        {
            if (!IsToBeProcessedByBroker)
            {
                throw new InvalidOperationException($"Det gick inte att svara på sammanhållen förfrågan med boknings-id {OrderGroup.OrderGroupNumber}, den har redan blivit besvarad");
            }

            AnswerDate = answerTime;
            AnsweredBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
            Attachments = attachedFiles;
            AnswerProcessedAt = RequiresApproval(hasTravelCosts) ? null : (DateTimeOffset?)answerTime;
            OrderGroup.SetStatus(RequiresApproval(hasTravelCosts) ?
                partialAnswer ? OrderStatus.RequestAwaitingPartialAccept : OrderStatus.RequestRespondedAwaitingApproval :
                partialAnswer ? OrderStatus.GroupAwaitingPartialResponse : OrderStatus.ResponseAccepted, false);
            SetStatus(RequiresApproval(hasTravelCosts) ?
                partialAnswer ? RequestStatus.PartiallyAccepted : RequestStatus.AnsweredAwaitingApproval :
                partialAnswer ? RequestStatus.PartiallyApproved : RequestStatus.Approved, false);
            LatestAnswerTimeForCustomer = latestAnswerTimeForCustomer;
            BrokerReferenceNumber = brokerReferenceNumber;
        }

        public void AnswerAcceptedRequest(
            DateTimeOffset answerTime,
            int userId,
            int? impersonatorId,
            List<RequestGroupAttachment> attachments,
            RequestGroup oldRequestGroup,
            bool hasTravelCosts,
            DateTimeOffset? latestAnswerTimeForCustomer,
            string brokerReferenceNumber,
            bool partialAnswer = false
            )
        {
            if (oldRequestGroup == null)
            {
                throw new ArgumentNullException($"Det gick inte att färdigställa tidigare bekräftad förfrågan med boknings-id {OrderGroup.OrderGroupNumber}, hittar ingen koppling till bekräftelsen");
            }
            AnswerDate = answerTime;
            AnsweredBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
            AnswerProcessedAt = (DateTimeOffset?)answerTime;
            ReceivedBy = oldRequestGroup.ReceivedBy;
            RecievedAt = oldRequestGroup.RecievedAt;
            ImpersonatingReceivedBy = oldRequestGroup.ImpersonatingReceivedBy;
            AcceptedAt = oldRequestGroup.AcceptedAt;
            AcceptedBy = oldRequestGroup.AcceptedBy;
            ImpersonatingAcceptedBy = oldRequestGroup.ImpersonatingAcceptedBy;
            ReplacingRequestGroupId = oldRequestGroup.RequestGroupId;
            Attachments = attachments;
            BrokerReferenceNumber = brokerReferenceNumber;
            LatestAnswerTimeForCustomer = latestAnswerTimeForCustomer;

            AnswerProcessedAt = RequiresApproval(hasTravelCosts) ? null : (DateTimeOffset?)answerTime;
            OrderGroup.SetStatus(RequiresApproval(hasTravelCosts) ?
                partialAnswer ? OrderStatus.RequestAwaitingPartialAccept : OrderStatus.RequestRespondedAwaitingApproval :
                partialAnswer ? OrderStatus.GroupAwaitingPartialResponse : OrderStatus.ResponseAccepted, false);
            SetStatus(RequiresApproval(hasTravelCosts) ?
                partialAnswer ? RequestStatus.PartiallyAccepted : RequestStatus.AnsweredAwaitingApproval :
                partialAnswer ? RequestStatus.PartiallyApproved : RequestStatus.Approved, false);
            LatestAnswerTimeForCustomer = latestAnswerTimeForCustomer;
            BrokerReferenceNumber = brokerReferenceNumber;
        }

        public void Accept(DateTimeOffset acceptTime, int userId, int? impersonatorId, List<RequestGroupAttachment> attachedFiles, bool partialAnswer, string brokerReferenceNumber)
        {
            if (!CanAccept)
            {
                throw new InvalidOperationException($"Det gick inte att bekräfta på sammanhållen förfrågan med boknings-id {OrderGroup.OrderGroupNumber}, den har redan blivit behandlad");
            }
            if (!IsAnswerLevelAccept)
            {
                throw new InvalidOperationException($"Det gick inte att bekräfta på sammanhållen förfrågan med boknings-id {OrderGroup.OrderGroupNumber}, den kräver fullt svar direkt");
            }
            if (partialAnswer)
            {
                throw new InvalidOperationException($"Del-bekräftelse är inte implementerad");
            }

            AcceptedAt = acceptTime;
            AcceptedBy = userId;
            ImpersonatingAcceptedBy = impersonatorId;
            Attachments = attachedFiles;
            OrderGroup.SetStatus(OrderStatus.RequestAcceptedAwaitingInterpreter);
            SetStatus(RequestStatus.AcceptedAwaitingInterpreter, false);
            BrokerReferenceNumber = brokerReferenceNumber;
        }

        public void AddView(int userId, int? impersonatorId, DateTimeOffset swedenNow)
        {
            if (!Views.Any(rv => rv.ViewedBy == userId))
            {
                Views.Add(new RequestGroupView
                {
                    ViewedBy = userId,
                    ImpersonatingViewedBy = impersonatorId,
                    ViewedAt = swedenNow
                });
            }
        }

        internal void SetExpiryManually(DateTimeOffset now, DateTimeOffset expiry, int userId, int? impersonatingUserId)
        {
            if (Status != RequestStatus.AwaitingDeadlineFromCustomer)
            {
                throw new InvalidOperationException($"There is no request awaiting deadline from customer on this order group {OrderGroupId}");
            }
            ExpiresAt = expiry;
            OrderGroup.SetStatus(OrderStatus.Requested);
            SetStatus(RequestStatus.Created);
            RequestGroupUpdateLatestAnswerTime = new RequestGroupUpdateLatestAnswerTime { UpdatedAt = now, UpdatedBy = userId, ImpersonatorUpdatedBy = impersonatingUserId };

        }

        #endregion
    }
}
