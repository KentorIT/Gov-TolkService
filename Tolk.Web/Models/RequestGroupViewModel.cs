using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class RequestGroupViewModel : RequestGroupBaseModel
    {
        public bool AllowConfirmationDenial { get; set; } = false;
        public bool AllowConfirmNoAnswer { get; set; } = false;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<RequestRequirementAnswerModel> RequirementAnswers { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<RequestRequirementAnswerModel> RequiredRequirementAnswers { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<RequestRequirementAnswerModel> DesiredRequirementAnswers { get; set; }

        [Display(Name = "Tillsatt tolk")]
        [DataType(DataType.MultilineText)]
        public string Interpreter { get; set; }

        [Display(Name = "Tillsatt extra tolk")]
        [DataType(DataType.MultilineText)]
        public string ExtraInterpreter { get; set; }

        public bool? IsInterpreterVerified { get; set; }

        public bool? IsExtraInterpreterVerified { get; set; }

        public string InterpreterVerificationMessage { get; set; }

        public string ExtraInterpreterVerificationMessage { get; set; }

        public bool DisplayExpectedTravelCostInfo { get; set; }

        [Display(Name = "Förmedling")]
        public string BrokerName { get; set; }

        [Display(Name = "Förmedlings organisationsnummer")]
        public string BrokerOrganizationNumber { get; set; }

        [Display(Name = "Förfrågan besvarad av")]
        [DataType(DataType.MultilineText)]
        public string AnsweredBy { get; set; }

        [Display(Name = "Bedömd resekostnad per tillfälle")]
        [DataType(DataType.Currency)]
        public decimal? ExpectedTravelCosts { get; set; }

        [Display(Name = "Kommentar till bedömd resekostnad")]
        [DataType(DataType.MultilineText)]
        public string ExpectedTravelCostInfo { get; set; }

        [Display(Name = "Bedömd resekostnad per tillfälle")]
        [DataType(DataType.Currency)]
        public decimal? ExtraInterpreterExpectedTravelCosts { get; set; }

        [Display(Name = "Kommentar till bedömd resekostnad")]
        [DataType(DataType.MultilineText)]
        public string ExtraInterpreterExpectedTravelCostInfo { get; set; }

        [Display(Name = "Status på bokning")]
        public OrderStatus OrderStatus { get; set; }

        [Required]
        [Display(Name = "Tolks kompetensnivå")]
        public CompetenceAndSpecialistLevel? InterpreterCompetenceLevel { get; set; }

        [Required]
        [Display(Name = "Tolks kompetensnivå")]
        public CompetenceAndSpecialistLevel? ExtraInterpreterCompetenceLevel { get; set; }

        public RequestStatus? ExtraInterpreterStatus { get; set; }

        public bool RequestIsAnswered => Status != RequestStatus.InterpreterReplaced && Status != RequestStatus.Created && Status != RequestStatus.Received;

        public bool RequestIsDeclinedByBroker => Status == RequestStatus.DeclinedByBroker || Status == RequestStatus.DeniedByTimeLimit;

        public bool ExtraInterpreterRequestIsAnswered => ExtraInterpreterStatus != RequestStatus.InterpreterReplaced && ExtraInterpreterStatus != RequestStatus.Created && ExtraInterpreterStatus != RequestStatus.Received;

        public bool ExtraInterpreterRequestIsDeclinedByBroker => ExtraInterpreterStatus == RequestStatus.DeclinedByBroker || ExtraInterpreterStatus == RequestStatus.DeniedByTimeLimit;

        public OrderBaseModel OrderGroupModel { get; set; }

        #region methods


        internal static RequestGroupViewModel GetModelFromRequestGroup(RequestGroup requestGroup, bool isCustomer = true)
        {
            OrderGroup orderGroup = requestGroup.OrderGroup;
            Order order = requestGroup.FirstRequestForFirstInterpreter.Order;
            Request request = requestGroup.FirstRequestForFirstInterpreter;
            Request requestExtraInterpreter = requestGroup.HasExtraInterpreter ? requestGroup.FirstRequestForExtraInterpreter : null;
            Order orderExtraInterpreter = requestExtraInterpreter?.Order;

            var verificationResult = request.InterpreterCompetenceVerificationResultOnStart ?? request.InterpreterCompetenceVerificationResultOnAssign;
            bool? isInterpreterVerified = verificationResult.HasValue ? (bool?)(verificationResult == VerificationResult.Validated) : null;
            var extraInterpreterVerificationResult = requestExtraInterpreter != null ? (requestExtraInterpreter.InterpreterCompetenceVerificationResultOnStart ?? requestExtraInterpreter.InterpreterCompetenceVerificationResultOnAssign) : null;
            bool? isExtraInterpreterVerified = extraInterpreterVerificationResult.HasValue ? (bool?)(extraInterpreterVerificationResult == VerificationResult.Validated) : null;

            return new RequestGroupViewModel
            {
                AllowConfirmationDenial = requestGroup.Status == RequestStatus.DeniedByCreator && !requestGroup.StatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.DeniedByCreator),
                AllowConfirmNoAnswer = requestGroup.Status == RequestStatus.ResponseNotAnsweredByCreator && !requestGroup.StatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.ResponseNotAnsweredByCreator),
                AssignmentType = order.AssignmentType,
                CreatedAt = orderGroup.CreatedAt,
                OrderGroupId = requestGroup.OrderGroupId,
                RequestGroupId = requestGroup.RequestGroupId,
                OrderGroupNumber = orderGroup.OrderGroupNumber,
                AnsweredBy = requestGroup.AnsweringUser?.CompleteContactInformation,
                BrokerName = requestGroup.Ranking.Broker.Name,
                BrokerOrganizationNumber = requestGroup.Ranking?.Broker.OrganizationNumber,
                DenyMessage = requestGroup.DenyMessage,
                CancelMessage = requestGroup.CancelMessage,
                ExpiresAt = requestGroup.ExpiresAt,
                LatestAnswerTimeForCustomer = requestGroup.LatestAnswerTimeForCustomer,
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
                OccasionList = new OccasionListModel
                {
                    Occasions = requestGroup.Requests.Where(r => r.Status != RequestStatus.InterpreterReplaced)
                        .Select(r => OrderOccasionDisplayModel.GetModelFromOrder(r.Order, PriceInformationModel.GetPriceinformationToDisplay(r.Order, alwaysUseOrderPriceRows: false), request: isCustomer ? null : r)),
                    AllOccasions = orderGroup.Orders.Select(o => OrderOccasionDisplayModel.GetModelFromOrder(o, request: isCustomer ? null : o.Requests.OrderBy(re => re.RequestId).Last())),
                    DisplayDetailedList = true
                },
                InterpreterAnswerModel = new InterpreterAnswerModel
                {
                    RequiredRequirementAnswers = order.Requirements.Where(r => r.IsRequired).Select(r => new RequestRequirementAnswerModel
                    {
                        OrderRequirementId = r.OrderRequirementId,
                        IsRequired = true,
                        Description = r.Description,
                        RequirementType = r.RequirementType,
                        Answer = request.RequirementAnswers != null ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId)?.Answer : string.Empty,
                        CanMeetRequirement = request.RequirementAnswers != null ? request.RequirementAnswers.Any() ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId).CanSatisfyRequirement : false : false,
                    }).ToList(),
                    DesiredRequirementAnswers = order.Requirements.Where(r => !r.IsRequired).Select(r => new RequestRequirementAnswerModel
                    {
                        OrderRequirementId = r.OrderRequirementId,
                        IsRequired = false,
                        Description = r.Description,
                        RequirementType = r.RequirementType,
                        Answer = request.RequirementAnswers != null ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId)?.Answer : string.Empty,
                        CanMeetRequirement = request.RequirementAnswers != null ? request.RequirementAnswers.Any() ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId).CanSatisfyRequirement : false : false,
                    }).ToList(),
                },
                ExtraInterpreterAnswerModel = requestGroup.HasExtraInterpreter ? new InterpreterAnswerModel
                {
                    RequiredRequirementAnswers = orderExtraInterpreter.Requirements.Where(r => r.IsRequired).Select(r => new RequestRequirementAnswerModel
                    {
                        OrderRequirementId = r.OrderRequirementId,
                        IsRequired = true,
                        Description = r.Description,
                        RequirementType = r.RequirementType,
                        Answer = requestExtraInterpreter.RequirementAnswers != null ? requestExtraInterpreter.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId)?.Answer : string.Empty,
                        CanMeetRequirement = requestExtraInterpreter.RequirementAnswers != null ? requestExtraInterpreter.RequirementAnswers.Any() ? requestExtraInterpreter.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId).CanSatisfyRequirement : false : false,
                    }).ToList(),
                    DesiredRequirementAnswers = orderExtraInterpreter.Requirements.Where(r => !r.IsRequired).Select(r => new RequestRequirementAnswerModel
                    {
                        OrderRequirementId = r.OrderRequirementId,
                        IsRequired = false,
                        Description = r.Description,
                        RequirementType = r.RequirementType,
                        Answer = requestExtraInterpreter.RequirementAnswers != null ? requestExtraInterpreter.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId)?.Answer : string.Empty,
                        CanMeetRequirement = requestExtraInterpreter.RequirementAnswers != null ? requestExtraInterpreter.RequirementAnswers.Any() ? requestExtraInterpreter.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId).CanSatisfyRequirement : false : false,
                    }).ToList(),
                } : null,
                HasExtraInterpreter = requestGroup.HasExtraInterpreter,
                Description = order.Description,
                LanguageName = orderGroup.LanguageName,
                Dialect = order.Requirements.Any(r => r.RequirementType == RequirementType.Dialect) ? order.Requirements.Single(r => r.RequirementType == RequirementType.Dialect)?.Description : string.Empty,
                LanguageHasAuthorizedInterpreter = orderGroup.LanguageHasAuthorizedInterpreter,
                InterpreterLocation = request.InterpreterLocation.HasValue ? (InterpreterLocation?)request.InterpreterLocation.Value : null,
                DisplayExpectedTravelCostInfo = GetDisplayExpectedTravelCostInfo(request.Order, request.InterpreterLocation ?? 0),
                Interpreter = request.Interpreter?.CompleteContactInformation,
                ExtraInterpreter = requestGroup.HasExtraInterpreter ? requestExtraInterpreter.Interpreter?.CompleteContactInformation : null,

                IsInterpreterVerified = verificationResult.HasValue ? (bool?)(verificationResult == VerificationResult.Validated) : null,
                IsExtraInterpreterVerified = extraInterpreterVerificationResult.HasValue ? (bool?)(extraInterpreterVerificationResult == VerificationResult.Validated) : null,
                InterpreterVerificationMessage = verificationResult.HasValue ? verificationResult.Value.GetDescription() : null,
                ExtraInterpreterVerificationMessage = extraInterpreterVerificationResult.HasValue ? extraInterpreterVerificationResult.Value.GetDescription() : null,
                InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)request.CompetenceLevel,
                ExtraInterpreterCompetenceLevel = requestGroup.HasExtraInterpreter ? (CompetenceAndSpecialistLevel?)requestExtraInterpreter.CompetenceLevel : null,
                ExpectedTravelCosts = request.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0,
                ExpectedTravelCostInfo = request.ExpectedTravelCostInfo,
                ExtraInterpreterExpectedTravelCosts = requestGroup.HasExtraInterpreter ? requestExtraInterpreter.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0 : 0,
                ExtraInterpreterExpectedTravelCostInfo = requestGroup.HasExtraInterpreter ? requestExtraInterpreter.ExpectedTravelCostInfo : null,
                RegionName = orderGroup.Region.Name,
                SpecificCompetenceLevelRequired = orderGroup.SpecificCompetenceLevelRequired,
                Status = requestGroup.Status,
                ExtraInterpreterStatus = requestExtraInterpreter?.Status,
                OrderStatus = orderGroup.Status,
            };
        }

        private static bool GetDisplayExpectedTravelCostInfo(Order o, int locationAnswer)
        {
            if (o.AllowExceedingTravelCost.HasValue && (o.AllowExceedingTravelCost.Value == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldBeApproved || o.AllowExceedingTravelCost.Value == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldNotBeApproved))
            {
                return locationAnswer == (int)BusinessLogic.Enums.InterpreterLocation.OnSite || locationAnswer == (int)BusinessLogic.Enums.InterpreterLocation.OffSiteDesignatedLocation;
            }
            return false;
        }

        #endregion
    }
}
