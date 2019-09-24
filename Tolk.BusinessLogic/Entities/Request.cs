using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Entities
{
    public class Request : RequestBase
    {
        #region constructors

        public Request() { }

        public Request(Ranking ranking, DateTimeOffset? expiry, DateTimeOffset creationTime, bool isTerminalRequest = false)
        {
            if (expiry.HasValue && expiry < creationTime)
            {
                throw new InvalidOperationException("The Request cannot have a expiry befor the creation time.");
            }
            Ranking = ranking;
            Status = RequestStatus.Created;
            ExpiresAt = expiry;
            CreatedAt = creationTime;
            IsTerminalRequest = isTerminalRequest;
        }

        public Request(Ranking ranking, DateTimeOffset creationTime, Quarantine quarantine)
        {
            Ranking = ranking;
            Status = RequestStatus.LostDueToQuarantine;
            CreatedAt = creationTime;
            Quarantine = quarantine;
            QuarantineId = quarantine.QuarantineId;
        }

        public Request(Request originalRequest, DateTimeOffset? expiry, DateTimeOffset creationTime)
            : this(originalRequest.Ranking, expiry, creationTime)
        {
            Interpreter = originalRequest.Interpreter;
            CompetenceLevel = originalRequest.CompetenceLevel;
        }

        #endregion

        #region properties

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestId { get; set; }

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }

        public int? RequestGroupId { get; set; }

        [ForeignKey(nameof(RequestGroupId))]
        public RequestGroup RequestGroup { get; set; }

        public int? InterpreterBrokerId { get; set; }

        public int? CompetenceLevel { get; set; }

        public VerificationResult? InterpreterCompetenceVerificationResultOnAssign { get; set; }

        public VerificationResult? InterpreterCompetenceVerificationResultOnStart { get; set; }

        [ForeignKey(nameof(InterpreterBrokerId))]
        public InterpreterBroker Interpreter { get; set; }

        public int? InterpreterLocation { get; set; }

        public int? ReplacingRequestId { get; set; }

        [MaxLength(1000)]
        public string ExpectedTravelCostInfo { get; set; }

        [ForeignKey(nameof(ReplacingRequestId))]
        [InverseProperty(nameof(ReplacedByRequest))]
        public Request ReplacingRequest { get; set; }

        public int? QuarantineId { get; set; }

        [ForeignKey(nameof(QuarantineId))]
        public Quarantine Quarantine { get; set; }

        #endregion

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

        #endregion

        #region Status Checks

        public bool CanCancel
        {
            get => (Order.Status == OrderStatus.Requested ||
                    Order.Status == OrderStatus.RequestResponded ||
                    Order.Status == OrderStatus.RequestRespondedNewInterpreter ||
                    Order.Status == OrderStatus.ResponseAccepted) &&
                    (IsToBeProcessedByBroker || IsAcceptedOrApproved);
        }

        public bool CanChangeInterpreter(DateTimeOffset swedenNow)
        {
            return IsAcceptedOrApproved && Order.StartAt > swedenNow;
        }

        public bool CanCreateRequisition
        {
            get => !(Requisitions.Any(r => r.Status == RequisitionStatus.Reviewed || r.Status == RequisitionStatus.Created) || Status != RequestStatus.Approved);
        }

        public void CreateComplaint(Complaint complaint)
        {
            if (!CanCreateComplaint)
            {
                throw new InvalidOperationException("Several complaints cannot be created or request does not have status approved.");
            }
            Complaints.Add(complaint);
        }

        public bool CanCreateComplaint
        {
            get => !Complaints.Any() && Status == RequestStatus.Approved;
        }

        #endregion

        #region Methods

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

        public void Approve(DateTimeOffset approveTime, int userId, int? impersonatorId)
        {
            if (!IsAccepted)
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
            InterpreterBroker interpreter,
            InterpreterLocation interpreterLocation,
            CompetenceAndSpecialistLevel competenceLevel,
            List<OrderRequirementRequestAnswer> requirementAnswers,
            List<RequestAttachment> attachedFiles,
            PriceInformation priceInformation,
            string expectedTravelCostInfo,
            VerificationResult? verificationResult = null
            )
        {
            if (!IsToBeProcessedByBroker)
            {
                throw new InvalidOperationException($"Det gick inte att svara på förfrågan med boknings-id {Order.OrderNumber}, den har redan blivit besvarad");
            }
            if (Order.ReplacingOrderId.HasValue)
            {
                throw new InvalidOperationException($"Något gick fel, det gick inte att svara på förfrågan med boknings-id {Order.OrderNumber}. Detta är ett ersättninguppdrag och skulle bli besvarat på annat sätt.");
            }

            ValidateAgainstOrder(interpreterLocation, competenceLevel, requirementAnswers);

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
            ExpectedTravelCostInfo = expectedTravelCostInfo;
            Status = RequiresAccept ? RequestStatus.Accepted : RequestStatus.Approved;
            Order.Status = RequiresAccept ? OrderStatus.RequestResponded : OrderStatus.ResponseAccepted;
        }

        public void ConfirmDenial(DateTimeOffset confirmedAt, int userId, int? impersonatorId)
        {
            if (Status != RequestStatus.DeniedByCreator)
            {
                throw new InvalidOperationException($"Förfrågan med boknings-id {Order.OrderNumber} är inte i rätt status för att kunna konfirmeras.");
            }

            RequestStatusConfirmations.Add(new RequestStatusConfirmation { ConfirmedBy = userId, ImpersonatingConfirmedBy = impersonatorId, RequestStatus = Status, ConfirmedAt = confirmedAt });
        }

        public void ConfirmCancellation(DateTimeOffset confirmedAt, int userId, int? impersonatorId)
        {
            if (Status != RequestStatus.CancelledByCreatorWhenApproved && Status != RequestStatus.CancelledByCreator)
            {
                throw new InvalidOperationException($"Förfrågan med boknings-id {Order.OrderNumber} är inte i rätt status för att kunna konfirmeras.");
            }

            RequestStatusConfirmations.Add(new RequestStatusConfirmation { ConfirmedBy = userId, ImpersonatingConfirmedBy = impersonatorId, RequestStatus = Status, ConfirmedAt = confirmedAt });
        }

        public bool RequiresAccept => Order.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved &&
            InterpreterLocation.HasValue && (InterpreterLocation.Value == (int)Enums.InterpreterLocation.OffSiteDesignatedLocation || InterpreterLocation.Value == (int)Enums.InterpreterLocation.OnSite);

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
            if (!CanDecline)
            {
                throw new InvalidOperationException($"Det gick inte att tacka nej till förfrågan med boknings-id {Order.OrderNumber}, den har redan blivit besvarad");
            }
            Status = RequestStatus.DeclinedByBroker;
            AnswerDate = declinedAt;
            AnsweredBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
            DenyMessage = message;
            Order.Status = !Order.ReplacingOrderId.HasValue ? OrderStatus.Requested : OrderStatus.NoBrokerAcceptedOrder;
        }

        public void AcceptReplacementOrder(
            DateTimeOffset acceptTime,
            int userId,
            int? impersonatorId,
            string expectedTravelCostInfo,
            InterpreterLocation interpreterLocation,
            PriceInformation priceInformation)
        {
            if (!IsToBeProcessedByBroker)
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
            ExpectedTravelCostInfo = expectedTravelCostInfo;
            if (RequiresAccept)
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
            InterpreterLocation interpreterLocation,
            CompetenceAndSpecialistLevel competenceLevel,
            List<OrderRequirementRequestAnswer> requirementAnswers,
            IEnumerable<RequestAttachment> attachments,
            PriceInformation priceInformation,
            bool isAutoAccepted,
            Request oldRequest,
            string expectedTravelCostInfo,
            VerificationResult? verificationResult = null)
        {
            if (Status != RequestStatus.AcceptedNewInterpreterAppointed)
            {
                throw new InvalidOperationException($"Något gick fel, det gick inte att byta tolk på förfrågan med boknings-id {Order.OrderNumber}");
            }
            ValidateAgainstOrder(interpreterLocation, competenceLevel, requirementAnswers);

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
            ExpectedTravelCostInfo = expectedTravelCostInfo;
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
            if (!CanDeny)
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
            if (!CanCancel)
            {
                throw new InvalidOperationException($"Order {OrderId} is {Order.Status}, and request {RequestId} is {Status}. Only Orders waiting to be delivered with active requests can be cancelled");
            }
            if (Order.StartAt < cancelledAt)
            {
                throw new InvalidOperationException($"Order {OrderId} has already passed its start time. Orders that has started cannot be cancelled");
            }
            if (Status == RequestStatus.Approved && !isReplaced)
            {
                Requisitions.Add(
                    new Requisition
                    {
                        CreatedAt = cancelledAt,
                        CreatedBy = userId,
                        ImpersonatingCreatedBy = impersonatorId,
                        Message = createFullCompensationRequisition ? "Genererad av systemet. Full ersättning utgår pga att avbokning mindre än 48 timmar före bokat tolktillfälle" : "Genererat av systemet vid avbokning, endast förmedlingsavgift utgår",
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

        #endregion

        #region private methods

        private void ValidateAgainstOrder(InterpreterLocation interpreterLocation, CompetenceAndSpecialistLevel competenceLevel, List<OrderRequirementRequestAnswer> requirementAnswers)
        {
            ValidateRequirements(Order.Requirements, requirementAnswers);

            if (!Order.InterpreterLocations.Any(l => l.InterpreterLocation == interpreterLocation))
            {
                throw new InvalidOperationException($"Interpreter location {EnumHelper.GetCustomName(interpreterLocation)} is not valid for this order.");
            }

            if (Order.SpecificCompetenceLevelRequired && !Order.CompetenceRequirements.Any(c => c.CompetenceLevel == competenceLevel))
            {
                throw new InvalidOperationException($"Specified competence level {EnumHelper.GetCustomName(competenceLevel)} is not valid for this order.");
            }
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

        private void ValidateRequirements(List<OrderRequirement> requirements, List<OrderRequirementRequestAnswer> requirementAnswers)
        {
            if (requirements.Count() != requirementAnswers.Count() ||
                !requirements.OrderBy(r => r.OrderRequirementId).Select(r => r.OrderRequirementId).SequenceEqual(requirementAnswers.OrderBy(r => r.OrderRequirementId).Select(a => a.OrderRequirementId)))
            {
                throw new InvalidOperationException($"The set of requirement answers does not match the set of requirements");
            }
            if (requirements.Any(r => r.IsRequired &&
                 requirementAnswers.Any(a => a.OrderRequirementId == r.OrderRequirementId &&
                     !a.CanSatisfyRequirement)))
            {
                throw new InvalidOperationException($"Negative answer on required requirement");
            }
        }

        #endregion
    }
}
