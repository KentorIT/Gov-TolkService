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
    public class OrderGroupModel : OrderBaseModel
    {
        public int? OrderGroupId { get; set; }

        [Display(Name = "Sammanhållet bokningsID")]
        public string OrderGroupNumber { get; set; }

        public int? RequestGroupId { get; set; }

        [Display(Name = "Uppdragstyp")]
        public AssignmentType AssignmentType { get; set; }

        public override DateTimeOffset? StartAt => OccasionList.FirstStartDateTime;

        public string ColorClassName => CssClassHelper.GetColorClassNameForOrderStatus(Status);

        [Display(Name = "Anledning till att den sammanhållna bokningen avbokas")]
        [DataType(DataType.MultilineText)]
        [ClientRequired]
        [StringLength(1000)]
        [Placeholder("Beskriv anledning till avbokning")]
        public string CancelMessage { get; set; }

        public bool ActiveRequestIsAnswered { get; set; } = false;

        [Display(Name = "Angiven bedömd resekostnad för extra tolk (exkl. moms)")]
        [DataType(DataType.Currency)]
        public decimal ExtraInterpreterExpectedTravelCosts { get; set; }

        [Display(Name = "Kommentar till bedömd resekostnad för extra tolk")]
        [DataType(DataType.MultilineText)]
        public string ExtraInterpreterExpectedTravelCostInfo { get; set; }

        public bool AllowCancellation { get; set; } = false;

        public bool AllowUpdateExpiry { get; set; } = false;

        [Display(Name = "Sista svarstid", Description = "Eftersom uppdraget sker i närtid, måste sista svarstid anges.")]
        [ClientRequired(ErrorMessage = "Ange sista svarstid")]
        public DateTimeOffset? LatestAnswerBy { get; set; }

        public CustomerInformationModel CustomerInformationModel { get; set; }

        public OccasionListModel OccasionList { get; set; }

        public RequestGroupViewModel ActiveRequestGroup { get; set; }

        public IEnumerable<BrokerListModel> PreviousRequestGroups { get; set; }

        #region methods

        internal static OrderGroupModel GetModelFromOrderGroup(OrderGroup orderGroup, Order firstOrder, RequestGroup activeRequestGroup, bool displayForBroker = false)
        {
            var model = new OrderGroupModel
            {
                AllowExceedingTravelCost = displayForBroker ? new RadioButtonGroup
                {
                    SelectedItem = orderGroup.AllowExceedingTravelCost == null ? null :
                    SelectListService.BoolList.Single(e => e.Value == EnumHelper.Parent<AllowExceedingTravelCost, TrueFalse>(orderGroup.AllowExceedingTravelCost.Value).ToString())
                } :
                    new RadioButtonGroup
                    {
                        SelectedItem = orderGroup.AllowExceedingTravelCost == null ? null :
                    SelectListService.AllowExceedingTravelCost.Single(e => e.Value == orderGroup.AllowExceedingTravelCost.ToString())
                    },
                IsCreatorInterpreterUser = orderGroup.CreatorIsInterpreterUser,

                Description = firstOrder.Description,

                CompetenceLevelDesireType = new RadioButtonGroup
                {
                    SelectedItem = orderGroup.SpecificCompetenceLevelRequired
                    ? SelectListService.DesireTypes.Single(item => EnumHelper.Parse<DesireType>(item.Value) == DesireType.Requirement)
                    : SelectListService.DesireTypes.Single(item => EnumHelper.Parse<DesireType>(item.Value) == DesireType.Request)
                },
                Status = orderGroup.Status,

                OrderGroupId = orderGroup.OrderGroupId,
                OrderGroupNumber = orderGroup.OrderGroupNumber,

                CreatedBy = orderGroup.ContactInformation,
                CreatedAt = orderGroup.CreatedAt,
                InvoiceReference = firstOrder.InvoiceReference,
                CustomerName = orderGroup.CustomerOrganisation.Name,
                CustomerOrganisationNumber = orderGroup.CustomerOrganisation.OrganisationNumber,
                CustomerReferenceNumber = firstOrder.CustomerReferenceNumber,
                LanguageName = orderGroup.OtherLanguage ?? orderGroup.Language?.Name ?? "-",
                CustomerUnitName = orderGroup.CustomerUnit?.Name ?? string.Empty,
                UnitName = firstOrder.UnitName,
                RegionName = orderGroup.Region.Name,
                LanguageHasAuthorizedInterpreter = orderGroup.LanguageHasAuthorizedInterpreter,

                RequestGroupId = activeRequestGroup?.RequestGroupId,
                AssignmentType = orderGroup.AssignmentType,
            };
            return model;
        }

        #endregion
    }
}
