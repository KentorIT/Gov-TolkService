using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Entities
{
    public class Request
    {
        private Request() { }

        public Request(Ranking ranking, DateTimeOffset expiry)
        {
            Ranking = ranking;
            Status = RequestStatus.Created;
            ExpiresAt = expiry;
        }
        public Request(Request replacingRequest, DateTimeOffset expiry)
        {
            RankingId = replacingRequest.RankingId;
            Status = RequestStatus.Created;
            ExpiresAt = expiry;
            InterpreterId = replacingRequest.InterpreterId;
            CompetenceLevel = replacingRequest.CompetenceLevel;
            ExpectedTravelCosts = replacingRequest.ExpectedTravelCosts;
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestId { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal? ExpectedTravelCosts { get; set; }

        public int RankingId { get; set; }

        public RequestStatus Status { get; set; }

        public Ranking Ranking { get; set; }

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }

        /// <summary>
        /// The time (inclusive) when the request is expired.
        /// </summary>
        public DateTimeOffset ExpiresAt { get; set; }

        [MaxLength(1000)]
        public string BrokerMessage { get; set; }

        public int? InterpreterId { get; set; }

        public int? CompetenceLevel { get; set; }

        [ForeignKey(nameof(InterpreterId))]
        public Interpreter Interpreter { get; set; }

        [MaxLength(1000)]
        public string DenyMessage { get; set; }

        public DateTimeOffset? RecievedAt { get; set; }

        public int? InterpreterLocation { get; set; }

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

        public void Received(DateTimeOffset receiveTime, int userId, int? impersonatorId)
        {
            if (Status != RequestStatus.Created)
            {
                throw new InvalidOperationException($"Tried to mark request {RequestId} as received by {userId}({impersonatorId}) but it is already {Status}");
            }

            Status = RequestStatus.Received;
            RecievedAt = receiveTime;
            ReceivedBy = userId;
            ImpersonatingReceivedBy = impersonatorId;
        }

        [ForeignKey(nameof(ImpersonatingAnswerProcessedBy))]
        public AspNetUser AnswerProcessedByImpersonator { get; private set; }

        public DateTimeOffset? CancelledAt { get; set; }

        public int? CancelledBy { get; set; }

        [ForeignKey(nameof(CancelledBy))]
        public AspNetUser CancelledByUser { get; set; }

        public int? ImpersonatingCanceller { get; set; }

        [ForeignKey(nameof(ImpersonatingCanceller))]
        public AspNetUser CancelledByImpersonator { get; set; }

        public DateTimeOffset? CancelConfirmedAt { get; set; }

        public int? CancelConfirmedBy { get; set; }

        [ForeignKey(nameof(CancelConfirmedBy))]
        public AspNetUser CancelConfirmedByUser { get; set; }

        public int? ImpersonatingCancelConfirmer { get; set; }

        [ForeignKey(nameof(ImpersonatingCancelConfirmer))]
        public AspNetUser CancelConfirmedByImpersonator { get; set; }

        [MaxLength(1000)]
        public string CancelMessage { get; set; }

        #region navigation

        public List<OrderRequirementRequestAnswer> RequirementAnswers { get; set; }

        public List<Requisition> Requisitions { get; set; }

        public List<RequestPriceRow> PriceRows { get; set; }

        public List<Complaint> Complaints { get; set; }

        #endregion

        public void Approve(DateTimeOffset approveTime, int userId, int? impersonatorId)
        {
            if (Status != RequestStatus.Accepted && Status != RequestStatus.AcceptedNewInterpreterAppointed)
            {
                throw new InvalidOperationException($"Request {RequestId} is {Status}. Only Accepted requests can be approved");
            }

            var approvedRequest = Order.Requests.FirstOrDefault(r => r.Status == RequestStatus.Approved);
            if (approvedRequest != null)
            {
                throw new InvalidOperationException($"Can only approve one request for an order. Order {OrderId} already has an approved request {approvedRequest.RequestId}.");
            }

            Status = RequestStatus.Approved;
            Order.Status = OrderStatus.ResponseAccepted;
            AnswerProcessedAt = approveTime;
            AnswerProcessedBy = userId;
            ImpersonatingAnswerProcessedBy = impersonatorId;
        }

        public void Accept(
            DateTimeOffset acceptTime,
            int userId,
            int? impersonatorId,
            int interperterId,
            decimal? expectedTravelCosts,
            Enums.InterpreterLocation? interpreterLocation,
            CompetenceAndSpecialistLevel? competenceLevel,
            IEnumerable<OrderRequirementRequestAnswer> requirementAnswers,
            PriceInformation priceInformation)
        {
            if (Status != RequestStatus.Received)
            {
                throw new InvalidOperationException($"Request {RequestId} is {Status}. Only Received requests can be accepted.");
            }

            Status = RequestStatus.Accepted;
            AnswerDate = acceptTime;
            AnsweredBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
            InterpreterId = interperterId;
            ExpectedTravelCosts = expectedTravelCosts;
            InterpreterLocation = (int?)interpreterLocation;
            CompetenceLevel = (int?)competenceLevel;

            RequirementAnswers.AddRange(requirementAnswers);
            foreach (var row in priceInformation.PriceRows)
            {
                PriceRows.Add(new RequestPriceRow
                {
                    StartAt = row.StartAt,
                    EndAt = row.EndAt,
                    IsBrokerFee = row.IsBrokerFee,
                    PriceListRowId = row.PriceListRowId,
                    TotalPrice = row.TotalPrice
                });
            }

            Order.Status = OrderStatus.RequestResponded;
        }

        public void ReplaceInterpreter(
            DateTimeOffset acceptTime,
            int userId,
            int? impersonatorId,
            int interperterId,
            decimal? expectedTravelCosts,
            Enums.InterpreterLocation? interpreterLocation,
            CompetenceAndSpecialistLevel? competenceLevel,
            IEnumerable<OrderRequirementRequestAnswer> requirementAnswers,
            PriceInformation priceInformation,
            bool isAutoAccepted,
            Request oldRequest)
        {
            if (Status != RequestStatus.AcceptedNewInterpreterAppointed)
            {
                throw new InvalidOperationException($"Request {RequestId} is {Status}. Only AcceptedNewInterpreter requests can be replaced by new interpreter.");
            }
            AnswerDate = acceptTime;
            AnsweredBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
            InterpreterId = interperterId;
            ExpectedTravelCosts = expectedTravelCosts;
            InterpreterLocation = (int?)interpreterLocation;
            CompetenceLevel = (int?)competenceLevel;
            AnswerProcessedAt = isAutoAccepted ? oldRequest.AnswerProcessedAt : null;
            AnswerProcessedBy = isAutoAccepted ? oldRequest.AnswerProcessedBy : null;
            ImpersonatingAnswerProcessedBy = isAutoAccepted ? oldRequest.ImpersonatingAnswerProcessedBy : null;
            ReceivedBy = oldRequest.ReceivedBy;
            RecievedAt = oldRequest.RecievedAt;
            ImpersonatingReceivedBy = oldRequest.ImpersonatingReceivedBy;
            PriceRows = new List<RequestPriceRow>();
            RequirementAnswers = new List<OrderRequirementRequestAnswer>(requirementAnswers);
            foreach (var row in priceInformation.PriceRows)
            {
                PriceRows.Add(new RequestPriceRow
                {
                    StartAt = row.StartAt,
                    EndAt = row.EndAt,
                    IsBrokerFee = row.IsBrokerFee,
                    PriceListRowId = row.PriceListRowId,
                    TotalPrice = row.TotalPrice
                });
            }
            //if old request already was approved by customer
            if (oldRequest.Status == RequestStatus.Approved)
            {
                if (!isAutoAccepted)
                {
                    Order.Status = OrderStatus.RequestRespondedNewInterpreter;
                }
                else
                {
                    Status = oldRequest.Status;
                }
            }
        }

        public void Deny(DateTimeOffset denyTime, int userId, int? impersonatorId, string message)
        {
            if (Status != RequestStatus.Accepted && Status != RequestStatus.AcceptedNewInterpreterAppointed)
            {
                throw new InvalidOperationException($"Request {RequestId} is {Status}. Only Accepted requests can be denied.");
            }

            Status = RequestStatus.DeniedByCreator;
            AnswerProcessedAt = denyTime;
            AnswerProcessedBy = userId;
            ImpersonatingAnswerProcessedBy = impersonatorId;
            Order.Status = OrderStatus.Requested;
            DenyMessage = message;
        }

        public void Cancel(DateTimeOffset cancelledAt, int userId, int? impersonatorId, string message, bool createFullCompensationRequisition)
        {
            if (Order.Status != OrderStatus.Requested && Order.Status != OrderStatus.RequestResponded && Order.Status != OrderStatus.RequestRespondedNewInterpreter && Order.Status != OrderStatus.ResponseAccepted)
            {
                throw new InvalidOperationException($"Order {OrderId} is {Order.Status}. Only Orders waiting to be delivered can be cancelled");
            }
            if (Order.StartAt < cancelledAt)
            {
                throw new InvalidOperationException($"Order {OrderId} has already passed its start time. Orders that has started cannot be cancelled");
            }
            if (Status != RequestStatus.Created && Status != RequestStatus.Received && Status != RequestStatus.Accepted && Status != RequestStatus.Approved && Status != RequestStatus.AcceptedNewInterpreterAppointed)
            {
                throw new InvalidOperationException($"Request {RequestId} is {Status}. Only active requests can be cancelled.");
            }
            if (Status == RequestStatus.Approved)
            {
                Requisitions.Add(
                    new Requisition
                    {
                        CreatedAt = cancelledAt,
                        CreatedBy = userId,
                        ImpersonatingCreatedBy = impersonatorId,
                        Message = createFullCompensationRequisition ? "Genererat av systemet, eftersom tillfället avbokades för tätt inpå" : "Genererat av systemet vid avbokning, endast förmedlingsavgift utgår",
                        Status = RequisitionStatus.AutomaticApprovalFromCancelledOrder,
                        SessionStartedAt = Order.StartAt,
                        SessionEndedAt = Order.EndAt,
                        PriceRows = GetPriceRows(createFullCompensationRequisition)
                    }
                );
            }
            Status = Status == RequestStatus.Approved ? RequestStatus.CancelledByCreatorWhenApproved : RequestStatus.CancelledByCreator;
            CancelledAt = cancelledAt;
            CancelledBy = userId;
            ImpersonatingCanceller = impersonatorId;
            CancelMessage = message;
            Order.Status = OrderStatus.CancelledByCreator;
        }
        private List<RequisitionPriceRow> GetPriceRows(bool createFullCompensationRequisition)
        {
            var priceRows = createFullCompensationRequisition ? PriceRows : PriceRows.Where(p => p.IsBrokerFee).ToList();
            return priceRows
                .Select(p => new RequisitionPriceRow
                {
                    StartAt = p.StartAt,
                    EndAt = p.EndAt,
                    IsBrokerFee = p.IsBrokerFee,
                    PriceListRowId = p.PriceListRowId,
                    TotalPrice = p.TotalPrice
                }).ToList();
        }

        public void CancelByBroker(DateTimeOffset cancelledAt, int userId, int? impersonatorId, string cancelMessage)
        {
            if (Order.Status != OrderStatus.ResponseAccepted)
            {
                throw new InvalidOperationException($"Order {OrderId} is {Order.Status}. Only Orders where response is accepted can be cancelled by broker.");
            }
            if (Order.StartAt < cancelledAt)
            {
                throw new InvalidOperationException($"Order {OrderId} has already passed its start time. Orders that has started can not be cancelled");
            }
            if (Status != RequestStatus.Approved)
            {
                throw new InvalidOperationException($"Request {RequestId} is {Status}. Only approved requests can be cancelled.");
            }

            Status = RequestStatus.CancelledByBroker;
            CancelledAt = cancelledAt;
            CancelledBy = userId;
            ImpersonatingCanceller = impersonatorId;
            CancelMessage = cancelMessage;
            Order.Status = OrderStatus.CancelledByBroker;
        }

        public void CreateRequisition(Requisition requisition)
        {
            if (Requisitions.Any(r => r.Status == RequisitionStatus.Approved || r.Status == RequisitionStatus.Created))
            {
                throw new InvalidOperationException($"A requisition cannot be created when there are active requisitions.");
            }

            Requisitions.Add(requisition);
            //Change status on order accordingly.
            Order.DeliverRequisition();
        }

        public void CreateComplaint(Complaint complaint)
        {
            if (Complaints.Any())
            {
                throw new InvalidOperationException($"Several complaints cannot be created.");
            }

            Complaints.Add(complaint);
        }
    }
}
