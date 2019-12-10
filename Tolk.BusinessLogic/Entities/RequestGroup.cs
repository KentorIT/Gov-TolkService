using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestGroup : RequestBase
    {
        #region constructors

        internal RequestGroup() { }

        internal RequestGroup(Ranking ranking, DateTimeOffset? expiry, DateTimeOffset creationTime, List<Request> requests, bool isTerminalRequest = false)
        {
            requests.ForEach(r => r.RequestGroup = this);
            Ranking = ranking;
            Status = RequestStatus.Created;
            ExpiresAt = expiry;
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

        #endregion

        #region navigation

        public List<Request> Requests { get; set; }

        public List<RequestGroupStatusConfirmation> StatusConfirmations { get; set; }

        public List<RequestGroupView> Views { get; set; }

        public List<RequestGroupAttachment> Attachments { get; set; }

        public RequestGroupUpdateLatestAnswerTime RequestGroupUpdateLatestAnswerTime { get; set; }

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

        public override void Deny(DateTimeOffset denyTime, int userId, int? impersonatorId, string message)
        {
            if (!CanDeny)
            {
                throw new InvalidOperationException($"RequestGRoup {RequestGroupId} is {Status}. Only Accepted request groups can be denied.");
            }
            base.Deny(denyTime, userId, impersonatorId, message);
            Requests.ForEach(r => r.Deny(denyTime, userId, impersonatorId, message));
            OrderGroup.SetStatus(OrderStatus.Requested, false);
        }

        public override void Received(DateTimeOffset receiveTime, int userId, int? impersonatorId = null)
        {
            if (Status != RequestStatus.Created)
            {
                throw new InvalidOperationException($"Tried to mark request as received by {userId}({impersonatorId}) but it is already {Status}");
            }

            base.Received(receiveTime, userId, impersonatorId);
            Requests.ForEach(r => r.ReceivedInGroup(receiveTime, userId, impersonatorId));
        }

        public void ConfirmDenial(DateTimeOffset confirmedAt, int userId, int? impersonatorId)
        {
            if (Status != RequestStatus.DeniedByCreator)
            {
                throw new InvalidOperationException($"Förfrågan med boknings-id {OrderGroup.OrderGroupNumber} är inte i rätt status för att kunna konfirmeras.");
            }
            StatusConfirmations.Add(new RequestGroupStatusConfirmation { ConfirmedBy = userId, ImpersonatingConfirmedBy = impersonatorId, RequestStatus = Status, ConfirmedAt = confirmedAt });
        }

        public override void Approve(DateTimeOffset approveTime, int userId, int? impersonatorId)
        {
            if (!IsAccepted)
            {
                throw new InvalidOperationException($"Request {RequestGroupId} is {Status}. Only Accepted requests can be approved");
            }
            base.Approve(approveTime, userId, impersonatorId);
            Requests.ForEach(r => r.Approve(approveTime, userId, impersonatorId));
            OrderGroup.SetStatus(OrderStatus.ResponseAccepted, false);
        }

        public void Accept(DateTimeOffset acceptTime, int userId, int? impersonatorId, List<RequestGroupAttachment> attachedFiles, bool hasTravelCosts, bool partialAnswer)
        {
            if (!IsToBeProcessedByBroker)
            {
                throw new InvalidOperationException($"Det gick inte att svara på sammanhållen förfrågan med boknings-id {OrderGroup.OrderGroupNumber}, den har redan blivit besvarad");
            }

            AnswerDate = acceptTime;
            AnsweredBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
            Attachments = attachedFiles;
            AnswerProcessedAt = RequiresApproval(hasTravelCosts) ? null : (DateTimeOffset?)acceptTime;
            OrderGroup.SetStatus(RequiresApproval(hasTravelCosts) ? 
                partialAnswer ? OrderStatus.RequestAwaitingPartialAccept : OrderStatus.RequestResponded :
                partialAnswer ? OrderStatus.GroupAwaitingPartialResponse : OrderStatus.ResponseAccepted, false);
            SetStatus(RequiresApproval(hasTravelCosts) ?
                partialAnswer ? RequestStatus.PartiallyAccepted : RequestStatus.Accepted :
                partialAnswer ? RequestStatus.PartiallyApproved : RequestStatus.Approved, false);
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

        #endregion
    }
}
