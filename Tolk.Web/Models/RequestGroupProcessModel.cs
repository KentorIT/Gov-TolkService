using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class RequestGroupProcessModel : RequestGroupBaseModel
    {


        //ROWS with occasions!!
        public OccasionListModel OccasionList { get; set; }

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

        #region methods

        internal static RequestGroupProcessModel GetModelFromRequestGroup(RequestGroup requestGroup, Guid fileGroupKey, long combinedMaxSizeAttachments, int userId, bool allowDeclineExtraInterpreter)
        {
            OrderGroup orderGroup = requestGroup.OrderGroup;
            Order order = requestGroup.Requests.First().Order;
            var viewedByUser = requestGroup.Views.Any(rv => rv.ViewedBy != userId) ?
                requestGroup.Views.First(rv => rv.ViewedBy != userId).ViewedByUser.FullName + " håller också på med denna förfrågan"
                : string.Empty;
            return new RequestGroupProcessModel
            {
                AllowDeclineExtraInterpreter = allowDeclineExtraInterpreter,
                ViewedByUser = viewedByUser,
                OrderGroupId = requestGroup.OrderGroupId,
                RequestGroupId = requestGroup.RequestGroupId,
                BrokerId = requestGroup.Ranking.BrokerId,
                OrderGroupNumber = orderGroup.OrderGroupNumber,
                FileGroupKey = fileGroupKey,
                CombinedMaxSizeAttachments = combinedMaxSizeAttachments,
                CreatedAt = requestGroup.CreatedAt,
                ExpiresAt = requestGroup.ExpiresAt.Value,
                AttachmentListModel = new AttachmentListModel
                {
                    AllowDelete = false,
                    AllowDownload = true,
                    AllowUpload = false,
                    DisplayFiles = orderGroup.Attachments.Select(a => new FileModel
                    {
                        Id = a.Attachment.AttachmentId,
                        FileName = a.Attachment.FileName,
                        Size = a.Attachment.Blob.Length
                    }).ToList()
                },
                InterpreterAnswerModel = new InterpreterAnswerModel
                {
                    RequiredRequirementAnswers = orderGroup.Requirements.Where(r => r.IsRequired).Select(r => new RequestRequirementAnswerModel
                    {
                        OrderRequirementId = r.OrderGroupRequirementId,
                        IsRequired = true,
                        Description = r.Description,
                        RequirementType = r.RequirementType,
                    }).ToList(),
                    DesiredRequirementAnswers = orderGroup.Requirements.Where(r => !r.IsRequired).Select(r => new RequestRequirementAnswerModel
                    {
                        OrderRequirementId = r.OrderGroupRequirementId,
                        IsRequired = false,
                        Description = r.Description,
                        RequirementType = r.RequirementType,
                    }).ToList(),
                },
                ExtraInterpreterAnswerModel = requestGroup.HasExtraInterpreter ? new InterpreterAnswerModel
                {
                    RequiredRequirementAnswers = orderGroup.Requirements.Where(r => r.IsRequired).Select(r => new RequestRequirementAnswerModel
                    {
                        OrderRequirementId = r.OrderGroupRequirementId,
                        IsRequired = true,
                        Description = r.Description,
                        RequirementType = r.RequirementType,
                    }).ToList(),
                    DesiredRequirementAnswers = orderGroup.Requirements.Where(r => !r.IsRequired).Select(r => new RequestRequirementAnswerModel
                    {
                        OrderRequirementId = r.OrderGroupRequirementId,
                        IsRequired = false,
                        Description = r.Description,
                        RequirementType = r.RequirementType,
                    }).ToList(),
                } : null,
                OccasionList = new OccasionListModel
                {
                    Occasions = requestGroup.Requests.Select(r => r.Order)
                        .Select(o => OrderOccasionDisplayModel.GetModelFromOrder(o, PriceInformationModel.GetPriceinformationToDisplay(o))),
                    AllOccasions = orderGroup.Orders.Select(o => OrderOccasionDisplayModel.GetModelFromOrder(o))
                },
                HasExtraInterpreter = requestGroup.HasExtraInterpreter,
                AllowExceedingTravelCost = orderGroup.AllowExceedingTravelCost == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldBeApproved || orderGroup.AllowExceedingTravelCost == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldNotBeApproved,
                AssignmentType = orderGroup.AssignmentType,
                CreatedBy = orderGroup.CreatedByUser.CompleteContactInformation,
                CustomerName = orderGroup.CustomerOrganisation.Name,
                CustomerOrganisationNumber = orderGroup.CustomerOrganisation.OrganisationNumber,
                CustomerReferenceNumber = order.CustomerReferenceNumber,
                CustomerUnitName = orderGroup.CustomerUnit?.Name,
                Description = order.Description,
                LanguageName = orderGroup.LanguageName,
                Dialect = orderGroup.Requirements.Any(r => r.RequirementType == RequirementType.Dialect) ? orderGroup.Requirements.Single(r => r.RequirementType == RequirementType.Dialect)?.Description : string.Empty,
                LanguageHasAuthorizedInterpreter = orderGroup.LanguageHasAuthorizedInterpreter,
                RankedInterpreterLocationFirstAddressModel = OrderModel.GetInterpreterLocation(order.InterpreterLocations.Single(l => l.Rank == 1)),
                RankedInterpreterLocationSecondAddressModel = OrderModel.GetInterpreterLocation(order.InterpreterLocations.SingleOrDefault(l => l.Rank == 2)),
                RankedInterpreterLocationThirdAddressModel = OrderModel.GetInterpreterLocation(order.InterpreterLocations.SingleOrDefault(l => l.Rank == 3)),
                RegionName = orderGroup.Region.Name,
                SpecificCompetenceLevelRequired = orderGroup.SpecificCompetenceLevelRequired,
                Status = requestGroup.Status,
                UnitName = order.UnitName,
                RequestedCompetenceLevelFirst = orderGroup.CompetenceRequirements.SingleOrDefault(l => l.Rank == 1 || l.Rank == null)?.CompetenceLevel,
                RequestedCompetenceLevelSecond = orderGroup.CompetenceRequirements.SingleOrDefault(l => l.Rank == 2)?.CompetenceLevel,
            };
        }


        #endregion
    }
}
