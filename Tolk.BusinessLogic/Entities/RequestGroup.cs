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

        private RequestGroup() { }

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

        #endregion

        #region Methods

        public Request FirstRequest => Requests.First();

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
                base.Status = value;
            }
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
            OrderGroup.SetStatus(OrderStatus.Requested);
        }

        public bool HasExtraInterpreter => OrderGroup.Orders.Any(o => o.IsExtraInterpreterForOrderId != null);

        public bool RequiresAccept =>
            OrderGroup.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved &&
            FirstRequest.InterpreterLocation.HasValue &&
            (FirstRequest.InterpreterLocation.Value == (int)Enums.InterpreterLocation.OffSiteDesignatedLocation ||
                FirstRequest.InterpreterLocation.Value == (int)Enums.InterpreterLocation.OnSite);

        public void ConfirmDenial(DateTimeOffset confirmedAt, int userId, int? impersonatorId)
        {
            if (Status != RequestStatus.DeniedByCreator)
            {
                throw new InvalidOperationException($"Förfrågan med boknings-id {OrderGroup.OrderGroupNumber} är inte i rätt status för att kunna konfirmeras.");
            }
            StatusConfirmations.Add(new RequestGroupStatusConfirmation { ConfirmedBy = userId, ImpersonatingConfirmedBy = impersonatorId, RequestStatus = Status, ConfirmedAt = confirmedAt });
        }

        public void Approve(DateTimeOffset approveTime, int userId, int? impersonatorId)
        {
            if (!IsAccepted)
            {
                throw new InvalidOperationException($"Request {RequestGroupId} is {Status}. Only Accepted requests can be approved");
            }

            Status = RequestStatus.Approved;
            OrderGroup.SetStatus(OrderStatus.ResponseAccepted);
            AnswerProcessedAt = approveTime;
            AnswerProcessedBy = userId;
            ImpersonatingAnswerProcessedBy = impersonatorId;
        }

        public void Accept(DateTimeOffset acceptTime, int userId, int? impersonatorId, List<RequestGroupAttachment> attachedFiles)
        {
            if (!IsToBeProcessedByBroker)
            {
                throw new InvalidOperationException($"Det gick inte att svara på sammanhållen förfrågan med boknings-id {OrderGroup.OrderGroupNumber}, den har redan blivit besvarad");
            }

            AnswerDate = acceptTime;
            AnsweredBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
            Attachments = attachedFiles;
            AnswerProcessedAt = RequiresAccept ? null : (DateTimeOffset?)acceptTime;
            OrderGroup.SetStatus(RequiresAccept ? OrderStatus.RequestResponded : OrderStatus.ResponseAccepted);
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
