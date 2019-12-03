using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Services;
using System;

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


        [Display(Name = "Anledning till att bokningen avbokas")]
        [DataType(DataType.MultilineText)]
        [ClientRequired]
        [StringLength(1000)]
        [Placeholder("Beskriv anledning till avbokning.")]
        public string CancelMessage { get; set; }

        public bool AllowNoAnswerConfirmation { get; set; } = false;
        public bool ActiveRequestIsAnswered { get; set; } = false;

        [Display(Name = "Angiven bedömd resekostnad för extra tolk (exkl. moms)")]
        [DataType(DataType.Currency)]
        public decimal ExtraInterpreterExpectedTravelCosts { get; set; }

        [Display(Name = "Kommentar till bedömd resekostnad för extra tolk")]
        [DataType(DataType.MultilineText)]
        public string ExtraInterpreterExpectedTravelCostInfo { get; set; }

        public bool AllowOrderGroupCancellation { get; set; } = false;
        public bool AllowPrint { get; set; } = false;
        public bool AllowDenial { get; set; } = false;
        public bool AllowUpdateExpiry { get; set; } = false;

        [Display(Name = "Sista svarstid", Description = "Eftersom uppdraget sker i närtid, måste sista svarstid anges.")]
        [ClientRequired(ErrorMessage = "Ange sista svarstid")]
        public DateTimeOffset? LatestAnswerBy { get; set; }

        public CustomerInformationModel CustomerInformationModel { get; set; }

        public OccasionListModel OccasionList { get; set; }

        public RequestGroupViewModel ActiveRequestGroup { get; set; }

        public IEnumerable<BrokerListModel> PreviousRequestGroups { get; set; }

        #region methods

        internal static OrderGroupModel GetModelFromOrderGroup(OrderGroup orderGroup, RequestGroup activeRequestGroup)
        {
            bool useRankedInterpreterLocation = orderGroup.FirstOrder.InterpreterLocations.Count > 1;
            OrderCompetenceRequirement competenceFirst = null;
            OrderCompetenceRequirement competenceSecond = null;
            var competenceRequirements = orderGroup.CompetenceRequirements.Select(r => new OrderCompetenceRequirement
            {
                CompetenceLevel = r.CompetenceLevel,
                Rank = r.Rank,
            }).ToList();
            competenceRequirements = competenceRequirements.OrderBy(r => r.Rank).ToList();
            competenceFirst = competenceRequirements.Count > 0 ? competenceRequirements[0] : null;
            competenceSecond = competenceRequirements.Count > 1 ? competenceRequirements[1] : null;

            var model = new OrderGroupModel
            {
                AllowExceedingTravelCost = new RadioButtonGroup { SelectedItem = orderGroup.FirstOrder.AllowExceedingTravelCost == null ? null : SelectListService.AllowExceedingTravelCost.Single(e => e.Value == orderGroup.FirstOrder.AllowExceedingTravelCost.ToString()) },

                Description = orderGroup.FirstOrder.Description,

                CompetenceLevelDesireType = new RadioButtonGroup
                {
                    SelectedItem = orderGroup.SpecificCompetenceLevelRequired
                    ? SelectListService.DesireTypes.Single(item => EnumHelper.Parse<DesireType>(item.Value) == DesireType.Requirement)
                    : SelectListService.DesireTypes.Single(item => EnumHelper.Parse<DesireType>(item.Value) == DesireType.Request)
                },
                RequestedCompetenceLevelFirst = competenceFirst?.CompetenceLevel,
                RequestedCompetenceLevelSecond = competenceSecond?.CompetenceLevel,
                Status = orderGroup.Status,

                RankedInterpreterLocationFirst = orderGroup.FirstOrder.InterpreterLocations.Single(l => l.Rank == 1)?.InterpreterLocation,
                RankedInterpreterLocationSecond = orderGroup.FirstOrder.InterpreterLocations.SingleOrDefault(l => l.Rank == 2)?.InterpreterLocation,
                RankedInterpreterLocationThird = orderGroup.FirstOrder.InterpreterLocations.SingleOrDefault(l => l.Rank == 3)?.InterpreterLocation,
                RankedInterpreterLocationFirstAddressModel = GetInterpreterLocation(orderGroup.FirstOrder.InterpreterLocations.Single(l => l.Rank == 1)),
                RankedInterpreterLocationSecondAddressModel = GetInterpreterLocation(orderGroup.FirstOrder.InterpreterLocations.SingleOrDefault(l => l.Rank == 2)),
                RankedInterpreterLocationThirdAddressModel = GetInterpreterLocation(orderGroup.FirstOrder.InterpreterLocations.SingleOrDefault(l => l.Rank == 3)),
                OrderRequirements = orderGroup.Requirements.Select(r => new OrderRequirementModel
                {
                    OrderRequirementId = r.OrderGroupRequirementId,
                    RequirementDescription = r.Description,
                    RequirementIsRequired = r.IsRequired,
                    RequirementType = r.RequirementType,
                    CanSatisfyRequirement = true,
                    Answer = "Yes we can!"
                    //CanSatisfyRequirement = r.?.SingleOrDefault(a => a.RequestId == activeRequestId)?.CanSatisfyRequirement,
                    //Answer = r.RequirementAnswers?.SingleOrDefault(a => a.RequestId == activeRequestId)?.Answer
                }).ToList(),
                AttachmentListModel = new AttachmentListModel
                {
                    AllowDelete = false,
                    AllowDownload = true,
                    AllowUpload = false,
                    Title = "Bifogade filer från myndighet",
                    DisplayFiles = orderGroup.Attachments.Select(a => new FileModel
                    {
                        Id = a.Attachment.AttachmentId,
                        FileName = a.Attachment.FileName,
                        Size = a.Attachment.Blob.Length
                    }).ToList()
                },

                OrderGroupId = orderGroup.OrderGroupId,
                OrderGroupNumber = orderGroup.OrderGroupNumber,

                CreatedBy = orderGroup.ContactInformation,
                CreatedAt = orderGroup.CreatedAt,
                InvoiceReference = orderGroup.FirstOrder.InvoiceReference,
                CustomerName = orderGroup.CustomerOrganisation.Name,
                CustomerOrganisationNumber = orderGroup.CustomerOrganisation.OrganisationNumber,
                CustomerReferenceNumber = orderGroup.FirstOrder.CustomerReferenceNumber,
                LanguageName = orderGroup.OtherLanguage ?? orderGroup.Language?.Name ?? "-",
                CustomerUnitName = orderGroup.CustomerUnit?.Name ?? string.Empty,
                UnitName = orderGroup.FirstOrder.UnitName,
                RegionName = orderGroup.Region.Name,
                LanguageHasAuthorizedInterpreter = orderGroup.LanguageHasAuthorizedInterpreter,

                RequestGroupId = activeRequestGroup?.RequestGroupId,
                AssignmentType = orderGroup.AssignmentType,
                OccasionList = new OccasionListModel
                {
                    Occasions = orderGroup.Orders
                         .Select(o => OrderOccasionDisplayModel.GetModelFromOrder(o, PriceInformationModel.GetPriceinformationToDisplay(o))),
                    AllOccasions = orderGroup.Orders.Select(o => OrderOccasionDisplayModel.GetModelFromOrder(o)),
                    DisplayDetailedList = true
                },

                //those values should only be presented if ordergroup should be approved/denied since they could be changed (or display that this is the first occasions cost)?
                ExpectedTravelCostInfo = activeRequestGroup?.FirstRequestForFirstInterpreter.ExpectedTravelCostInfo,
                ExpectedTravelCosts = activeRequestGroup?.FirstRequestForFirstInterpreter.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0,
                ExtraInterpreterExpectedTravelCostInfo = activeRequestGroup.HasExtraInterpreter ? activeRequestGroup?.FirstRequestForExtraInterpreter.ExpectedTravelCostInfo : null,
                ExtraInterpreterExpectedTravelCosts = activeRequestGroup.HasExtraInterpreter ? activeRequestGroup?.FirstRequestForExtraInterpreter.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0 : 0,
                PreviousRequestGroups = orderGroup.RequestGroups.Where(r =>
                      r.Status == RequestStatus.DeclinedByBroker ||
                      r.Status == RequestStatus.DeniedByTimeLimit ||
                      r.Status == RequestStatus.DeniedByCreator ||
                      r.Status == RequestStatus.LostDueToQuarantine
                ).Select(r => new BrokerListModel
                {
                    Status = r.Status,
                    BrokerName = r.Ranking.Broker.Name,
                    DenyMessage = r.DenyMessage,
                }).ToList(),

            };
            return model;
        }

        #endregion
    }
}
