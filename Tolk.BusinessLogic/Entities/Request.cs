﻿using System;
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

        public Request(Ranking ranking, DateTimeOffset? expiry, DateTimeOffset creationTime, bool isTerminalRequest = false, bool isChangeInterpreter = false, RequestGroup requestGroup = null)
        {
            if (!isChangeInterpreter && expiry.HasValue && expiry < creationTime)
            {
                throw new InvalidOperationException("The Request cannot have an expiry before the creation time.");
            }
            Ranking = ranking;
            Status = RequestStatus.Created;
            ExpiresAt = expiry;
            CreatedAt = creationTime;
            IsTerminalRequest = isTerminalRequest;
            RequestGroup = requestGroup;
        }

        internal Request(Ranking ranking, DateTimeOffset creationTime, Quarantine quarantine)
        {
            Ranking = ranking;
            Status = RequestStatus.LostDueToQuarantine;
            CreatedAt = creationTime;
            Quarantine = quarantine;
            QuarantineId = quarantine.QuarantineId;
        }

        internal Request(Request originalRequest, DateTimeOffset? expiry, DateTimeOffset creationTime)
            : this(originalRequest.Ranking, expiry, creationTime)
        {
            Interpreter = originalRequest.Interpreter;
            CompetenceLevel = originalRequest.CompetenceLevel;
            InterpreterCompetenceVerificationResultOnAssign = originalRequest.InterpreterCompetenceVerificationResultOnAssign;
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

        public override RequestStatus Status
        {
            get => base.Status;
            set
            {
                if (value == RequestStatus.PartiallyAccepted)
                {
                    throw new InvalidOperationException($"A {nameof(Request)} cannot be set to {nameof(RequestStatus.PartiallyAccepted)}");
                }
                base.Status = value;
            }
        }

        public bool? CompletedNotificationIsHandled { get; set; }

        #endregion

        #region navigation

        public List<RequestAttachment> Attachments { get; set; }

        public List<OrderRequirementRequestAnswer> RequirementAnswers { get; set; }

        public List<Requisition> Requisitions { get; set; }

        public List<RequestPriceRow> PriceRows { get; set; }

        public List<Complaint> Complaints { get; set; }

        public List<RequestStatusConfirmation> RequestStatusConfirmations { get; set; }

        public RequestUpdateLatestAnswerTime RequestUpdateLatestAnswerTime { get; set; }

        public List<RequestView> RequestViews { get; set; }

        [InverseProperty(nameof(ReplacingRequest))]
        public Request ReplacedByRequest { get; set; }

        #endregion

        #region Status Checks

        public bool CanCancel => CanCancelRequestBelongsToGroup || CanCancelRequestNotBelongsToGroup;

        public bool CanCancelFromGroup => Order.Status == OrderStatus.Requested && IsToBeProcessedByBroker && Order.OrderGroupId.HasValue;

        private bool CanCancelRequestNotBelongsToGroup => !Order.OrderGroupId.HasValue &&
            (Order.Status == OrderStatus.Requested || Order.Status == OrderStatus.RequestResponded
            || Order.Status == OrderStatus.RequestRespondedNewInterpreter || Order.Status == OrderStatus.ResponseAccepted) &&
            (IsToBeProcessedByBroker || IsAcceptedOrApproved);

        private bool CanCancelRequestBelongsToGroup => Order.OrderGroupId.HasValue &&
            (Order.Status == OrderStatus.RequestRespondedNewInterpreter || Order.Status == OrderStatus.ResponseAccepted) &&
            (Status == RequestStatus.Approved || Status == RequestStatus.AcceptedNewInterpreterAppointed);

        public bool CanCreateReplacementOrderOnCancel => !Order.OrderGroupId.HasValue && !Order.ReplacingOrderId.HasValue && Status == RequestStatus.Approved;

        public bool CanChangeInterpreter(DateTimeOffset swedenNow) => Order.StartAt > swedenNow &&
            ((!RequestGroupId.HasValue && IsAcceptedOrApproved) ||
            (RequestGroupId.HasValue && (Status == RequestStatus.Approved || Status == RequestStatus.AcceptedNewInterpreterAppointed)));

        public bool CanCreateRequisition => !Requisitions.Any(r => r.Status == RequisitionStatus.Reviewed || r.Status == RequisitionStatus.Created) && IsApprovedOrDelivered;

        public bool CanCreateComplaint(DateTimeOffset swedenNow) => !Complaints.Any() && HasCorrectStatusForCreateComplaint && !(IsApprovedOrDelivered && Order.StartAt > swedenNow);

        public bool HasCorrectStatusForCreateComplaint => IsApprovedOrDelivered || Status == RequestStatus.CancelledByBroker;

        public bool IsApprovedOrDelivered => (Status == RequestStatus.Approved || Status == RequestStatus.Delivered);

        public bool TerminateOnDenial => Status == RequestStatus.AcceptedNewInterpreterAppointed && RequestGroupId.HasValue;

        public bool RequiresAccept => Order.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved && InterpreterLocation.HasValue
            && (InterpreterLocation.Value == (int)Enums.InterpreterLocation.OffSiteDesignatedLocation || InterpreterLocation.Value == (int)Enums.InterpreterLocation.OnSite)
            && ((PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0) > 0);

        #endregion

        #region Methods

        public void CreateComplaint(Complaint complaint, DateTimeOffset now)
        {
            if (!CanCreateComplaint(now))
            {
                throw new InvalidOperationException("Det gick inte att skapa reklamation");
            }
            Complaints.Add(complaint);
        }

        internal override void Received(DateTimeOffset receiveTime, int userId, int? impersonatorId = null)
        {
            if (Status != RequestStatus.Created)
            {
                throw new InvalidOperationException($"Tried to mark request as received by {userId}({impersonatorId}) but it is already {Status}");
            }

            base.Received(receiveTime, userId, impersonatorId);
        }

        internal void ReceivedInGroup(DateTimeOffset receiveTime, int userId, int? impersonatorId = null)
        {
            if (Order.OrderGroupId == null)
            {
                throw new InvalidOperationException($"Beställningen {Order.OrderNumber}, tillhör inte en sammanhållen bokning. Anropa rätt metod för detta.");
            }

            Received(receiveTime, userId, impersonatorId);
        }

        internal override void Approve(DateTimeOffset approveTime, int userId, int? impersonatorId)
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
            base.Approve(approveTime, userId, impersonatorId);
            Order.Status = OrderStatus.ResponseAccepted;
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
            DateTimeOffset? latestAnswerTimeForCustomer,
            string brokerReferenceNumber,
            VerificationResult? verificationResult = null,
            bool overrideRequireAccept = false
            )
        {
            if (priceInformation == null)
            {
                throw new ArgumentNullException($"Det gick inte att svara på förfrågan med boknings-id {Order.OrderNumber} prisrader saknas");
            }
            if (!IsToBeProcessedByBroker)
            {
                throw new InvalidOperationException($"Det gick inte att svara på förfrågan med boknings-id {Order.OrderNumber}, den har redan blivit besvarad");
            }
            if (Order.ReplacingOrderId.HasValue)
            {
                throw new InvalidOperationException($"Något gick fel, det gick inte att svara på förfrågan med boknings-id {Order.OrderNumber}. Detta är ett ersättninguppdrag och skulle bli besvarat på annat sätt.");
            }
            ValidateInterpreterLocationAgainstOrder(interpreterLocation);
            ValidateRequirementsAgainstOrder(competenceLevel, requirementAnswers);
            ValidateLatestAnswerTimeAndTravelCost(interpreterLocation, priceInformation, latestAnswerTimeForCustomer, acceptTime);
            AnswerDate = acceptTime;
            AnsweredBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
            Interpreter = interpreter ?? throw new InvalidOperationException("Interpreter is not set.");
            InterpreterLocation = (int?)interpreterLocation;
            CompetenceLevel = (int?)competenceLevel;
            RequirementAnswers = requirementAnswers;
            Attachments = attachedFiles;
            PriceRows = priceInformation.PriceRows.Select(row => DerivedClassConstructor.Construct<PriceRowBase, RequestPriceRow>(row)).ToList();
            InterpreterCompetenceVerificationResultOnAssign = verificationResult;
            BrokerReferenceNumber = brokerReferenceNumber;
            ExpectedTravelCostInfo = expectedTravelCostInfo;
            LatestAnswerTimeForCustomer = latestAnswerTimeForCustomer;

            var requiresAccept = overrideRequireAccept || RequiresAccept;

            Status = requiresAccept ? RequestStatus.Accepted : RequestStatus.Approved;
            Order.Status = requiresAccept ? OrderStatus.RequestResponded : OrderStatus.ResponseAccepted;
            AnswerProcessedAt = requiresAccept ? null : (DateTimeOffset?)acceptTime;
        }

        public void ConfirmDenial(DateTimeOffset confirmedAt, int userId, int? impersonatorId)
        {
            if (Status != RequestStatus.DeniedByCreator)
            {
                throw new InvalidOperationException($"Förfrågan med boknings-id {Order.OrderNumber} är inte i rätt status för att kunna bekräfta avböjande.");
            }
            if (RequestStatusConfirmations.Any(r => r.RequestStatus == Status))
            {
                throw new InvalidOperationException($"Förfrågan med boknings-id {Order.OrderNumber} har redan bekräftats avböjd.");
            }
            RequestStatusConfirmations.Add(new RequestStatusConfirmation { ConfirmedBy = userId, ImpersonatingConfirmedBy = impersonatorId, RequestStatus = Status, ConfirmedAt = confirmedAt });
        }

        public void ConfirmCancellation(DateTimeOffset confirmedAt, int userId, int? impersonatorId)
        {
            if (Status != RequestStatus.CancelledByCreatorWhenApproved && Status != RequestStatus.CancelledByCreator)
            {
                throw new InvalidOperationException($"Förfrågan med boknings-id {Order.OrderNumber} är inte i rätt status för att kunna bekräfta avbokning.");
            }
            if (RequestStatusConfirmations.Any(r => r.RequestStatus == Status))
            {
                throw new InvalidOperationException($"Förfrågan med boknings-id {Order.OrderNumber} har redan bekräftats avbokad.");
            }
            RequestStatusConfirmations.Add(new RequestStatusConfirmation { ConfirmedBy = userId, ImpersonatingConfirmedBy = impersonatorId, RequestStatus = Status, ConfirmedAt = confirmedAt });
        }

        public void ConfirmNoAnswer(DateTimeOffset confirmedAt, int userId, int? impersonatorId)
        {
            if (Status != RequestStatus.ResponseNotAnsweredByCreator)
            {
                throw new InvalidOperationException($"Förfrågan med boknings-id {Order.OrderNumber} är inte i rätt status för att kunna bekräfta obesvarad.");
            }
            if (RequestStatusConfirmations.Any(r => r.RequestStatus == Status))
            {
                throw new InvalidOperationException($"Förfrågan med boknings-id {Order.OrderNumber} har redan bekräftats obesvarad.");
            }
            RequestStatusConfirmations.Add(new RequestStatusConfirmation { ConfirmedBy = userId, ImpersonatingConfirmedBy = impersonatorId, RequestStatus = Status, ConfirmedAt = confirmedAt });
        }

        public void ConfirmNoRequisition(DateTimeOffset confirmedAt, int userId, int? impersonatorId)
        {
            if (Status != RequestStatus.Approved)
            {
                throw new InvalidOperationException($"Förfrågan med boknings-id {Order.OrderNumber} är inte i rätt status för att kunna arkiveras.");
            }
            if (RequestStatusConfirmations.Any(r => r.RequestStatus == Status))
            {
                throw new InvalidOperationException($"Bokningsförfrågan med boknings-id {Order.OrderNumber} har redan arkiverats");
            }
            if (Order.StartAt > confirmedAt)
            {
                throw new InvalidOperationException($"Tolkuppdraget med boknings-id {Order.OrderNumber} har inte startat ännu och kan därför inte arkiveras");
            }
            RequestStatusConfirmations.Add(new RequestStatusConfirmation { ConfirmedBy = userId, ImpersonatingConfirmedBy = impersonatorId, RequestStatus = Status, ConfirmedAt = confirmedAt });
            Status = RequestStatus.Delivered;
            Order.Status = OrderStatus.Delivered;
        }

        public void ConfirmOrderChange(List<int> confirmedOrderChangeLogEntriesId, DateTimeOffset confirmedAt, int userId, int? impersonatorId)
        {
            if (confirmedOrderChangeLogEntriesId == null)
            {
                throw new InvalidOperationException("Hittade inga ändringar att bekräfta.");
            }
            var ids = Order.OrderChangeLogEntries.Where(oc => oc.BrokerId == Ranking.BrokerId).Select(oc => oc.OrderChangeLogEntryId);
            if (confirmedOrderChangeLogEntriesId.Except(ids).Any())
            {
                throw new InvalidOperationException("Ändringarna tillhörde inte den här bokningsförfrågan");
            }
            var previousConfirmations = Order.OrderChangeLogEntries
                .Where(oc => confirmedOrderChangeLogEntriesId.Contains(oc.OrderChangeLogEntryId) && oc.OrderChangeConfirmation != null && oc.OrderChangeLogType != OrderChangeLogType.ContactPerson);
            if (previousConfirmations.Any())
            {
                throw new InvalidOperationException("Bokningsändring redan bekräftad");
            }
            foreach (OrderChangeLogEntry oc in Order.OrderChangeLogEntries)
            {
                if (confirmedOrderChangeLogEntriesId.Contains(oc.OrderChangeLogEntryId))
                {
                    oc.OrderChangeConfirmation = new OrderChangeConfirmation
                    {
                        ConfirmedAt = confirmedAt,
                        ConfirmedBy = userId,
                        ImpersonatingConfirmedBy = impersonatorId
                    };
                }
            }
        }

        public void AddRequestView(int userId, int? impersonatorId, DateTimeOffset swedenNow)
        {
            if (!RequestViews.Any(rv => rv.ViewedBy == userId))
            {
                RequestViews.Add(new RequestView
                {
                    ViewedBy = userId,
                    ImpersonatingViewedBy = impersonatorId,
                    ViewedAt = swedenNow
                });
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
                throw new InvalidOperationException($"Det gick inte att tacka nej till förfrågan med boknings-id {Order.OrderNumber}, den har redan blivit besvarad");
            }
            base.Decline(declinedAt, userId, impersonatorId, message);
            Order.Status = !Order.ReplacingOrderId.HasValue ? OrderStatus.Requested : OrderStatus.NoBrokerAcceptedOrder;
        }

        internal void DeclineInGroup(
            DateTimeOffset declinedAt,
            int userId,
            int? impersonatorId,
            string message)
        {
            if (Order.OrderGroupId == null)
            {
                throw new InvalidOperationException($"Beställningen {Order.OrderNumber}, tillhör inte en sammanhållen bokning. Anropa rätt metod för att tacka nej till en del av en grupp.");
            }

            Decline(declinedAt, userId, impersonatorId, message);
        }

        public void AcceptReplacementOrder(
            DateTimeOffset acceptTime,
            int userId,
            int? impersonatorId,
            string expectedTravelCostInfo,
            InterpreterLocation interpreterLocation,
            PriceInformation priceInformation,
            DateTimeOffset? latestAnswerTimeForCustomer,
            string brokerReferenceNumber)
        {
            if (priceInformation == null)
            {
                throw new ArgumentNullException($"Det gick inte att svara på ersättningsuppdraget, förfrågan med boknings-id {Order.OrderNumber} saknar prisrader");
            }
            if (!IsToBeProcessedByBroker)
            {
                throw new InvalidOperationException($"Det gick inte att svara på ersättningsuppdraget, förfrågan med boknings-id {Order.OrderNumber} har redan blivit besvarad");
            }
            if (!Order.ReplacingOrderId.HasValue)
            {
                throw new InvalidOperationException($"Något gick fel, det gick inte att svara på förfrågan med boknings-id {Order.OrderNumber}. Detta är inget ersättninguppdrag och skulle bli besvarat på annat sätt.");
            }
            ValidateInterpreterLocationAgainstOrder(interpreterLocation);
            ValidateLatestAnswerTimeAndTravelCost(interpreterLocation, priceInformation, latestAnswerTimeForCustomer, acceptTime);
            AnswerDate = acceptTime;
            AnsweredBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
            InterpreterLocation = (int?)interpreterLocation;
            ExpectedTravelCostInfo = expectedTravelCostInfo;
            BrokerReferenceNumber = brokerReferenceNumber;
            PriceRows = priceInformation.PriceRows.Select(row => DerivedClassConstructor.Construct<PriceRowBase, RequestPriceRow>(row)).ToList();
            if (RequiresAccept)
            {
                Status = RequestStatus.Accepted;
                Order.Status = OrderStatus.RequestResponded;
            }
            else
            {
                Status = RequestStatus.Approved;
                Order.Status = OrderStatus.ResponseAccepted;
                AnswerProcessedAt = acceptTime;
            }
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
            string brokerReferenceNumber,
            VerificationResult? verificationResult = null,
            DateTimeOffset? latestAnswerTimeForCustomer = null
            )
        {
            if (Status != RequestStatus.AcceptedNewInterpreterAppointed)
            {
                throw new InvalidOperationException($"Något gick fel, det gick inte att byta tolk på förfrågan med boknings-id {Order.OrderNumber}");
            }
            if (priceInformation == null)
            {
                throw new ArgumentNullException($"Det gick inte att byta tolk på förfrågan med boknings-id {Order.OrderNumber}, förfrågan saknar prisrader");
            }
            if (oldRequest == null)
            {
                throw new ArgumentNullException($"Det gick inte att byta tolk på förfrågan med boknings-id {Order.OrderNumber}, hittar ingen koppling till tidigare förfrågan");
            }
            ValidateInterpreterLocationAgainstOrder(interpreterLocation);
            ValidateRequirementsAgainstOrder(competenceLevel, requirementAnswers);
            ValidateLatestAnswerTimeAndTravelCost(interpreterLocation, priceInformation, latestAnswerTimeForCustomer, acceptTime);
            AnswerDate = acceptTime;
            AnsweredBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
            Interpreter = interperter;
            InterpreterLocation = (int?)interpreterLocation;
            CompetenceLevel = (int?)competenceLevel;
            AnswerProcessedAt = isAutoAccepted ? (DateTimeOffset?)acceptTime : null;
            ReceivedBy = oldRequest.ReceivedBy;
            RecievedAt = oldRequest.RecievedAt;
            ImpersonatingReceivedBy = oldRequest.ImpersonatingReceivedBy;
            ReplacingRequestId = oldRequest.RequestId;
            PriceRows = new List<RequestPriceRow>();
            RequirementAnswers = requirementAnswers;
            Attachments = attachments.ToList();
            PriceRows = priceInformation.PriceRows.Select(row => DerivedClassConstructor.Construct<PriceRowBase, RequestPriceRow>(row)).ToList();
            ExpectedTravelCostInfo = expectedTravelCostInfo;
            InterpreterCompetenceVerificationResultOnAssign = verificationResult;
            LatestAnswerTimeForCustomer = latestAnswerTimeForCustomer;
            BrokerReferenceNumber = brokerReferenceNumber;

            if (!isAutoAccepted)
            {
                Order.Status = OrderStatus.RequestRespondedNewInterpreter;
            }
            if (isAutoAccepted)
            {
                Status = RequestStatus.Approved;
                if (Order.Status != OrderStatus.ResponseAccepted)
                {
                    Order.Status = OrderStatus.ResponseAccepted;
                }
            }
        }

        internal override void Deny(DateTimeOffset denyTime, int userId, int? impersonatorId, string message)
        {
            if (!CanDeny)
            {
                throw new InvalidOperationException($"Request {RequestId} is {Status}. Only Accepted requests can be denied.");
            }
            base.Deny(denyTime, userId, impersonatorId, message);
            Order.Status = OrderStatus.Requested;
        }

        internal void Cancel(DateTimeOffset cancelledAt, int userId, int? impersonatorId, string message, bool createFullCompensationRequisition = false, bool isReplaced = false, bool isCancelledFromGroup = false, List<MealBreak> mealbreaks = null, PriceInformation pi = null)
        {
            if ((!isCancelledFromGroup && !CanCancel) || (isCancelledFromGroup && !CanCancelFromGroup))
            {
                throw new InvalidOperationException($"Order {OrderId} is {Order.Status}, and request {RequestId} is {Status}. Order or request has wrong status to be cancelled");
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
                        Message = createFullCompensationRequisition ? "Genererad av systemet. Full ersättning utgår pga att avbokning skett mindre än 48 timmar före bokat tolktillfälle." : "Genererat av systemet vid avbokning, endast förmedlingsavgift utgår.",
                        Status = RequisitionStatus.AutomaticGeneratedFromCancelledOrder,
                        SessionStartedAt = Order.StartAt,
                        SessionEndedAt = Order.EndAt,
                        PriceRows = pi?.PriceRows.Select(row => DerivedClassConstructor.Construct<PriceRowBase, RequisitionPriceRow>(row)).ToList() ?? GetPriceRows(createFullCompensationRequisition),
                        MealBreaks = mealbreaks
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
                throw new InvalidOperationException("A requisition cannot be created when there are active requisitions.");
            }
            if (!(Status == RequestStatus.Approved || Status == RequestStatus.Delivered))
            {
                throw new InvalidOperationException("A requisition cannot be created when request is not approved or delivered.");
            }
            if (Order.StartAt > requisition?.CreatedAt)
            {
                throw new InvalidOperationException("A requisition cannot be created before order start time.");
            }
            Requisitions.Add(requisition);
            //Change status on order accordingly.
            Order.DeliverRequisition();
            Status = RequestStatus.Delivered;
        }

        #endregion

        #region private methods

        private void ValidateLatestAnswerTimeAndTravelCost(InterpreterLocation interpreterLocation, PriceInformation priceInformation, DateTimeOffset? latestAnswerTimeForCustomer, DateTimeOffset now)
        {
            if (Order.AllowExceedingTravelCost == AllowExceedingTravelCost.No)
            {
                if (latestAnswerTimeForCustomer != null)
                {
                    throw new InvalidOperationException("It is not possible to set LatestAnswerTimeForCustomer when customer does not allow exceeding travel cost.");
                }
                if (priceInformation.PriceRows.Any(p => p.PriceRowType == PriceRowType.TravelCost))
                {
                    throw new InvalidOperationException("It is not possible to set ExpectedTravelCost when customer does not allow exceeding travel cost.");
                }

            }
            else if (interpreterLocation == Enums.InterpreterLocation.OffSitePhone || interpreterLocation == Enums.InterpreterLocation.OffSiteVideo)
            {
                if (latestAnswerTimeForCustomer != null)
                {
                    throw new InvalidOperationException($"It is not possible to set LatestAnswerTimeForCustomer for interpreter location {EnumHelper.GetCustomName(interpreterLocation)}.");
                }
                if (priceInformation.PriceRows.Any(p => p.PriceRowType == PriceRowType.TravelCost))
                {
                    throw new InvalidOperationException($"It is not possible to set ExpectedTravelCost for interpreter location {EnumHelper.GetCustomName(interpreterLocation)}.");
                }
            }
            if (latestAnswerTimeForCustomer != null)
            {
                if (latestAnswerTimeForCustomer.Value >= Order.StartAt)
                {
                    throw new InvalidOperationException("LatestAnswerTimeForCustomer must not be after order start time.");
                }
                if (latestAnswerTimeForCustomer.Value <= now)
                {
                    throw new InvalidOperationException("LatestAnswerTimeForCustomer must not be before now.");
                }
            }
        }

        private void ValidateInterpreterLocationAgainstOrder(InterpreterLocation interpreterLocation)
        {
            if (!Order.InterpreterLocations.Any(l => l.InterpreterLocation == interpreterLocation))
            {
                throw new InvalidOperationException($"Interpreter location {EnumHelper.GetCustomName(interpreterLocation)} is not valid for this order.");
            }
        }

        private void ValidateRequirementsAgainstOrder(CompetenceAndSpecialistLevel competenceLevel, List<OrderRequirementRequestAnswer> requirementAnswers)
        {
            if (!RequestGroupId.HasValue)
            {
                ValidateRequirements(Order.Requirements, requirementAnswers);
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
                    PriceRowType = p.PriceRowType == PriceRowType.TravelCost ? PriceRowType.Outlay : p.PriceRowType,
                    PriceListRowId = p.PriceListRowId,
                    Price = p.Price,
                    Quantity = p.Quantity,
                    PriceCalculationChargeId = p.PriceCalculationChargeId,
                }).ToList();
        }

        private static void ValidateRequirements(List<OrderRequirement> requirements, List<OrderRequirementRequestAnswer> requirementAnswers)
        {

            if (requirements.Count != requirementAnswers.Count ||
                !requirements.OrderBy(r => r.OrderRequirementId).Select(r => r.OrderRequirementId).SequenceEqual(requirementAnswers.OrderBy(r => r.OrderRequirementId).Select(a => a.OrderRequirementId)))
            {
                throw new InvalidOperationException("The set of requirement answers does not match the set of requirements");
            }
            if (requirements.Any(r => r.IsRequired &&
                 requirementAnswers.Any(a => a.OrderRequirementId == r.OrderRequirementId &&
                     !a.CanSatisfyRequirement)))
            {
                throw new InvalidOperationException("Negative answer on required requirement");
            }
        }

        #endregion
    }
}
