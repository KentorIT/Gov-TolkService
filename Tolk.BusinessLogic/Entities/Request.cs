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
        public Request() { }

        public Request(Ranking ranking, DateTimeOffset? expiry, DateTimeOffset creationTime, bool isTerminalRequest = false)
        {
            Ranking = ranking;
            Status = RequestStatus.Created;
            ExpiresAt = expiry;
            CreatedAt = creationTime;
            IsTerminalRequest = isTerminalRequest;
        }
        public Request(Request originalRequest, DateTimeOffset? expiry, DateTimeOffset creationTime)
        {
            Ranking = originalRequest.Ranking;
            Status = RequestStatus.Created;
            ExpiresAt = expiry;
            CreatedAt = creationTime;
            Interpreter = originalRequest.Interpreter;
            CompetenceLevel = originalRequest.CompetenceLevel;
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestId { get; set; }

        public int RankingId { get; set; }

        public RequestStatus Status { get; set; }

        public Ranking Ranking { get; set; }

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }

        /// <summary>
        /// The time (inclusive) when the request is expired. If null, expiry must be manually defined
        /// </summary>
        public DateTimeOffset? ExpiresAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        [MaxLength(1000)]
        public string BrokerMessage { get; set; }

        public int? InterpreterBrokerId { get; set; }

        public int? CompetenceLevel { get; set; }

        public VerificationResult? InterpreterCompetenceVerificationResultOnAssign { get; set; }

        public VerificationResult? InterpreterCompetenceVerificationResultOnStart { get; set; }

        [ForeignKey(nameof(InterpreterBrokerId))]
        public InterpreterBroker Interpreter { get; set; }

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

        public void Received(DateTimeOffset receiveTime, int userId, int? impersonatorId = null)
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

        [MaxLength(1000)]
        public string CancelMessage { get; set; }

        public int? ReplacingRequestId { get; set; }

        [ForeignKey(nameof(ReplacingRequestId))]
        [InverseProperty(nameof(ReplacedByRequest))]
        public Request ReplacingRequest { get; set; }

        /// <summary>
        /// If true, this Request wil not be followed by another to the next broker.
        /// </summary>
        public bool IsTerminalRequest { get; set; }

        #region navigation

        public List<RequestAttachment> Attachments { get; set; }

        public List<OrderRequirementRequestAnswer> RequirementAnswers { get; set; }

        public List<Requisition> Requisitions { get; set; }

        public List<RequestPriceRow> PriceRows { get; set; }

        public List<Complaint> Complaints { get; set; }

        public List<RequestStatusConfirmation> RequestStatusConfirmations { get; set; }

        public List<RequestView> RequestViews { get; set; }

        [InverseProperty(nameof(ReplacingRequest))]
        public Request ReplacedByRequest { get; set; }
        public static object HttpContext { get; set; }

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

        public bool CanApprove
        {
            get { return Status == RequestStatus.Accepted || Status == RequestStatus.AcceptedNewInterpreterAppointed; }
        }

        public bool CanChangeInterpreter
        {
            get { return Status == RequestStatus.Approved || Status == RequestStatus.Accepted || Status == RequestStatus.AcceptedNewInterpreterAppointed; }
        }

        public void Accept(
            DateTimeOffset acceptTime,
            int userId,
            int? impersonatorId,
            InterpreterBroker interpreter,
            InterpreterLocation interpreterLocation,
            CompetenceAndSpecialistLevel competenceLevel,
            List<OrderRequirementRequestAnswer> requirementAnswers,
            List<RequestAttachment> attachedFiles,
            PriceInformation priceInformation, 
            VerificationResult? verificationResult = null)
        {
            if (Status != RequestStatus.Received)
            {
                throw new InvalidOperationException($"Det gick inte att svara på förfrågan med boknings-id {Order.OrderNumber}, den har redan blivit besvarad");
            }
            if (Order.ReplacingOrderId.HasValue)
            {
                throw new InvalidOperationException($"Något gick fel, det gick inte att svara på förfrågan med boknings-id {Order.OrderNumber}. Detta är ett ersättninguppdrag och skulle bli besvarat på annat sätt.");
            }
            ////TODO: Add validation of RequirementAnswers, to make sure that the caller has answered true to all required!!!
            ////Add validation for interperter location
            //if (!Order.InterpreterLocations.Any(l => l.InterpreterLocation == interpreterLocation))
            //{
            //    throw new InvalidOperationException($"Interpreter location {EnumHelper.GetCustomName(interpreterLocation)} is not valid for this order.");
            //}
            ////Add Validation for competencelevel, if required

            bool requiresAccept = Order.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved;
            Status = requiresAccept ? RequestStatus.Accepted : RequestStatus.Approved;
            AnswerDate = acceptTime;
            AnsweredBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
            Interpreter = interpreter ?? throw new InvalidOperationException($"Interpreter is not set.");
            InterpreterLocation = (int?)interpreterLocation;
            CompetenceLevel = (int?)competenceLevel;
            RequirementAnswers.AddRange(requirementAnswers);
            Attachments = attachedFiles;
            PriceRows.AddRange(priceInformation.PriceRows.Select(row => DerivedClassConstructor.Construct<PriceRowBase, RequestPriceRow>(row)));
            InterpreterCompetenceVerificationResultOnAssign = verificationResult;

            Order.Status = requiresAccept ? OrderStatus.RequestResponded : OrderStatus.ResponseAccepted;
        }

        public void AddRequestView(int userId, int? impersonatorId, DateTimeOffset swedenNow)
        {
            if (!RequestViews.Any(rv => rv.RequestId == RequestId && rv.ViewedBy == userId))
            {
                RequestViews.Add(new RequestView
                {
                    ViewedBy = userId,
                    ImpersonatingViewedBy = impersonatorId,
                    ViewedAt = swedenNow
                });
            }
        }

        public void Decline(
            DateTimeOffset declinedAt,
            int userId,
            int? impersonatorId,
            string message)
        {
            if (Status != RequestStatus.Created && Status != RequestStatus.Received)
            {
                throw new InvalidOperationException($"Det gick inte att tacka nej till förfrågan med boknings-id {Order.OrderNumber}, den har redan blivit besvarad");
            }
            Status = RequestStatus.DeclinedByBroker;
            AnswerDate = declinedAt;
            AnsweredBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
            DenyMessage = message;
            if (!Order.ReplacingOrderId.HasValue)
            {
                Order.Status = OrderStatus.Requested;
            }
            else
            {
                Order.Status = OrderStatus.NoBrokerAcceptedOrder;
            }
        }

        public void AcceptReplacementOrder(
            DateTimeOffset acceptTime,
            int userId,
            int? impersonatorId,
            decimal? expectedTravelCosts,
            InterpreterLocation interpreterLocation,
            PriceInformation priceInformation)
        {
            if (Status != RequestStatus.Received)
            {
                throw new InvalidOperationException($"Det gick inte att svara på ersättningsuppdraget, förfrågan med boknings-id {Order.OrderNumber} har redan blivit besvarad");
            }
            if (!Order.ReplacingOrderId.HasValue)
            {
                throw new InvalidOperationException($"Något gick fel, det gick inte att svara på förfrågan med boknings-id {Order.OrderNumber}. Detta är inget ersättninguppdrag och skulle bli besvarat på annat sätt.");
            }

            AnswerDate = acceptTime;
            AnsweredBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
            InterpreterLocation = (int?)interpreterLocation;
            if (Order.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved)
            {
                Status = RequestStatus.Accepted;
                Order.Status = OrderStatus.RequestResponded;
            }
            else
            {
                Status = RequestStatus.Approved;
                Order.Status = OrderStatus.ResponseAccepted;
            }
            PriceRows.AddRange(priceInformation.PriceRows.Select(row => DerivedClassConstructor.Construct<PriceRowBase, RequestPriceRow>(row)));
        }

        public void ReplaceInterpreter(
            DateTimeOffset acceptTime,
            int userId,
            int? impersonatorId,
            InterpreterBroker interperter,
            InterpreterLocation? interpreterLocation,
            CompetenceAndSpecialistLevel? competenceLevel,
            List<OrderRequirementRequestAnswer> requirementAnswers,
            IEnumerable<RequestAttachment> attachments,
            PriceInformation priceInformation,
            bool isAutoAccepted,
            Request oldRequest,
            VerificationResult? verificationResult = null)
        {
            //TODO: Add validation of RequirementAnswers, to make sure that the caller has answered true to all required!!!
            if (Status != RequestStatus.AcceptedNewInterpreterAppointed)
            {
                throw new InvalidOperationException($"Något gick fel, det gick inte att byta tolk på förfrågan med boknings-id {Order.OrderNumber}");
            }
            AnswerDate = acceptTime;
            AnsweredBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
            Interpreter = interperter;
            InterpreterLocation = (int?)interpreterLocation;
            CompetenceLevel = (int?)competenceLevel;
            AnswerProcessedAt = isAutoAccepted ? oldRequest.AnswerProcessedAt : null;
            AnswerProcessedBy = isAutoAccepted ? oldRequest.AnswerProcessedBy : null;
            ImpersonatingAnswerProcessedBy = isAutoAccepted ? oldRequest.ImpersonatingAnswerProcessedBy : null;
            ReceivedBy = oldRequest.ReceivedBy;
            RecievedAt = oldRequest.RecievedAt;
            ImpersonatingReceivedBy = oldRequest.ImpersonatingReceivedBy;
            ReplacingRequestId = oldRequest.RequestId;
            PriceRows = new List<RequestPriceRow>();
            RequirementAnswers = requirementAnswers;
            Attachments = attachments.ToList();
            PriceRows.AddRange(priceInformation.PriceRows.Select(row => DerivedClassConstructor.Construct<PriceRowBase, RequestPriceRow>(row)));
            InterpreterCompetenceVerificationResultOnAssign = verificationResult; 
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

        public void Cancel(DateTimeOffset cancelledAt, int userId, int? impersonatorId, string message, bool createFullCompensationRequisition = false, bool isReplaced = false)
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
            if (Status == RequestStatus.Approved && !isReplaced)
            {
                Requisitions.Add(
                    new Requisition
                    {
                        CreatedAt = cancelledAt,
                        CreatedBy = userId,
                        ImpersonatingCreatedBy = impersonatorId,
                        Message = createFullCompensationRequisition ? "Genererat av systemet, eftersom tillfället avbokades för tätt inpå" : "Genererat av systemet vid avbokning, endast förmedlingsavgift utgår",
                        Status = RequisitionStatus.AutomaticGeneratedFromCancelledOrder,
                        SessionStartedAt = Order.StartAt,
                        SessionEndedAt = Order.EndAt,
                        PriceRows = GetPriceRows(createFullCompensationRequisition)
                    }
                );
            }
            Status = Status == RequestStatus.Approved && !isReplaced ? RequestStatus.CancelledByCreatorWhenApproved : RequestStatus.CancelledByCreator;
            CancelledAt = cancelledAt;
            CancelledBy = userId;
            ImpersonatingCanceller = impersonatorId;
            CancelMessage = message;
            Order.Status = OrderStatus.CancelledByCreator;
        }

        private List<RequisitionPriceRow> GetPriceRows(bool createFullCompensationRequisition)
        {
            var priceRows = createFullCompensationRequisition ? PriceRows : PriceRows.Where(p => p.PriceRowType == PriceRowType.BrokerFee).ToList();
            return priceRows
                .Select(p => new RequisitionPriceRow
                {
                    StartAt = p.StartAt,
                    EndAt = p.EndAt,
                    PriceRowType = p.PriceRowType,
                    PriceListRowId = p.PriceListRowId,
                    Price = p.Price,
                    Quantity = p.Quantity,
                    PriceCalculationChargeId = p.PriceCalculationChargeId,
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
            if (Requisitions.Any(r => r.Status == RequisitionStatus.Reviewed || r.Status == RequisitionStatus.Created))
            {
                throw new InvalidOperationException($"A requisition cannot be created when there are active requisitions.");
            }
            if (Status != RequestStatus.Approved)
            {
                throw new InvalidOperationException($"A requisition cannot be created when request is not approved.");
            }
            Requisitions.Add(requisition);
            //Change status on order accordingly.
            Order.DeliverRequisition();
        }

        public bool CanCreateRequisition
        {
            get { return !(Requisitions.Any(r => r.Status == RequisitionStatus.Reviewed || r.Status == RequisitionStatus.Created) || Status != RequestStatus.Approved); }
        }

        public void CreateComplaint(Complaint complaint)
        {
            if (!CanCreateComplaint)
            {
                throw new InvalidOperationException($"Several complaints cannot be created or request does not have status approved .");
            }
            Complaints.Add(complaint);
        }

        public bool CanCreateComplaint
        {
            get { return !Complaints.Any() && Status == RequestStatus.Approved; }
        }

        public bool CanProcess
        {
            get { return Status == RequestStatus.Created || Status == RequestStatus.Received; }
        }
    }
}
