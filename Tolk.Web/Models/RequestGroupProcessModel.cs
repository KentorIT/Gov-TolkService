using System;
using System.Collections.Generic;
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

        internal static RequestGroupProcessModel GetModelFromRequestGroup(RequestGroup requestGroup, Order order, Guid fileGroupKey, long combinedMaxSizeAttachments, int userId, bool allowDeclineExtraInterpreter)
        {
            OrderGroup orderGroup = requestGroup.OrderGroup;
            return new RequestGroupProcessModel
            {
                AllowDeclineExtraInterpreter = allowDeclineExtraInterpreter,
                OrderGroupId = requestGroup.OrderGroupId,
                RequestGroupId = requestGroup.RequestGroupId,
                BrokerId = requestGroup.Ranking.BrokerId,
                OrderGroupNumber = orderGroup.OrderGroupNumber,
                FileGroupKey = fileGroupKey,
                CombinedMaxSizeAttachments = combinedMaxSizeAttachments,
                CreatedAt = requestGroup.CreatedAt,
                ExpiresAt = requestGroup.ExpiresAt.Value,
                OrderHasAllowExceedingTravelCost = orderGroup.AllowExceedingTravelCost == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldBeApproved || orderGroup.AllowExceedingTravelCost == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldNotBeApproved,
                AllowExceedingTravelCost = new RadioButtonGroup { SelectedItem = orderGroup.AllowExceedingTravelCost == null ? null : SelectListService.BoolList.Single(e => e.Value == EnumHelper.Parent<AllowExceedingTravelCost, TrueFalse>(orderGroup.AllowExceedingTravelCost.Value).ToString()) },
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
                    ReferenceNumber = order.CustomerReferenceNumber
                },
                Description = order.Description,
                LanguageName = orderGroup.LanguageName,
                LanguageHasAuthorizedInterpreter = orderGroup.LanguageHasAuthorizedInterpreter,
                RegionName = orderGroup.Region.Name,
                SpecificCompetenceLevelRequired = orderGroup.SpecificCompetenceLevelRequired,
                Status = requestGroup.Status,
            };
        }

        #endregion
    }
}
