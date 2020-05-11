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

        internal static RequestGroupViewModel GetModelFromRequestGroup(OrderGroup orderGroup, RequestGroup requestGroup, Request request, Request requestExtraInterpreter)
        {
            Order order = request.Order;
            Order orderExtraInterpreter = requestExtraInterpreter?.Order;

            var verificationResult = request.InterpreterCompetenceVerificationResultOnStart ?? request.InterpreterCompetenceVerificationResultOnAssign;
            bool? isInterpreterVerified = verificationResult.HasValue ? (bool?)(verificationResult == VerificationResult.Validated) : null;
            var extraInterpreterVerificationResult = requestExtraInterpreter != null ? (requestExtraInterpreter.InterpreterCompetenceVerificationResultOnStart ?? requestExtraInterpreter.InterpreterCompetenceVerificationResultOnAssign) : null;
            bool? isExtraInterpreterVerified = extraInterpreterVerificationResult.HasValue ? (bool?)(extraInterpreterVerificationResult == VerificationResult.Validated) : null;

            return new RequestGroupViewModel
            {
                AssignmentType = orderGroup.AssignmentType,
                CreatedAt = orderGroup.CreatedAt,
                OrderGroupId = requestGroup.OrderGroupId,
                RequestGroupId = requestGroup.RequestGroupId,
                OrderGroupNumber = orderGroup.OrderGroupNumber,
                AnsweredBy = requestGroup.AnsweringUser?.CompleteContactInformation,
                BrokerName = requestGroup.Ranking.Broker.Name,
                BrokerOrganizationNumber = requestGroup.Ranking.Broker.OrganizationNumber,
                DenyMessage = requestGroup.DenyMessage,
                CancelMessage = requestGroup.CancelMessage,
                ExpiresAt = requestGroup.ExpiresAt ?? null,
                LatestAnswerTimeForCustomer = requestGroup.LatestAnswerTimeForCustomer,
                HasExtraInterpreter = requestExtraInterpreter != null,
                Description = order.Description,
                LanguageName = orderGroup.LanguageName,
                LanguageHasAuthorizedInterpreter = orderGroup.LanguageHasAuthorizedInterpreter,
                InterpreterLocation = request.InterpreterLocation.HasValue ? (InterpreterLocation?)request.InterpreterLocation.Value : null,
                DisplayExpectedTravelCostInfo = GetDisplayExpectedTravelCostInfo(request.Order, request.InterpreterLocation ?? 0),
                Interpreter = request.Interpreter?.CompleteContactInformation,
                ExtraInterpreter = requestExtraInterpreter?.Interpreter?.CompleteContactInformation,

                IsInterpreterVerified = verificationResult.HasValue ? (bool?)(verificationResult == VerificationResult.Validated) : null,
                IsExtraInterpreterVerified = extraInterpreterVerificationResult.HasValue ? (bool?)(extraInterpreterVerificationResult == VerificationResult.Validated) : null,
                InterpreterVerificationMessage = verificationResult.HasValue ? verificationResult.Value.GetDescription() : null,
                ExtraInterpreterVerificationMessage = extraInterpreterVerificationResult.HasValue ? extraInterpreterVerificationResult.Value.GetDescription() : null,
                InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)request.CompetenceLevel,
                ExtraInterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)requestExtraInterpreter?.CompetenceLevel,
                RegionName = orderGroup.Region.Name,
                SpecificCompetenceLevelRequired = orderGroup.SpecificCompetenceLevelRequired,
                Status = requestGroup.Status,
                ExtraInterpreterStatus = requestExtraInterpreter?.Status,
                OrderStatus = orderGroup.Status,
                CustomerInformationModel = new CustomerInformationModel
                {
                    Name = orderGroup.CustomerOrganisation.Name,
                    CreatedBy = orderGroup.CreatedByUser.FullName,
                    OrganisationNumber = orderGroup.CustomerOrganisation.OrganisationNumber,
                    UnitName = orderGroup.CustomerUnit?.Name,
                    DepartmentName = order.UnitName,
                    ReferenceNumber = order.CustomerReferenceNumber,
                    InvoiceReference = order.InvoiceReference,
                }
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
