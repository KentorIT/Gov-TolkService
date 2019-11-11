using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class RequestGroupProcessModel : RequestGroupBaseModel
    {
        public int OrderGroupId { get; set; } 

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<FileModel> Files { get; set; }

        public AttachmentListModel AttachmentListModel { get; set; }

        public Guid? FileGroupKey { get; set; }

        public long? CombinedMaxSizeAttachments { get; set; }

        [Display(Name = "Orsak till avböjande")]
        [DataType(DataType.MultilineText)]
        [ClientRequired]
        [StringLength(1000)]
        [Placeholder("Beskriv anledning tydligt.")]
        public string DenyMessage { get; set; }

        [Display(Name = "Svara senast")]
        public DateTimeOffset ExpiresAt { get; set; }

        public string ColorClassName { get => CssClassHelper.GetColorClassNameForRequestStatus(Status); }

        //INTERPRETER REPLACEMENT
        public InterpreterAnswerModel InterpreterAnswerModel { get; set; }
        public InterpreterAnswerModel ExtraInterpreterAnswerModel { get; set; }

        [ClientRequired]
        [Display(Name = "Inställelsesätt")]
        public InterpreterLocation? InterpreterLocation { get; set; }

        [Display(Name = "Uppdragstyp")]
        public AssignmentType AssignmentType { get; set; }

        [Display(Name = "Språk och dialekt")]
        [DataType(DataType.MultilineText)]
        public string LanguageAndDialect => $"{LanguageName}\n{Dialect}";
        [Display(Name = "Region")]
        public string RegionName { get; set; }
        [Display(Name = "Myndighet")]
        public string CustomerName { get; set; }
        [Display(Name = "Skapad av")]
        [DataType(DataType.MultilineText)]
        public string CreatedBy { get; set; }
        [Display(Name = "Myndighetens enhet")]
        public string CustomerUnitName { get; set; }
        [Display(Name = "Myndighetens avdelning")]
        public string UnitName { get; set; }
        [Display(Name = "Myndighetens organisationsnummer")]
        public string CustomerOrganisationNumber { get; set; }
        [Display(Name = "Myndighetens ärendenummer")]
        public string CustomerReferenceNumber { get; set; }
        [Display(Name = "Övrig information om uppdraget")]
        public string Description { get; set; }

        public string LanguageName { get; set; }
        public string Dialect { get; set; }

        public bool SpecificCompetenceLevelRequired { get; set; }
        public bool LanguageHasAuthorizedInterpreter { get; set; }
        public bool HasExtraInterpreter { get; set; }
        public bool AllowExceedingTravelCost { get; set; }
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

        //ROWS with occasions!!
        public OccasionListModel OccasionList { get; set; }

        #region methods

        internal static RequestGroupProcessModel GetModelFromRequestGroup(RequestGroup requestGroup, Guid fileGroupKey, long combinedMaxSizeAttachments, int userId)
        {

            OrderGroup orderGroup = requestGroup.OrderGroup;
            Order order = requestGroup.OrderGroup.FirstOrder;
            var viewedByUser = requestGroup.Views.Any(rv => rv.ViewedBy != userId) ?
                requestGroup.Views.First(rv => rv.ViewedBy != userId).ViewedByUser.FullName + " håller också på med denna förfrågan"
                : string.Empty;
            return new RequestGroupProcessModel
            {
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

                //NEW
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
                    Occasions = orderGroup.Orders
                        .Select(o => OrderOccasionDisplayModel.GetModelFromOrder(o, PriceInformationModel.GetPriceinformationToDisplay(o)))
                },
                HasExtraInterpreter = requestGroup.HasExtraInterpreter,
                AllowExceedingTravelCost = orderGroup.AllowExceedingTravelCost == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldBeApproved || orderGroup.AllowExceedingTravelCost == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldNotBeApproved,
                AssignmentType = orderGroup.AssignmentType,
                CreatedBy = orderGroup.CreatedByUser.CompleteContactInformation,
                CustomerName = orderGroup.CustomerOrganisation.Name,
                //AttachmentListModel
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
