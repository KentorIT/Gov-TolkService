﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;
using Tolk.Web.Services;

namespace Tolk.Web.Models
{
    public class OrderViewModel : OrderBaseModel
    {
        public int? OrderId { get; set; }

        public int? ReplacingOrderId { get; set; }

        public int? OrderGroupId { get; set; }

        public string OrderGroupNumber { get; set; }

        public OrderStatus? OrderGroupStatus { get; set; }

        public string GroupStatusCssClassColor => OrderGroupStatus.HasValue ? CssClassHelper.GetColorClassNameForOrderStatus(OrderGroupStatus.Value) : string.Empty;

        public string LastTimeForRequiringLatestAnswerBy { get; set; }

        public string NextLastTimeForRequiringLatestAnswerBy { get; set; }

        [Display(Name = "Dialekt")]

        public string Dialect { get; set; }
        [Display(Name = "Datum och tid", Description = "Datum och tid för tolkuppdraget")]
        public virtual TimeRange TimeRange { get; set; }

        public AttachmentListModel RequestAttachmentListModel { get; set; }

        [Display(Name = "Sista svarstid")]
        public DateTimeOffset? LatestAnswerBy { get; set; }

        [Display(Name = "Uppdragstyp")]
        public string AssignmentTypeName => AssignmentType.GetDescription();

        public AssignmentType AssignmentType { get; set; }

        [Display(Name = "Rätt att granska rekvisition", Description = "Välj vid behov en annan person som skall ges rätt att granska rekvisition, t ex person som deltar vid tolktillfället. Denna uppgift kan du även komplettera eller ändra senare.")]
        public int? ContactPersonId { get; set; }

        public int? ChangeContactPersonId { get; set; }

        public bool DisplayForBroker { get; set; } = false;

        public string WarningOrderTimeInfo { get; set; } = string.Empty;

        public string WarningOrderRequiredCompetenceInfo { get; set; } = string.Empty;

        public string WarningOrderGroupCloseInTime { get; set; } = string.Empty;

        public PriceInformation PriceInformation { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<FileModel> Files { get; set; }

        #region details


        public string ColorClassName { get => CssClassHelper.GetColorClassNameForOrderStatus(Status); }

        [Display(Name = "BokningsID")]
        public string OrderNumber { get; set; }

        public int? ReplacedByOrderId { get; set; }

        [Display(Name = "Ersatt av BokningsID")]
        public string ReplacedByOrderNumber { get; set; }

        [Display(Name = "Ersätter BokningsID")]
        public string ReplacingOrderNumber { get; set; }

        public int CreatedById { get; set; }

        public PriceInformationModel ActiveRequestPriceInformationModel { get; set; }

        public override decimal ExpectedTravelCosts
        {
            get => ActiveRequestPriceInformationModel.ExpectedTravelCosts;
            set => base.ExpectedTravelCosts = value;
        }

        [Display(Name = "Status på aktiv förfrågan")]
        public RequestStatus? RequestStatus { get; set; }

        public int? RequestId { get; set; }
        public int? RequestGroupId { get; set; }

        public RequestViewModel ActiveRequest { get; set; }

        public IEnumerable<BrokerListModel> PreviousRequests { get; set; }

        [Display(Name = "Anledning till att bokningen avbokas")]
        [DataType(DataType.MultilineText)]
        [ClientRequired]
        [StringLength(1000)]
        [Placeholder("Beskriv anledning till avbokning.")]
        public string CancelMessage { get; set; }

        [Display(Name = "Är tolkanvändare samma person som bokar")]
        public bool IsCreatorInterpreterUser { get; set; }

        #endregion

        public bool OrderUpdateIsEnabled { get; set; } = false;

        [DataType(DataType.MultilineText)]
        [Display(Name = "Bokningsändringar")]
        public string DisplayOrderChangeText { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<int> ConfirmedOrderChangeLogEntries { get; set; } = new List<int>();

        #region user rights

        public bool UserCanCancelOrder { get; set; } = false;
        public bool UserCanEdit { get; set; } = false;
        public bool UserCanEditContactPerson { get; set; } = false;
        public bool UserCanPrint { get; set; } = false;
        public bool UserCanCreateComplaint { get; set; } = false;
        public bool UserCanAccept { get; set; } = false;
        public bool UserCanCanCreateRequisition { get; set; } = false;
        public bool UserCanCancelRequest{ get; set; } = false;
        #endregion

        public bool RequestCanBeCancelled { get; set; } = false;
        public bool RequestCanBeReplaced { get; set; } = false;
        public bool RequestCanBePrinted { get; set; } = false;
        public bool RequestIsApprovedOrDelivered { get; set; } = false;

        public bool StartAtIsInFuture { get; set; } = false;
        public bool TimeIsValidForOrderReplacement { get; set; } = false;
        public bool MealbreakIncluded { get; set; } = false;

        public bool HasNoBrokerAcceptedConfirmation { get; set; } = true;
        public bool HasResponseNotAnsweredByCreatorConfirmation { get; set; } = true;
        public bool HasCancelledByBrokerConfirmation { get; set; } = true;
        public bool HasNoRequisitionConfirmation { get; set; } = true;
        public bool HasDeniedByCreatorConfirmation { get; set; } = true;
        public bool HasResponseNotAnsweredByCreatorBrokerConfirmation { get; set; } = true;
        public bool HasCancelledByCreatorWhenApprovedConfirmation { get; set; } = true;
        public bool HasComplaints { get; set; } = true;
        public bool HasActiveRequests { get; set; } = true;

        public bool AllowOrderCancellation => UserCanCancelOrder && RequestCanBeCancelled && StartAtIsInFuture;
        public bool AllowReplacementOnCancel => AllowOrderCancellation && TimeIsValidForOrderReplacement && RequestCanBeReplaced;

        public bool AllowDenial => AllowExceedingTravelCost != null && EnumHelper.Parse<AllowExceedingTravelCost>(AllowExceedingTravelCost.SelectedItem.Value) == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldBeApproved;
        public bool AllowSettingTravelCosts => AllowExceedingTravelCost != null && (EnumHelper.Parse<AllowExceedingTravelCost>(AllowExceedingTravelCost.SelectedItem.Value) != BusinessLogic.Enums.AllowExceedingTravelCost.No);

        public bool AllowEditContactPerson => Status != OrderStatus.CancelledByBroker && Status != OrderStatus.CancelledByCreator && Status != OrderStatus.NoBrokerAcceptedOrder && Status != OrderStatus.ResponseNotAnsweredByCreator && UserCanEditContactPerson;

        public bool AllowUpdate => OrderUpdateIsEnabled && Status == OrderStatus.ResponseAccepted && StartAtIsInFuture && UserCanEdit;

        public bool AllowRequisitionRegistration => RequestIsApprovedOrDelivered && UserCanCanCreateRequisition && !HasActiveRequests && !StartAtIsInFuture;

        public bool AllowConfirmNoRequisition => UserCanCanCreateRequisition && RequestStatus == BusinessLogic.Enums.RequestStatus.Approved && !RequisitionId.HasValue && !StartAtIsInFuture && !HasNoRequisitionConfirmation;

        public bool AllowRequestCancellation => StartAtIsInFuture && UserCanCancelRequest;

        public bool AllowConfirmationDenial => RequestStatus == BusinessLogic.Enums.RequestStatus.DeniedByCreator && !HasDeniedByCreatorConfirmation;

        public bool AllowConfirmNoAnswer => RequestStatus == BusinessLogic.Enums.RequestStatus.ResponseNotAnsweredByCreator && !HasResponseNotAnsweredByCreatorBrokerConfirmation;

        public bool AllowConfirmCancellationByCreator => RequestStatus == BusinessLogic.Enums.RequestStatus.CancelledByCreatorWhenApproved && !HasCancelledByCreatorWhenApprovedConfirmation;

        public bool DisplayOrderChange => (RequestStatus == BusinessLogic.Enums.RequestStatus.Approved || RequestStatus == BusinessLogic.Enums.RequestStatus.AcceptedNewInterpreterAppointed) && StartAtIsInFuture &&
            ConfirmedOrderChangeLogEntries.Any();

        public override bool AllowProcessing
        {
            get => ActiveRequestIsAnswered && (AllowProcessingOrderBelongsToGroup || AllowProcessingOrderNotBelongsToGroup);
            set => base.AllowProcessing = value;
        }

        private bool AllowProcessingOrderBelongsToGroup => OrderGroupId.HasValue && RequestStatus == BusinessLogic.Enums.RequestStatus.AcceptedNewInterpreterAppointed;

        private bool AllowProcessingOrderNotBelongsToGroup => !OrderGroupId.HasValue && (RequestStatus == BusinessLogic.Enums.RequestStatus.Accepted || RequestStatus == BusinessLogic.Enums.RequestStatus.AcceptedNewInterpreterAppointed);

        [Display(Name = "Skapa ersättningsuppdrag")]
        public bool AddReplacementOrder { get; set; } = false;

        public bool AllowComplaintCreation => HasComplaints && RequestIsApprovedOrDelivered && !StartAtIsInFuture && UserCanCreateComplaint;

        public bool AllowRequestPrint => RequestCanBePrinted && UserCanPrint;

        public bool AllowNoAnswerConfirmation => UserCanEdit && Status == OrderStatus.NoBrokerAcceptedOrder && !HasNoBrokerAcceptedConfirmation;

        public bool AllowResponseNotAnsweredConfirmation => UserCanEdit && Status == OrderStatus.ResponseNotAnsweredByCreator && !HasResponseNotAnsweredByCreatorConfirmation;

        public bool AllowUpdateExpiry => OrderGroupId == null && Status == OrderStatus.AwaitingDeadlineFromCustomer && UserCanEdit;

        public bool AllowConfirmCancellation => UserCanEdit && Status == OrderStatus.CancelledByBroker && !HasCancelledByBrokerConfirmation;

        public string InfoMessage { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public bool ActiveRequestIsAnswered { get; set; }

        public bool IsReplacement => ReplacingOrderId.HasValue;

        public bool IsInOrderGroup => OrderGroupId.HasValue;
        public bool SeveralOccasions { get; set; } = false;

        public bool HasOnsiteLocation => RankedInterpreterLocationFirst == InterpreterLocation.OnSite || RankedInterpreterLocationFirst == InterpreterLocation.OffSiteDesignatedLocation
        || RankedInterpreterLocationSecond == InterpreterLocation.OnSite || RankedInterpreterLocationSecond == InterpreterLocation.OffSiteDesignatedLocation
        || RankedInterpreterLocationThird == InterpreterLocation.OnSite || RankedInterpreterLocationThird == InterpreterLocation.OffSiteDesignatedLocation;

        public EventLogModel EventLog { get; set; }

        public IEnumerable<InterpreterLocation> RankedInterpreterLocations
        {
            get
            {
                if (RankedInterpreterLocationFirstAddressModel?.InterpreterLocation != null)
                {
                    yield return RankedInterpreterLocationFirstAddressModel.InterpreterLocation.Value;
                }
                if (RankedInterpreterLocationSecondAddressModel?.InterpreterLocation != null)
                {
                    yield return RankedInterpreterLocationSecondAddressModel.InterpreterLocation.Value;
                }
                if (RankedInterpreterLocationThirdAddressModel?.InterpreterLocation != null)
                {
                    yield return RankedInterpreterLocationThirdAddressModel.InterpreterLocation.Value;
                }
            }
        }

        //STUFF
        /// <summary>
        /// NOTE USE BOTH AS CUSTOMER AND AS BROKER, IF CUSTOMER GETS VIEW CHECKER
        /// </summary>
        public string ViewedByUser { get; set; } = string.Empty;

        public bool CompetenceIsRequired { get; set; }

        //TODO Possibly remove, uses this construction for the benefit of other models....
        public override bool SpecificCompetenceLevelRequired => CompetenceIsRequired;

        public bool HasPreviousRequests => PreviousRequests.Any();

        public string DisplayMealBreakIncluded { get; set; }

        public int? ComplaintId { get; set; }

        public int? RequisitionId { get; set; }

        internal static OrderViewModel GetModelFromOrder(Order order, Request request)
        {
            var model = new OrderViewModel
            {
#warning Dont like, should be possible to make lighter.
                AllowExceedingTravelCost = new RadioButtonGroup { SelectedItem = order.AllowExceedingTravelCost == null ? null : SelectListService.AllowExceedingTravelCost.Single(e => e.Value == order.AllowExceedingTravelCost.ToString()) },
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                AssignmentType = order.AssignmentType,
                ReplacingOrderId = order.ReplacingOrderId,
                ReplacedByOrderId = order.ReplacedByOrder?.OrderId,
                ReplacingOrderNumber = order.ReplacingOrder?.OrderNumber,
                ReplacedByOrderNumber = order.ReplacedByOrder?.OrderNumber,
                RegionName = order.Region.Name,
                OrderGroupId = order.OrderGroupId,
                OrderGroupNumber = order.Group?.OrderGroupNumber,
                OrderGroupStatus = order.Group?.Status,
                CreatedBy = order.ContactInformation,
                CreatedById = order.CreatedBy,
                ContactPerson = order.ContactPersonUser?.CompleteContactInformation,
                ChangeContactPersonId = order.ContactPersonId,
                CreatedAt = order.CreatedAt,
                InvoiceReference = order.InvoiceReference,
                LanguageName = order.OtherLanguage ?? order.Language?.Name,
                CustomerUnitName = order.CustomerUnit?.Name,
                CustomerReferenceNumber = order.CustomerReferenceNumber,
                LanguageHasAuthorizedInterpreter = order.LanguageHasAuthorizedInterpreter,
                CompetenceIsRequired = order.SpecificCompetenceLevelRequired,
                TimeRange = new TimeRange
                {
                    StartDateTime = order.StartAt,
                    EndDateTime = order.EndAt
                },
                DisplayMealBreakIncluded = (int)(order.EndAt.DateTime - order.StartAt.DateTime).TotalMinutes > 240 ? OrderModel.GetMealBreakText(order.MealBreakIncluded) : null,
                Description = order.Description,
                UnitName = order.UnitName,
                IsCreatorInterpreterUser = order.CreatorIsInterpreterUser ?? true,
                MealbreakIncluded = order.MealBreakIncluded ?? false,

            };
            if (request != null)
            {
                model.RequestCanBeCancelled = request.CanCancel;
                model.RequestCanBeReplaced = request.CanCreateReplacementOrderOnCancel;
                model.RequestCanBePrinted = request.CanPrint;
                model.RequestStatus = request.Status;
                model.BrokerName = request.Ranking.Broker.Name;
                model.BrokerOrganizationNumber = request.Ranking.Broker.OrganizationNumber;
                //don't use AnsweredBy since request for replacement order can have interpreter etc but not is answered
                model.ActiveRequestIsAnswered = request.InterpreterBrokerId != null && request.Status != BusinessLogic.Enums.RequestStatus.Created && request.Status != BusinessLogic.Enums.RequestStatus.Received;
                model.RequestId = request.RequestId;
                model.RequestGroupId = request.RequestGroupId;
                model.RequestIsApprovedOrDelivered = request.IsApprovedOrDelivered;
                if (model.ActiveRequestIsAnswered)
                {
                    model.CancelMessage = request.CancelMessage;
                    model.AnsweredBy = request.AnsweringUser?.CompleteContactInformation;
                    model.ExpectedTravelCostInfo = request.ExpectedTravelCostInfo;
                    if (request.InterpreterLocation.HasValue)
                    {
                        model.InterpreterLocationAnswer = (InterpreterLocation)request.InterpreterLocation.Value;
                    }
                    model.InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel)request.CompetenceLevel;
                    model.InterpreterName = request.Interpreter?.CompleteContactInformation;
                    model.TerminateOnDenial = request.TerminateOnDenial;

                }
            }
            return model;
        }

        internal static OrderViewModel GetModelFromOrderForConfirmation(Order order)
        {
            bool useRankedInterpreterLocation = order.InterpreterLocations.Count > 1;

            OrderCompetenceRequirement competenceFirst = null;
            OrderCompetenceRequirement competenceSecond = null;
            //Can get this from list on order since this is an order that has yet to be saved to database.
            var competenceRequirements = order.CompetenceRequirements.Select(r => new OrderCompetenceRequirement
            {
                CompetenceLevel = r.CompetenceLevel,
                Rank = r.Rank,
            }).ToList();

            competenceRequirements = competenceRequirements.OrderBy(r => r.Rank).ToList();
            competenceFirst = competenceRequirements.Count > 0 ? competenceRequirements[0] : null;
            competenceSecond = competenceRequirements.Count > 1 ? competenceRequirements[1] : null;

            return new OrderViewModel
            {
                //Dont like, should be possible to make lighter.
                AllowExceedingTravelCost = new RadioButtonGroup { SelectedItem = order.AllowExceedingTravelCost == null ? null : SelectListService.AllowExceedingTravelCost.Single(e => e.Value == order.AllowExceedingTravelCost.ToString()) },
                AssignmentType = order.AssignmentType,
                CreatorIsInterpreterUser = order.CreatorIsInterpreterUser.HasValue ? new RadioButtonGroup { SelectedItem = SelectListService.BoolList.Single(e => e.Value == (order.CreatorIsInterpreterUser.Value ? TrueFalse.Yes.ToString() : TrueFalse.No.ToString())) } : null,
                CustomerReferenceNumber = order.CustomerReferenceNumber,
                InvoiceReference = order.InvoiceReference,
                TimeRange = new TimeRange
                {
                    StartDateTime = order.StartAt,
                    EndDateTime = order.EndAt
                },
                Description = order.Description,
                UnitName = order.UnitName,
                LanguageHasAuthorizedInterpreter = order.LanguageHasAuthorizedInterpreter,
                CompetenceLevelDesireType = new RadioButtonGroup
                {
                    SelectedItem = order.SpecificCompetenceLevelRequired
                    ? SelectListService.DesireTypes.Single(item => EnumHelper.Parse<DesireType>(item.Value) == DesireType.Requirement)
                    : SelectListService.DesireTypes.Single(item => EnumHelper.Parse<DesireType>(item.Value) == DesireType.Request)
                },
                RequestedCompetenceLevelFirst = competenceFirst?.CompetenceLevel,
                RequestedCompetenceLevelSecond = competenceSecond?.CompetenceLevel,
                RankedInterpreterLocationFirst = order.InterpreterLocations.Single(l => l.Rank == 1)?.InterpreterLocation,
                RankedInterpreterLocationSecond = order.InterpreterLocations.SingleOrDefault(l => l.Rank == 2)?.InterpreterLocation,
                RankedInterpreterLocationThird = order.InterpreterLocations.SingleOrDefault(l => l.Rank == 3)?.InterpreterLocation,
                RankedInterpreterLocationFirstAddressModel = GetInterpreterLocation(order.InterpreterLocations.Single(l => l.Rank == 1)),
                RankedInterpreterLocationSecondAddressModel = GetInterpreterLocation(order.InterpreterLocations.SingleOrDefault(l => l.Rank == 2)),
                RankedInterpreterLocationThirdAddressModel = GetInterpreterLocation(order.InterpreterLocations.SingleOrDefault(l => l.Rank == 3)),
                OrderRequirements = order.Requirements.Select(r => new OrderRequirementModel
                {
                    OrderRequirementId = r.OrderRequirementId,
                    RequirementDescription = r.Description,
                    RequirementIsRequired = r.IsRequired,
                    RequirementType = r.RequirementType
                }).ToList(),
            };
        }

    }
}
