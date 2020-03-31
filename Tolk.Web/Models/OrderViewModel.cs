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

        [Display(Name = "Övrigt (annat) språk", Description = "Ange annat språk. Dialekt läggs till i fältet bredvid.")]

        public string OtherLanguage { get; set; }

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

        [Display(Name = "Status på aktiv förfrågan")]
        public RequestStatus? RequestStatus { get; set; }

        public int? RequestId { get; set; }

        public RequestModel ActiveRequest { get; set; }

        public IEnumerable<BrokerListModel> PreviousRequests { get; set; }

        public string CancelMessage { get; set; }


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


        public int NumberOfPreviousRequests { get; set; }
        public bool HasPreviousRequests => NumberOfPreviousRequests > 0;

        internal static OrderViewModel GetModelFromOrderAndRequest(Order order)
        {
            return new OrderViewModel
            {
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
                CreatedAt = order.CreatedAt,
                InvoiceReference = order.InvoiceReference,
                LanguageName = order.OtherLanguage ?? order.Language?.Name,
                CustomerUnitName = order.CustomerUnit?.Name,
                CustomerReferenceNumber = order.CustomerReferenceNumber,
                //Dialect = o.Requirements.Any(r => r.RequirementType == RequirementType.Dialect) ? o.Requirements.Single(r => r.RequirementType == RequirementType.Dialect).Description : string.Empty,
                LanguageHasAuthorizedInterpreter = order.LanguageHasAuthorizedInterpreter,
                CompetenceIsRequired = order.SpecificCompetenceLevelRequired,
                TimeRange = new TimeRange
                {
                    StartDateTime = order.StartAt,
                    EndDateTime = order.EndAt
                },
                Description = order.Description,
                UnitName = order.UnitName,
            };
        }
    }
}
