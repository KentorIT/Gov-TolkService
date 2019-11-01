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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<FileModel> Files { get; set; }

        public AttachmentListModel AttachmentListModel { get; set; }

        public Guid? FileGroupKey { get; set; }

        public long? CombinedMaxSizeAttachments { get; set; }

        [Display(Name = "Orsak till avböjande")]
        [DataType(DataType.MultilineText)]
        [Required]
        [StringLength(1000)]
        [Placeholder("Beskriv anledning tydligt.")]
        public string DenyMessage { get; set; }

        [Required]
        [Display(Name = "Tolkens kompetensnivå")]
        public CompetenceAndSpecialistLevel? InterpreterCompetenceLevel { get; set; }

        [Required]
        [Display(Name = "Tolk", Description = "I de fall tillsatt tolk har skyddad identitet skall inte tolkens namn eller kontaktuppgifter finnas i bekräftelsen. Använd i dessa fall valet ”Tolk med skyddade personuppgifter”. Överlämna tolkens uppgifter på annat sätt i enlighet med era säkerhetsrutiner")]
        public int? InterpreterId { get; set; }

        [Required]
        [EmailAddress]
        [RegularExpression(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*@((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$", ErrorMessage = "Felaktig e-postadress")]
        [Display(Name = "Tolkens e-postadress")]
        [StringLength(255)]
        public string NewInterpreterEmail { get; set; }

        [Required]
        [Display(Name = "Tolkens förnamn")]
        [StringLength(255)]
        public string NewInterpreterFirstName { get; set; }

        [Required]
        [Display(Name = "Tolkens efternamn")]
        [StringLength(255)]
        public string NewInterpreterLastName { get; set; }

        [Display(Name = "Kammarkollegiets tolknummer")]
        public string NewInterpreterOfficialInterpreterId { get; set; }

        [Required]
        [Display(Name = "Tolkens telefonnummer")]
        [StringLength(255)]
        public string NewInterpreterPhoneNumber { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<RequestRequirementAnswerModel> RequiredRequirementAnswers { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<RequestRequirementAnswerModel> DesiredRequirementAnswers { get; set; }

        [Range(0, 999999, ErrorMessage = "Kontrollera värdet för resekostnad")]
        [RegularExpression(@"^[^.]*$", ErrorMessage = "Värdet får inte innehålla punkttecken, ersätt med kommatecken")] // validate.js regex allows dots, despite not explicitly allowing them
        [ClientRequired(ErrorMessage = "Ange resekostnad (endast siffror, ange 0 om det inte finns någon kostnad)")]
        [Display(Name = "Bedömd resekostnad")]
        [DataType(DataType.Currency)]
        [Placeholder("Ange i SEK")]
        public decimal? ExpectedTravelCosts { get; set; }

        [Display(Name = "Kommentar till bedömd resekostnad", Description = "Här kan du kommentera den bedömda resekostanden som angivits genom att skriva in t ex antal km för bilersättning, eventuella biljettkostnader, spilltid mm")]
        [Placeholder("Ange t ex bedömt antal km, biljettkostnad, spilltid mm")]
        [StringLength(1000)]
        [DataType(DataType.MultilineText)]
        public string ExpectedTravelCostInfo { get; set; }

        [ClientRequired]
        [Display(Name = "Inställelsesätt")]
        public InterpreterLocation? InterpreterLocation { get; set; }

        [Display(Name = "Svara senast")]
        public DateTimeOffset ExpiresAt { get; set; }

        public string ColorClassName { get => CssClassHelper.GetColorClassNameForRequestStatus(Status); }

        //NEW
        public bool AllowExceedingTravelCost { get; set; }
        public bool SpecificCompetenceLevelRequired { get; set; }
        public AssignmentType AssignmentType { get; set; }
        public string LanguageAndDialect { get; set; }
        public string RegionName { get; set; }
        public string CustomerName { get; set; }
        public string CreatedBy { get; set; }
        public string CustomerUnitName { get; set; }
        public string UnitName { get; set; }
        public string CustomerOrganisationNumber { get; set; }
        public string CustomerReferenceNumber { get; set; }
        public bool LanguageHasAuthorizedInterpreter { get; set; }
        public IEnumerable<CompetenceAndSpecialistLevel> RequestedCompetenceLevels { get; set; }

        [Prefix(PrefixPosition = PrefixAttribute.Position.Value, Text = "<span class=\"competence-ranking-num\">1. </span>")]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelFirst { get; set; }

        [NoDisplayName]
        [Prefix(PrefixPosition = PrefixAttribute.Position.Value, Text = "<span class=\"competence-ranking-num\">2. </span>")]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelSecond { get; set; }
        public string Description { get; set; }
        public PriceInformationModel OrderCalculatedPriceInformationModel { get; set; }
        public InterpreterLocationAddressModel RankedInterpreterLocationFirstAddressModel { get; set; }
        public InterpreterLocationAddressModel RankedInterpreterLocationSecondAddressModel { get; set; }
        public InterpreterLocationAddressModel RankedInterpreterLocationThirdAddressModel { get; set; }

        public IEnumerable<InterpreterLocation> RankedInterpreterLocations { get; set; }

        #region methods

        internal static RequestGroupProcessModel GetModelFromRequestGroup(RequestGroup requestGroup, Guid fileGroupKey, long combinedMaxSizeAttachments)
        {
            return new RequestGroupProcessModel
            {
                RequestGroupId = requestGroup.RequestGroupId,
                OrderGroupNumber = requestGroup.OrderGroup.OrderGroupNumber,
                FileGroupKey = fileGroupKey,
                CombinedMaxSizeAttachments = combinedMaxSizeAttachments,
            };
        }

        internal static RequestModel GetModelFromRequest(Request request, bool requestSummaryOnly = false)
        {
            var complaint = request.Complaints?.FirstOrDefault();
            var replacingOrderRequest = requestSummaryOnly ? null : request.Order.ReplacingOrder?.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault(r => r.Ranking.BrokerId == request.Ranking.BrokerId);
            var replacedByOrderRequest = requestSummaryOnly ? null : request.Order.ReplacedByOrder?.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault(r => r.Ranking.BrokerId == request.Ranking.BrokerId);
            var attach = request.Attachments;
            var verificationResult = (request.InterpreterCompetenceVerificationResultOnStart ?? request.InterpreterCompetenceVerificationResultOnAssign);
            bool? isInterpreterVerified = verificationResult.HasValue ? (bool?)(verificationResult == VerificationResult.Validated) : null;

            return new RequestModel
            {
                Status = request.Status,
                AnsweredBy = request.AnsweringUser?.CompleteContactInformation,
                BrokerName = request.Ranking?.Broker?.Name,
                BrokerOrganizationNumber = request.Ranking?.Broker?.OrganizationNumber,
                DenyMessage = request.DenyMessage,
                CancelMessage = request.CancelMessage,
                RequestId = request.RequestId,
                CreatedAt = request.CreatedAt,
                ExpiresAt = request.ExpiresAt,
                Interpreter = request.Interpreter?.CompleteContactInformation,
                IsInterpreterVerified = verificationResult.HasValue ? (bool?)(verificationResult == VerificationResult.Validated) : null,
                InterpreterVerificationMessage = verificationResult.HasValue ? verificationResult.Value.GetDescription() : null,
                InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)request.CompetenceLevel,
                ExpectedTravelCosts = request.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0,
                ExpectedTravelCostInfo = request.ExpectedTravelCostInfo,
                RequisitionId = request.Requisitions?.OrderByDescending(r => r.RequisitionId).FirstOrDefault()?.RequisitionId,
                RequirementAnswers = request.Order.Requirements.Select(r => new RequestRequirementAnswerModel
                {
                    OrderRequirementId = r.OrderRequirementId,
                    IsRequired = r.IsRequired,
                    Description = r.Description,
                    RequirementType = r.RequirementType,
                    Answer = request.RequirementAnswers != null ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId)?.Answer : string.Empty,
                    CanMeetRequirement = request.RequirementAnswers != null ? request.RequirementAnswers.Any() ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId).CanSatisfyRequirement : false : false,
                }).ToList(),
                RequiredRequirementAnswers = request.Order.Requirements.Where(r => r.IsRequired).Select(r => new RequestRequirementAnswerModel
                {
                    OrderRequirementId = r.OrderRequirementId,
                    IsRequired = true,
                    Description = r.Description,
                    RequirementType = r.RequirementType,
                    Answer = request.RequirementAnswers != null ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId)?.Answer : string.Empty,
                    CanMeetRequirement = request.RequirementAnswers != null ? request.RequirementAnswers.Any() ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId).CanSatisfyRequirement : false : false,
                }).ToList(),
                DesiredRequirementAnswers = request.Order.Requirements.Where(r => !r.IsRequired).Select(r => new RequestRequirementAnswerModel
                {
                    OrderRequirementId = r.OrderRequirementId,
                    IsRequired = false,
                    Description = r.Description,
                    RequirementType = r.RequirementType,
                    Answer = request.RequirementAnswers != null ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId)?.Answer : string.Empty,
                    CanMeetRequirement = request.RequirementAnswers != null ? request.RequirementAnswers.Any() ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId).CanSatisfyRequirement : false : false,
                }).ToList(),

                InterpreterLocation = request.InterpreterLocation.HasValue ? (InterpreterLocation?)request.InterpreterLocation.Value : null,
                OrderModel = OrderModel.GetModelFromOrder(request.Order, null, true),
                ComplaintId = complaint?.ComplaintId,
                ReplacingOrderRequestId = requestSummaryOnly ? null : replacingOrderRequest?.RequestId,
                ReplacedByOrderRequestStatus = requestSummaryOnly ? null : replacedByOrderRequest?.Status,
                ReplacedByOrderRequestId = requestSummaryOnly ? null : replacedByOrderRequest?.RequestId,
                AttachmentListModel = new AttachmentListModel
                {
                    AllowDelete = false,
                    AllowDownload = true,
                    AllowUpload = false,
                    Title = "Bifogade filer från förmedling",
                    DisplayFiles = request.Attachments.Select(a => new FileModel
                    {
                        Id = a.Attachment.AttachmentId,
                        FileName = a.Attachment.FileName,
                        Size = a.Attachment.Blob.Length
                    }).ToList()
                }
            };
        }

        #endregion
    }
}
