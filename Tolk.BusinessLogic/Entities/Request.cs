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

        public Request(Ranking ranking, RequestExpiryResponse newRequestExpiry, DateTimeOffset creationTime, bool isTerminalRequest = false, bool isAReplacingRequest = false, RequestGroup requestGroup = null, DateTimeOffset? respondedStartAt = null)
        {
            if (!isAReplacingRequest && newRequestExpiry.ExpiryAt.HasValue && newRequestExpiry.ExpiryAt < creationTime)
            {
                throw new InvalidOperationException("The Request cannot have an expiry before the creation time.");
            }
            Ranking = ranking;
            Status = RequestStatus.Created;
            ExpiresAt = newRequestExpiry.ExpiryAt;
            CreatedAt = creationTime;
            IsTerminalRequest = isTerminalRequest;
            RequestGroup = requestGroup;
            RequestAnswerRuleType = newRequestExpiry.RequestAnswerRuleType;
            LastAcceptAt = newRequestExpiry.LastAcceptedAt;
            RespondedStartAt = respondedStartAt;
        }

        internal Request(Ranking ranking, DateTimeOffset creationTime, Quarantine quarantine)
        {
            Ranking = ranking;
            Status = RequestStatus.LostDueToQuarantine;
            CreatedAt = creationTime;
            Quarantine = quarantine;
            QuarantineId = quarantine.QuarantineId;
        }

        //Used when replacing order, should not inherit RespondedStartAt
        internal Request(Request originalRequest, RequestExpiryResponse newRequestExpiry, DateTimeOffset creationTime)
            : this(originalRequest.Ranking, newRequestExpiry, creationTime)
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

        /// <summary>
        /// Used when the customer has set flexible start at and expected length
        /// </summary>
        public DateTimeOffset? RespondedStartAt { get; set; }

        public DateTimeOffset CalculatedStartAt => RespondedStartAt ?? Order.StartAt;

        public DateTimeOffset CalculatedEndAt => RespondedStartAt.HasValue ? RespondedStartAt.Value.Add(Order.ExpectedLength.Value): Order.EndAt;

        public bool AllowOrderAgreementCreation()
        {
            //If invalid status, return false;
            if (!IsApprovedOrDelivered && !IsCancelledByCreator)
            {
                return false;
            }

            if (IsApprovedOrDelivered && OrderAgreementPayloads.Count == 0 && Requisitions.Count == 0)
            {
                //The Order Agreement should be created on this request
                return true;
            }
            if ((IsCancelledByCreator && !Requisitions.Any(r => r.Status == RequisitionStatus.AutomaticGeneratedFromCancelledOrder)) ||
                (!IsCancelledByCreator && Requisitions.Any(r => r.Status == RequisitionStatus.AutomaticGeneratedFromCancelledOrder)))
            {
                return false;
            }
            var requisition = Requisitions.Where(r => r.Status == RequisitionStatus.Approved ||
            r.Status == RequisitionStatus.AutomaticGeneratedFromCancelledOrder ||
            r.Status == RequisitionStatus.Created ||
            r.Status == RequisitionStatus.Reviewed).SingleOrDefault();
            if (requisition == null || OrderAgreementPayloads.Any(p => p.RequisitionId == requisition.RequisitionId))
            {
                return false;
            }
            //The order agreement shold be created on the found requisition.
            return true;
        }

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

        public List<OrderAgreementPayload> OrderAgreementPayloads { get; set; }

        #endregion

        #region Status Checks

        public bool CanCancel => CanCancelRequestBelongsToGroup || CanCancelRequestNotBelongsToGroup;

        public bool CanCancelFromGroup => (Order.Status == OrderStatus.Requested || Order.Status == OrderStatus.RequestAcceptedAwaitingInterpreter) && IsToBeProcessedByBroker && Order.OrderGroupId.HasValue;

        private bool CanCancelRequestNotBelongsToGroup => !Order.OrderGroupId.HasValue &&
            (Order.Status == OrderStatus.Requested || Order.Status == OrderStatus.RequestRespondedAwaitingApproval
            || Order.Status == OrderStatus.RequestRespondedNewInterpreter || Order.Status == OrderStatus.ResponseAccepted || Order.Status == OrderStatus.RequestAcceptedAwaitingInterpreter) &&
            (IsToBeProcessedByBroker || IsAcceptedOrApproved);

        private bool CanCancelRequestBelongsToGroup => Order.OrderGroupId.HasValue &&
            (Order.Status == OrderStatus.RequestRespondedNewInterpreter || Order.Status == OrderStatus.ResponseAccepted || Order.Status == OrderStatus.RequestAcceptedAwaitingInterpreter) &&
            (Status == RequestStatus.Approved || Status == RequestStatus.AcceptedNewInterpreterAppointed);

        public bool CanCreateReplacementOrderOnCancel => !Order.OrderGroupId.HasValue && !Order.ReplacingOrderId.HasValue && Status == RequestStatus.Approved;

        public bool CanChangeInterpreter(DateTimeOffset swedenNow) => CalculatedStartAt > swedenNow &&
            ((!RequestGroupId.HasValue && IsAcceptedOrApproved) ||
            (RequestGroupId.HasValue && (Status == RequestStatus.Approved || Status == RequestStatus.AcceptedNewInterpreterAppointed)));

        public bool CanCreateRequisition => !Requisitions.Any(r => r.Status == RequisitionStatus.Reviewed || r.Status == RequisitionStatus.Created) && IsApprovedOrDelivered;

        public bool CanCreateComplaint(DateTimeOffset swedenNow) => !Complaints.Any() && HasCorrectStatusForCreateComplaint && !(IsApprovedOrDelivered && (RespondedStartAt ?? Order.StartAt) > swedenNow);

        public bool HasCorrectStatusForCreateComplaint => IsApprovedOrDelivered || Status == RequestStatus.CancelledByBroker;

        public bool IsApprovedOrDelivered => (Status == RequestStatus.Approved || Status == RequestStatus.Delivered);

        public bool IsCancelledByCreator => Status == RequestStatus.CancelledByCreatorWhenApprovedOrAccepted || Status == RequestStatus.CancelledByCreator;

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
            if (!IsAwaitingApproval)
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

        public void Answer(
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
            bool overrideRequireAccept = false,
            DateTimeOffset? respondedStartAt = null
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
            if (respondedStartAt.HasValue && RespondedStartAt.HasValue)
            {
                throw new InvalidOperationException($"Något gick fel, det gick inte att svara på förfrågan med boknings-id {Order.OrderNumber}. Uppdragets starttid är redan satt och kan inte ändras.");
            }
            ValidateRespondedStartAtAgainstOrder(respondedStartAt);
            ValidateInterpreterLocationAgainstOrder(interpreterLocation);
            ValidateRequirementsAgainstOrder(requirementAnswers);
            ValidateCompetenceLevelAgainstOrder(competenceLevel);
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
            RespondedStartAt = respondedStartAt;
            var requiresAccept = overrideRequireAccept || RequiresAccept;

            Status = requiresAccept ? RequestStatus.AnsweredAwaitingApproval : RequestStatus.Approved;
            Order.Status = requiresAccept ? OrderStatus.RequestRespondedAwaitingApproval : OrderStatus.ResponseAccepted;
            AnswerProcessedAt = requiresAccept ? null : (DateTimeOffset?)acceptTime;
        }

        public void AnswerAcceptedRequest(
            DateTimeOffset answerTime,
            int userId,
            int? impersonatorId,
            InterpreterBroker interperter,
            InterpreterLocation interpreterLocation,
            CompetenceAndSpecialistLevel competenceLevel,
            List<OrderRequirementRequestAnswer> requirementAnswers,
            List<RequestAttachment> attachments,
            PriceInformation priceInformation,
            Request oldRequest,
            string expectedTravelCostInfo,
            DateTimeOffset? latestAnswerTimeForCustomer,
            string brokerReferenceNumber,
            VerificationResult? verificationResult = null,
            bool overrideRequireAccept = false
            )
        {
            if (Status != RequestStatus.AcceptedNewInterpreterAppointed)
            {
                throw new InvalidOperationException($"Något gick fel, förfrågan med boknings-id {Order.OrderNumber} har inte rätt status");
            }
            if (priceInformation == null)
            {
                throw new ArgumentNullException($"Det gick inte att färdigställa tidigare bekräftad förfrågan med boknings-id {Order.OrderNumber}, förfrågan saknar prisrader");
            }
            if (oldRequest == null)
            {
                throw new ArgumentNullException($"Det gick inte att färdigställa tidigare bekräftad förfrågan med boknings-id {Order.OrderNumber}, hittar ingen koppling till bekräftelsen");
            }
            ValidateInterpreterLocationAgainstOrder(interpreterLocation);
            ValidateRequirementsAgainstOrder(requirementAnswers);
            ValidateCompetenceLevelAgainstOrder(competenceLevel);
            ValidateLatestAnswerTimeAndTravelCost(interpreterLocation, priceInformation, latestAnswerTimeForCustomer, answerTime);
            AnswerDate = answerTime;
            AnsweredBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
            Interpreter = interperter;
            InterpreterLocation = (int?)interpreterLocation;
            CompetenceLevel = (int?)competenceLevel;
            AnswerProcessedAt = (DateTimeOffset?)answerTime;
            ReceivedBy = oldRequest.ReceivedBy;
            RecievedAt = oldRequest.RecievedAt;
            ImpersonatingReceivedBy = oldRequest.ImpersonatingReceivedBy;
            AcceptedAt = oldRequest.AcceptedAt;
            AcceptedBy = oldRequest.AcceptedBy;
            ImpersonatingAcceptedBy = oldRequest.ImpersonatingAcceptedBy;
            ReplacingRequestId = oldRequest.RequestId;
            RequirementAnswers = requirementAnswers;
            Attachments = attachments;
            PriceRows = priceInformation.PriceRows.Select(row => DerivedClassConstructor.Construct<PriceRowBase, RequestPriceRow>(row)).ToList();
            ExpectedTravelCostInfo = expectedTravelCostInfo;
            InterpreterCompetenceVerificationResultOnAssign = verificationResult;
            BrokerReferenceNumber = brokerReferenceNumber;
            LatestAnswerTimeForCustomer = latestAnswerTimeForCustomer;

            var requiresAccept = overrideRequireAccept || RequiresAccept;

            Status = requiresAccept ? RequestStatus.AnsweredAwaitingApproval : RequestStatus.Approved;
            Order.Status = requiresAccept ? OrderStatus.RequestRespondedAwaitingApproval : OrderStatus.ResponseAccepted;
            AnswerProcessedAt = requiresAccept ? null : (DateTimeOffset?)answerTime;
        }

        public void Accept(
            DateTimeOffset acceptTime,
            int userId,
            int? impersonatorId,
            InterpreterLocation interpreterLocation,
            CompetenceAndSpecialistLevel? competenceLevel,
            List<OrderRequirementRequestAnswer> requirementAnswers,
            List<RequestAttachment> attachedFiles,
            PriceInformation priceInformation,
            string brokerReferenceNumber, 
            DateTimeOffset? respondedStartAt = null
            )
        {
            if (priceInformation == null)
            {
                throw new ArgumentNullException($"Det gick inte att bekräfta förfrågan med boknings-id {Order.OrderNumber} prisrader saknas");
            }
            if (!IsToBeProcessedByBroker)
            {
                throw new InvalidOperationException($"Det gick inte att bekräfta förfrågan med boknings-id {Order.OrderNumber}, den har redan blivit besvarad");
            }
            if (!IsAnswerLevelAccept)
            {
                throw new InvalidOperationException($"Det gick inte att bekräfta på förfrågan med boknings-id {Order.OrderNumber}, den kräver fullt svar direkt");
            }
            if (Order.ReplacingOrderId.HasValue)
            {
                throw new InvalidOperationException($"Något gick fel, det gick inte att svara på förfrågan med boknings-id {Order.OrderNumber}. Detta är ett ersättninguppdrag och skulle bli besvarat på annat sätt.");
            }
            ValidateRespondedStartAtAgainstOrder(respondedStartAt);
            ValidateRequirementsAgainstOrder(requirementAnswers);
            ValidateCompetenceLevelAgainstOrder(competenceLevel);
            ValidateInterpreterLocationAgainstOrder(interpreterLocation);
            AcceptedAt = acceptTime;
            AcceptedBy = userId;
            InterpreterLocation = (int?)interpreterLocation;
            ImpersonatingAcceptedBy = impersonatorId;
            CompetenceLevel = (int?)competenceLevel;
            RequirementAnswers = requirementAnswers;
            Attachments = attachedFiles;
            PriceRows = priceInformation.PriceRows.Select(row => DerivedClassConstructor.Construct<PriceRowBase, RequestPriceRow>(row)).ToList();
            BrokerReferenceNumber = brokerReferenceNumber;
            RespondedStartAt = respondedStartAt;

            Status = RequestStatus.AcceptedAwaitingInterpreter;
            Order.Status = OrderStatus.RequestAcceptedAwaitingInterpreter;
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
            if (Status != RequestStatus.CancelledByCreatorWhenApprovedOrAccepted && Status != RequestStatus.CancelledByCreator)
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
            if (CalculatedStartAt > confirmedAt)
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

        public void DeclineRequest(
            DateTimeOffset declinedAt,
            int userId,
            int? impersonatorId,
            string message,
            List<MealBreak> mealbreaks = null,
            List<RequisitionPriceRow> priceRows = null)
        {
            if (!CanDecline)
            {
                throw new InvalidOperationException($"Det gick inte att tacka nej till förfrågan med boknings-id {Order.OrderNumber}, den har redan blivit besvarad");
            }
            if (priceRows != null)
            {
                Requisitions.Add(
                    new Requisition
                    {
                        CreatedAt = declinedAt,
                        CreatedBy = userId,
                        ImpersonatingCreatedBy = impersonatorId,
                        Message = "Genererad av systemet. Full ersättning utgår pga att avbokning skett mindre än 48 timmar före bokat tolktillfälle och att tolken inte kan inställa sig efter de förändrade tidsramarna.",
                        Status = RequisitionStatus.AutomaticGeneratedFromCancelledOrder,
                        SessionStartedAt = CalculatedStartAt,
                        SessionEndedAt = CalculatedEndAt,
                        PriceRows = priceRows,
                        MealBreaks = mealbreaks
                    }
                );

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
                Status = RequestStatus.AnsweredAwaitingApproval;
                Order.Status = OrderStatus.RequestRespondedAwaitingApproval;
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
            ValidateRequirementsAgainstOrder(requirementAnswers);
            ValidateCompetenceLevelAgainstOrder(competenceLevel);
            ValidateLatestAnswerTimeAndTravelCost((InterpreterLocation)oldRequest.InterpreterLocation.Value, priceInformation, latestAnswerTimeForCustomer, acceptTime);
            AnswerDate = acceptTime;
            AnsweredBy = userId;
            ImpersonatingAnsweredBy = impersonatorId;
            Interpreter = interperter;
            InterpreterLocation = oldRequest.InterpreterLocation;
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

        internal void Cancel(DateTimeOffset cancelledAt, int userId, int? impersonatorId, string message, bool createFullCompensationRequisition = false, bool isReplaced = false, bool isCancelledFromGroup = false, List<MealBreak> mealbreaks = null, List<RequisitionPriceRow> priceRows = null)
        {
            if ((!isCancelledFromGroup && !CanCancel) || (isCancelledFromGroup && !CanCancelFromGroup))
            {
                throw new InvalidOperationException($"Order {OrderId} is {Order.Status}, and request {RequestId} is {Status}. Order or request has wrong status to be cancelled");
            }
            if (CalculatedStartAt < cancelledAt)
            {
                throw new InvalidOperationException($"Order {OrderId} has already passed its start time. Orders that has started cannot be cancelled");
            }
            if (Status == RequestStatus.Approved && !isReplaced && priceRows == null)
            {
                throw new InvalidOperationException($"Price rows must be provided for order {OrderId}, if a approved order is cancelled without providing a replacement");
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
                        SessionStartedAt = CalculatedStartAt,
                        SessionEndedAt = CalculatedEndAt,
                        PriceRows = priceRows,
                        MealBreaks = mealbreaks
                    }
                );
            }
            Status = ((Status == RequestStatus.Approved || Status == RequestStatus.AcceptedAwaitingInterpreter) && !isReplaced) ? RequestStatus.CancelledByCreatorWhenApprovedOrAccepted : RequestStatus.CancelledByCreator;
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
            if (CalculatedStartAt < cancelledAt)
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

        internal void TerminateDueToEndedFrameworkAgreement(DateTimeOffset terminatedAt, string terminationMessage, IEnumerable<RequestStatus> terminatableStatuses)
        {
            if (!terminatableStatuses.Contains(Status))
            {
                throw new InvalidOperationException($"Request {RequestId} is {Status}. Only requests under negotiation can be terminated due to ended framework agreement");
            }
            if (CalculatedStartAt < terminatedAt)
            {
                throw new InvalidOperationException($"Order {OrderId} has already passed its start time. Orders that have started can not be terminated due to ended framework agreement");
            }

            Status = NewStatusWhenRequestIsTerminatedDueToEndedFrameworkAgreement;
            CancelledAt = terminatedAt;
            CancelMessage = terminationMessage;
            Order.Status = OrderStatus.TerminatedDueToTerminatedFrameworkAgreement;
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
            if (CalculatedStartAt > requisition?.CreatedAt)
            {
                throw new InvalidOperationException("A requisition cannot be created before order start time.");
            }
            Requisitions.Add(requisition);
            //Change status on order accordingly.
            Order.DeliverRequisition();
            Status = RequestStatus.Delivered;
        }

        public List<RequisitionPriceRow> GenerateRequisitionPriceRows(bool createFullCompensationRequisition)
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
        
        internal void SetRequestExpiryManually(DateTimeOffset expiry, DateTimeOffset updatedAt, int userId, int? impersonatingUserId)
        {
            if (Status != RequestStatus.AwaitingDeadlineFromCustomer)
            {
                throw new InvalidOperationException($"There is no request awaiting deadline from customer on this order {OrderId}");
            }
            ExpiresAt = expiry;
            Order.Status = OrderStatus.Requested;
            Status = RequestStatus.Created;
            RequestUpdateLatestAnswerTime = new RequestUpdateLatestAnswerTime { UpdatedAt = updatedAt, UpdatedBy = userId, ImpersonatorUpdatedBy = impersonatingUserId };
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
                if (latestAnswerTimeForCustomer.Value >= CalculatedStartAt)
                {
                    throw new InvalidOperationException("LatestAnswerTimeForCustomer must not be after order start time.");
                }
                if (latestAnswerTimeForCustomer.Value <= now)
                {
                    throw new InvalidOperationException("LatestAnswerTimeForCustomer must not be before now.");
                }
            }
        }
        private void ValidateRespondedStartAtAgainstOrder(DateTimeOffset? respondedStartAt)
        {
            if (Order.ExpectedLength.HasValue && !respondedStartAt.HasValue)
            {
                throw new InvalidOperationException($"Något gick fel, det gick inte att svara på förfrågan med boknings-id {Order.OrderNumber}. Detta är en bokning med flexibel start, vilket kräver att svaret innehåller en angiven starttid.");
            }

            if (!Order.ExpectedLength.HasValue && respondedStartAt.HasValue)
            {
                throw new InvalidOperationException($"Något gick fel, det gick inte att svara på förfrågan med boknings-id {Order.OrderNumber}. Detta är inte en bokning med flexibel start, vilket gör att man inte får tillhandahålla en starttid.");
            }

            if (!Order.IsValidRespondedStartAt(respondedStartAt))
            {
                throw new InvalidOperationException($"Något gick fel, det gick inte att svara på förfrågan med boknings-id {Order.OrderNumber}. Den angivna starttiden är inte korrekt i relation till bokningens flexibla tider.");
            }
        }

        private void ValidateInterpreterLocationAgainstOrder(InterpreterLocation interpreterLocation)
        {
            if (!Order.InterpreterLocations.Any(l => l.InterpreterLocation == interpreterLocation))
            {
                throw new InvalidOperationException($"Interpreter location {EnumHelper.GetCustomName(interpreterLocation)} is not valid for this order.");
            }
        }

        private void ValidateRequirementsAgainstOrder(List<OrderRequirementRequestAnswer> requirementAnswers)
        {
            if (!RequestGroupId.HasValue)
            {
                ValidateRequirements(Order.Requirements, requirementAnswers);
            }
        }

        private void ValidateCompetenceLevelAgainstOrder(CompetenceAndSpecialistLevel? competenceLevel)
        {
            if (Order.SpecificCompetenceLevelRequired)
            {
                if (!competenceLevel.HasValue)
                {
                    throw new InvalidOperationException($"Competence level must be specified for this order.");
                }
                if (!Order.CompetenceRequirements.Any(c => c.CompetenceLevel == competenceLevel))
                {
                    throw new InvalidOperationException($"Specified competence level {EnumHelper.GetCustomName(competenceLevel.Value)} is not valid for this order.");
                }
            }
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
