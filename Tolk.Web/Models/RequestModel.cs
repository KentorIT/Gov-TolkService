using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class RequestModel
    {
        public int RequestId { get; set; }

        [Display(Name = "Status på förfrågan")]
        public RequestStatus Status { get; set; }

        public int BrokerId { get; set; }

        public int? ReplacingOrderRequestId { get; set; }

        public RequestStatus? ReplacedByOrderRequestStatus { get; set; }

        public int? ReplacedByOrderRequestId { get; set; }

        public OrderModel OrderModel { get; set; }

        public int? OrderId
        {
            get
            {
                return OrderModel?.OrderId;
            }
        }

        public string ViewedByUser { get; set; } = string.Empty;

        public List<FileModel> Files { get; set; }

        public AttachmentListModel AttachmentListModel { get; set; }

        public Guid? FileGroupKey { get; set; }

        public long? CombinedMaxSizeAttachments { get; set; }

        [Display(Name = "Förfrågan besvarad av")]
        [DataType(DataType.MultilineText)]
        public string AnsweredBy { get; set; }

        [Display(Name = "Förmedling")]
        public string BrokerName { get; set; }

        [Display(Name = "Förmedlings organisationsnummer")]
        public string BrokerOrganizationNumber { get; set; }

        [Display(Name = "Orsak till avböjande")]
        [DataType(DataType.MultilineText)]
        [Required]
        [StringLength(1000)]
        public string DenyMessage { get; set; }


        [Display(Name = "Orsak till avbokning")]
        [DataType(DataType.MultilineText)]
        [Required]
        [StringLength(1000)]
        public string CancelMessage { get; set; }

        public string Info48HCancelledByCustomer { get; set; }

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

        [Display(Name = "Tolk-ID")]
        public string NewInterpreterOfficialInterpreterId { get; set; }

        [Required]
        [Display(Name = "Tolkens telefonnummer")]
        [StringLength(255)]
        public string NewInterpreterPhoneNumber { get; set; }

        public List<RequestRequirementAnswerModel> RequirementAnswers { get; set; }

        public List<RequestRequirementAnswerModel> RequiredRequirementAnswers { get; set; }

        public List<RequestRequirementAnswerModel> DesiredRequirementAnswers { get; set; }

        public int? RequisitionId { get; set; }

        [Range(0, 999999, ErrorMessage = "Kontrollera värdet för resekostnad")]
        [ClientRequired(ErrorMessage = "Ange resekostnad (inga bokstäver tillåtna, ange 0 om det inte finns någon kostnad)")]
        [Display(Name = "Kunden accepterar överskridande av gränsvärde för resor för detta uppdrag, dvs över 2 tim restid eller 100 km reslängd. Uppgift om resekostnad måste anges. Finns inga reskostnader anges 0.")]
        [DataType(DataType.Currency)]
        public decimal? ExpectedTravelCosts { get; set; }

        [ClientRequired]
        [Display(Name = "Inställelsesätt")]
        public InterpreterLocation? InterpreterLocation { get; set; }

        [Display(Name = "Inställelsesätt enl. svar")]
        public InterpreterLocation? InterpreterLocationAnswer
        {
            get; set;
        }

        [Display(Name = "Inkommen")]
        public DateTimeOffset? CreatedAt { get; set; }

        [Display(Name = "Svara senast")]
        public DateTimeOffset? ExpiresAt { get; set; }

        public bool AllowInterpreterChange { get; set; } = false;

        public bool AllowRequisitionRegistration { get; set; } = false;

        public bool AllowCancellation { get; set; } = true;

        public bool AllowConfirmCancellation { get; set; } = false;

        public bool AllowConfirmationDenial { get; set; } = false;

        public EventLogModel EventLog { get; set; }

        public string ColorClassName { get => CssClassHelper.GetColorClassNameForRequestStatus(Status); }

        #region view stuff

        [Display(Name = "Tillsatt tolk")]
        [DataType(DataType.MultilineText)]
        public string Interpreter { get; set; }

        public PriceInformationModel OrderCalculatedPriceInformationModel { get; set; }

        public PriceInformationModel RequestCalculatedPriceInformationModel { get; set; }

        public int? ComplaintId { get; set; }

        public int? OldInterpreterId { get; set; }

        #endregion

        #region methods

        public static RequestModel GetModelFromRequest(Request request, bool requestSummaryOnly = false)
        {
            var complaint = request.Complaints?.FirstOrDefault();
            var replacingOrderRequest = requestSummaryOnly ? null : request.Order.ReplacingOrder?.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault(r => r.Ranking.BrokerId == request.Ranking.BrokerId);
            var replacedByOrderRequest = requestSummaryOnly ? null : request.Order.ReplacedByOrder?.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault(r => r.Ranking.BrokerId == request.Ranking.BrokerId);
            var attach = request.Attachments;
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
                InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)request.CompetenceLevel,
                ExpectedTravelCosts = request.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0,
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
                OrderModel = OrderModel.GetModelFromOrder(request.Order),
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
