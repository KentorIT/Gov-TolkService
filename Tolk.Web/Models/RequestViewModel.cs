using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class RequestViewModel
    {
        public int RequestId { get; set; }

        [Display(Name = "Status på förfrågan")]
        public RequestStatus Status { get; set; }

        public int? ReplacingOrderRequestId { get; set; }

        public int? ReplacedByOrderRequestId { get; set; }

        public int? RequestGroupId { get; set; }

        public string ViewedByUser { get; set; } = string.Empty;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<FileModel> Files { get; set; }

        public AttachmentListModel AttachmentListModel { get; set; }

        public RequestStatus? RequestGroupStatus { get; set; }

        public string GroupStatusCssClassColor => RequestGroupStatus.HasValue ? CssClassHelper.GetColorClassNameForRequestStatus(RequestGroupStatus.Value) : string.Empty;

        public long? CombinedMaxSizeAttachments { get; set; }

        [Display(Name = "Förfrågan besvarad av")]
        [DataType(DataType.MultilineText)]
        public string AnsweredBy { get; set; }

        public string AnswerProcessedBy { get; set; }

        public string AnswerProcessedAt { get; set; }

        [Display(Name = "Förmedling")]
        public string BrokerName { get; set; }

        [Display(Name = "Förmedlings organisationsnummer")]
        public string BrokerOrganizationNumber { get; set; }

        [Display(Name = "Orsak till avböjande")]
        [DataType(DataType.MultilineText)]
        public string DenyMessage { get; set; }

        [Display(Name = "Orsak till avbokning")]
        [DataType(DataType.MultilineText)]
        public string CancelMessage { get; set; }

        public string Info48HCancelledByCustomer { get; set; }

        [Display(Name = "Tolkens kompetensnivå")]
        public CompetenceAndSpecialistLevel? InterpreterCompetenceLevel { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<RequestRequirementAnswerModel> RequirementAnswers { get; set; }

        public int? RequisitionId { get; set; }

        [Display(Name = "Bedömd resekostnad")]
        [DataType(DataType.Currency)]
        public decimal ExpectedTravelCosts => RequestCalculatedPriceInformationModel.ExpectedTravelCosts;

        [Display(Name = "Kommentar till bedömd resekostnad")]
        [DataType(DataType.MultilineText)]
        public string ExpectedTravelCostInfo { get; set; }

        [Display(Name = "Inställelsesätt")]
        public InterpreterLocation? InterpreterLocation { get; set; }

        [Display(Name = "Inkommen")]
        public DateTimeOffset? CreatedAt { get; set; }

        [Display(Name = "Svara senast")]
        public DateTimeOffset? ExpiresAt { get; set; }

        [Display(Name = "Språk och dialekt")]
        [DataType(DataType.MultilineText)]
        public string LanguageAndDialect { get; set; }

        [Display(Name = "Region")]
        public string RegionName { get; set; }

        [Display(Name = "Datum och tid")]
        public TimeRange TimeRange { get; set; }

        //THINGS IN NEED OF VALIDATION!!!!!!!
        public bool RequestIsAnswered => Status != RequestStatus.InterpreterReplaced && Status != RequestStatus.Created && Status != RequestStatus.Received && RequestId > 0;
        public bool RequestIsDeclinedByBroker => Status == RequestStatus.DeclinedByBroker || Status == RequestStatus.DeniedByTimeLimit;
        public bool IsReplacedRequest => Status == RequestStatus.Received && ReplacingOrderRequestId > 0;

        public bool IsCancelled { get; set; }

        public bool AllowInterpreterChange { get; set; } = false;

        public bool AllowRequisitionRegistration { get; set; } = false;

        public bool AllowConfirmNoRequisition { get; set; } = false;

        public bool AllowCancellation { get; set; } = true;

        public bool AllowConfirmCancellation { get; set; } = false;

        public bool AllowConfirmationDenial { get; set; } = false;

        public bool AllowConfirmNoAnswer { get; set; } = false;

        public bool AllowProcessing { get; set; } = true;

        public bool DisplayExpectedTravelCostInfo { get; set; }

        public EventLogModel EventLog { get; set; }

        public string ColorClassName => CssClassHelper.GetColorClassNameForRequestStatus(Status);

        [DataType(DataType.MultilineText)]
        [Display(Name = "Bokningsändringar")]
        public string DisplayOrderChangeText { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<int> ConfirmedOrderChangeLogEntries { get; set; } = new List<int>();

        [Display(Name = "Vill du ange en sista tid för att besvara tillsättning för myndighet", Description = "Ange om du vill sätta en tid för när myndigheten senast ska besvara tillsättningen. Om du anger en tid och myndigheten inte svarar inom angiven tid avslutas förfrågan.")]
        [ClientRequired]
        public RadioButtonGroup SetLatestAnswerTimeForCustomer { get; set; }

        [Display(Name = "Sista tid att besvara tillsättning", Description = "Här har förmedlingen möjlighet att ange en tid för när myndigheten senast ska besvara tillsättningen. Om myndigheten inte svarar inom angiven tid avslutas förfrågan.")]
        [ClientRequired]
        public DateTimeOffset? LatestAnswerTimeForCustomer { get; set; }

        #region view stuff

        [Display(Name = "Tillsatt tolk")]
        [DataType(DataType.MultilineText)]
        public string Interpreter { get; set; }
        public string InterpreterEmail { get; set; }
        public string InterpreterPhoneNumber { get; set; }
        public string InterpreterOfficialInterpreterId { get; set; }

        public bool? IsInterpreterVerified { get; set; }

        public string InterpreterVerificationMessage { get; set; }

        public PriceInformationModel OrderCalculatedPriceInformationModel { get; set; }

        public PriceInformationModel RequestCalculatedPriceInformationModel { get; set; }

        public int? ComplaintId { get; set; }

        public int? OldInterpreterId { get; set; }

        public int? OtherInterpreterId { get; set; }

        #endregion

        internal static RequestViewModel GetModelFromRequest(Request request, AllowExceedingTravelCost? allowExceedingTravelCost)
        {
            var verificationResult = (request.InterpreterCompetenceVerificationResultOnStart ?? request.InterpreterCompetenceVerificationResultOnAssign);

            return new RequestViewModel
            {
                Status = request.Status,
                AnsweredBy = request.AnsweringUser?.CompleteContactInformation,
                BrokerName = request.Ranking?.Broker?.Name,
                BrokerOrganizationNumber = request.Ranking?.Broker?.OrganizationNumber,
                DenyMessage = request.DenyMessage,
                CancelMessage = request.CancelMessage,
                RequestGroupId = request.RequestGroupId,
                RequestGroupStatus = request.RequestGroup?.Status,
                RequestId = request.RequestId,
                CreatedAt = request.CreatedAt,
                ExpiresAt = request.ExpiresAt,
                Interpreter = request.Interpreter?.CompleteContactInformation,
                IsInterpreterVerified = verificationResult.HasValue ? (bool?)(verificationResult == VerificationResult.Validated) : null,
                InterpreterVerificationMessage = verificationResult.HasValue ? verificationResult.Value.GetDescription() : null,
                InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)request.CompetenceLevel,
                InterpreterLocation = request.InterpreterLocation.HasValue ? (InterpreterLocation?)request.InterpreterLocation.Value : null,
                DisplayExpectedTravelCostInfo = GetDisplayExpectedTravelCostInfo(allowExceedingTravelCost, request.InterpreterLocation ?? 0),
                LatestAnswerTimeForCustomer = request.LatestAnswerTimeForCustomer,
                ExpectedTravelCostInfo = request.ExpectedTravelCostInfo,
            };
        }

        private static bool GetDisplayExpectedTravelCostInfo(AllowExceedingTravelCost? allowExceedingTravelCost, int locationAnswer)
        {
            if (allowExceedingTravelCost.HasValue && (allowExceedingTravelCost.Value == AllowExceedingTravelCost.YesShouldBeApproved || allowExceedingTravelCost.Value == AllowExceedingTravelCost.YesShouldNotBeApproved))
            {
                return locationAnswer == (int)BusinessLogic.Enums.InterpreterLocation.OnSite || locationAnswer == (int)BusinessLogic.Enums.InterpreterLocation.OffSiteDesignatedLocation;
            }
            return false;
        }
    }
}
