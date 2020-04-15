using System;
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

        public bool AllowDenial => AllowExceedingTravelCost != null && EnumHelper.Parse<AllowExceedingTravelCost>(AllowExceedingTravelCost.SelectedItem.Value) == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldBeApproved;

        public bool AllowEditContactPerson { get; set; } = false;

        public bool AllowUpdate { get; set; } = false;

        [Display(Name = "Skapa ersättningsuppdrag")]
        public bool AddReplacementOrder { get; set; } = false;

        public bool AllowComplaintCreation { get; set; } = false;

        public bool AllowRequestPrint { get; set; } = false;

        public bool AllowNoAnswerConfirmation { get; set; } = false;

        public bool AllowResponseNotAnsweredConfirmation { get; set; } = false;

        public bool AllowUpdateExpiry { get; set; } = false;

        public bool AllowConfirmCancellation { get; set; } = false;

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

        internal static OrderViewModel GetModelFromOrder(Order order)
        {
            return new OrderViewModel
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
                IsCreatorInterpreterUser = order.CreatorIsInterpreterUser ?? true
            };
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
