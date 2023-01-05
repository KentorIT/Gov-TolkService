using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;
using Tolk.Web.Services;

namespace Tolk.Web.Models
{
    public class RequestGroupProcessModel : RequestGroupBaseModel
    {
        //The following two properties will be used later, if AllowDeclineExtraInterpreter is allowed.
        public bool ShouldAssignInterpreter { get; set; } = true;

        public bool ShouldAssignExtraInterpreter { get; set; } = true;

        public bool AllowDeclineExtraInterpreter { get; set; }

        public bool AllowAccept { get; set; }

        [Display(Name = "Tillsätt tolk direkt", Description = "Detta är en förfrågan med lång framförhållning, där ni som förmedling inte behöver tillsätta tolk i första svaret. Men om ni gör det så kommer det anses som en fullständig tillsättning, vilket gör att ni inte behöver tillsätta tolken senare.")]
        public bool FullAnswer { get; set; } = true;

        [ClientRequired]
        [Display(Name = "Inställelsesätt")]
        public InterpreterLocation? InterpreterLocationOnAccept { get; set; }

        public InterpreterAcceptModel InterpreterAcceptModel { get; set; }

        public InterpreterAcceptModel ExtraInterpreterAcceptModel { get; set; }

        public IEnumerable<CompetenceAndSpecialistLevel> RequestedCompetenceLevels
        {
            get
            {
                if (RequestedCompetenceLevelFirst.HasValue)
                {
                    yield return RequestedCompetenceLevelFirst.Value;
                }
                if (RequestedCompetenceLevelSecond.HasValue)
                {
                    yield return RequestedCompetenceLevelSecond.Value;
                }
            }
        }

        [Prefix(PrefixPosition = PrefixAttribute.Position.Value, Text = "<span class=\"competence-ranking-num\">1. </span>")]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelFirst { get; set; }

        [NoDisplayName]
        [Prefix(PrefixPosition = PrefixAttribute.Position.Value, Text = "<span class=\"competence-ranking-num\">2. </span>")]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelSecond { get; set; }
        public InterpreterLocationAddressModel RankedInterpreterLocationFirstAddressModel { get; set; }
        public InterpreterLocationAddressModel RankedInterpreterLocationSecondAddressModel { get; set; }
        public InterpreterLocationAddressModel RankedInterpreterLocationThirdAddressModel { get; set; }

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

        public bool IsOnSiteOrOffSiteDesignatedLocationSelected => RankedInterpreterLocations.Any(i => i == BusinessLogic.Enums.InterpreterLocation.OnSite || i == BusinessLogic.Enums.InterpreterLocation.OffSiteDesignatedLocation);

        #region methods

        internal static RequestGroupProcessModel GetModelFromRequestGroup(RequestGroup requestGroup, Guid fileGroupKey, long combinedMaxSizeAttachments, bool allowDeclineExtraInterpreter)
        {
            OrderGroup orderGroup = requestGroup.OrderGroup;
            Order order = requestGroup.Requests.First().Order;
            return new RequestGroupProcessModel
            {
                AllowAccept = requestGroup.CanAccept,
                FullAnswer = !requestGroup.CanAccept,
                AllowDeclineExtraInterpreter = allowDeclineExtraInterpreter,
                OrderGroupId = requestGroup.OrderGroupId,
                RequestGroupId = requestGroup.RequestGroupId,
                BrokerId = requestGroup.Ranking.BrokerId,
                OrderGroupNumber = orderGroup.OrderGroupNumber,
                FileGroupKey = fileGroupKey,
                CombinedMaxSizeAttachments = combinedMaxSizeAttachments,
                CreatedAt = requestGroup.CreatedAt,
                ExpiresAt = requestGroup.ExpiresAt.Value,
                LastAcceptAt = requestGroup.LastAcceptAt,
                OccasionList = new OccasionListModel
                {
                    Occasions = requestGroup.Requests.Where(r => r.Status != RequestStatus.InterpreterReplaced)
                        .Select(r => OrderOccasionDisplayModel.GetModelFromOrder(r.Order, PriceInformationModel.GetPriceinformationToDisplay(r.Order, alwaysUseOrderPriceRows: false), r)),
                    AllOccasions = orderGroup.Orders.Select(o => OrderOccasionDisplayModel.GetModelFromOrder(o, request: o.Requests.OrderBy(re => re.RequestId).Last()))
                },
                HasExtraInterpreter = requestGroup.HasExtraInterpreter,
                OrderHasAllowExceedingTravelCost = orderGroup.AllowExceedingTravelCost == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldBeApproved || orderGroup.AllowExceedingTravelCost == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldNotBeApproved,
                AllowExceedingTravelCost = new RadioButtonGroup { SelectedItem = orderGroup.FirstOrder.AllowExceedingTravelCost == null ? null : SelectListService.BoolList.Single(e => e.Value == EnumHelper.Parent<AllowExceedingTravelCost, TrueFalse>(orderGroup.FirstOrder.AllowExceedingTravelCost.Value).ToString()) },
                CreatorIsInterpreterUser = orderGroup.CreatorIsInterpreterUser.HasValue ? new RadioButtonGroup { SelectedItem = SelectListService.BoolList.Single(e => e.Value == (orderGroup.CreatorIsInterpreterUser.Value ? TrueFalse.Yes.ToString() : TrueFalse.No.ToString())) } : null,
                AssignmentType = orderGroup.AssignmentType,
                CustomerInformationModel = new CustomerInformationModel
                {
                    CreatedBy = orderGroup.CreatedByUser.CompleteContactInformation,
                    Name = orderGroup.CustomerOrganisation.Name,
                    UnitName = orderGroup.CustomerUnit?.Name,
                    DepartmentName = order.UnitName,
                    InvoiceReference = order.InvoiceReference,
                    OrganisationNumber = orderGroup.CustomerOrganisation.OrganisationNumber,
                    PeppolId = orderGroup.CustomerOrganisation.PeppolId,
                    ReferenceNumber = order.CustomerReferenceNumber
                },
                Description = order.Description,
                LanguageName = orderGroup.LanguageName,
                LanguageHasAuthorizedInterpreter = orderGroup.LanguageHasAuthorizedInterpreter,
                RankedInterpreterLocationFirstAddressModel = OrderModel.GetInterpreterLocation(order.InterpreterLocations.Single(l => l.Rank == 1)),
                RankedInterpreterLocationSecondAddressModel = OrderModel.GetInterpreterLocation(order.InterpreterLocations.SingleOrDefault(l => l.Rank == 2)),
                RankedInterpreterLocationThirdAddressModel = OrderModel.GetInterpreterLocation(order.InterpreterLocations.SingleOrDefault(l => l.Rank == 3)),
                RegionName = orderGroup.Region.Name,
                SpecificCompetenceLevelRequired = orderGroup.SpecificCompetenceLevelRequired,
                Status = requestGroup.Status,
                TravelConditionHours = EnumHelper.GetContractDefinition((FrameworkAgreementResponseRuleset)requestGroup?.Ranking.FrameworkAgreementId).TravelConditionHours,
                TravelConditionKilometers = EnumHelper.GetContractDefinition((FrameworkAgreementResponseRuleset)requestGroup?.Ranking.FrameworkAgreementId).TravelConditionKilometers,
                FrameworkAgreementNumberOnCreated = requestGroup.Requests.First().Ranking.FrameworkAgreement.AgreementNumber
            };
        }

        #endregion
    }
}
