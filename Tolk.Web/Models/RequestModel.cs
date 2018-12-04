using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
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

        [Display(Name = "Orsak till avslag")]
        [DataType(DataType.MultilineText)]
        [Required]
        public string DenyMessage { get; set; }


        [Display(Name = "Orsak till avbokning")]
        [DataType(DataType.MultilineText)]
        [Required]
        public string CancelMessage { get; set; }

        public string Info48HCancelledByCustomer { get; set; }

        [Required]
        [Display(Name = "Tolkens kompetensnivå")]
        public CompetenceAndSpecialistLevel? InterpreterCompetenceLevel { get; set; }

        [Required]
        [Display(Name = "Tolk")]
        public int? InterpreterId { get; set; }

        [Required]
        [EmailAddress]
        [RegularExpression(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*@((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$", ErrorMessage = "Felaktig e-postadress")]
        [Display(Name = "Tolkens e-postadress")]
        public string NewInterpreterEmail { get; set; }

        public List<RequestRequirementAnswerModel> RequirementAnswers { get; set; }

        public int? RequisitionId { get; set; }

        [Display(Name = "Förväntad resekostnad (exkl. restid och moms)")]
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

        [Display(Name = "Svar senast")]
        public DateTimeOffset? ExpiresAt { get; set; }

        public bool AllowInterpreterChange { get; set; } = false;

        public bool AllowCancellation { get; set; } = false;

        public EventLogModel EventLog { get; set; }

        #region view stuff

        [Display(Name = "Tillsatt tolk")]
        [DataType(DataType.MultilineText)]
        public string Interpreter{ get; set; }

        public PriceInformationModel OrderCalculatedPriceInformationModel { get; set; }

        public PriceInformationModel RequestCalculatedPriceInformationModel { get; set; }

        public int? ComplaintId { get; set; }

        [Display(Name = "Reklamationens status")]
        public ComplaintStatus? ComplaintStatus { get; set; }

        [Display(Name = "Typ av reklamation")]
        public ComplaintType? ComplaintType { get; set; }
        [Display(Name = "Reklamationens beskriving")]
        public string ComplaintMessage { get; set; }

        #endregion

        #region methods

        public static RequestModel GetModelFromRequest(Request request)
        {
            var complaint = request.Complaints?.FirstOrDefault();
            var replacingOrderRequest = request.Order.ReplacingOrder?.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault(r => r.Ranking.BrokerId == request.Ranking.BrokerId);
            var replacedByOrderRequest = request.Order.ReplacedByOrder?.Requests.OrderByDescending(r => r.RequestId).FirstOrDefault(r => r.Ranking.BrokerId == request.Ranking.BrokerId);
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
                Interpreter = request.Interpreter?.User?.CompleteContactInformation,
                InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)request.CompetenceLevel,
                ExpectedTravelCosts = request.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0,
                RequisitionId = request.Requisitions?.FirstOrDefault(req => req.Status == RequisitionStatus.Created || req.Status == RequisitionStatus.Approved)?.RequisitionId,
                RequirementAnswers = request.Order.Requirements.Select(r => new RequestRequirementAnswerModel
                {
                    OrderRequirementId = r.OrderRequirementId,
                    IsRequired = r.IsRequired,
                    Description = r.Description,
                    RequirementType =  r.RequirementType,
                    Answer = request.RequirementAnswers != null ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId)?.Answer : string.Empty,
                    CanMeetRequirement = request.RequirementAnswers != null ? request.RequirementAnswers.Any() ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId).CanSatisfyRequirement : false : false,
                }).ToList(),
                InterpreterLocation = request.InterpreterLocation.HasValue ? (InterpreterLocation?)request.InterpreterLocation.Value : null,
                OrderModel = OrderModel.GetModelFromOrder(request.Order),
                ComplaintId = complaint?.ComplaintId,
                ComplaintMessage = complaint?.ComplaintMessage,
                ComplaintStatus = complaint?.Status,
                ComplaintType = complaint?.ComplaintType,
                ReplacingOrderRequestId = replacingOrderRequest?.RequestId,
                ReplacedByOrderRequestStatus = replacedByOrderRequest?.Status,
                ReplacedByOrderRequestId = replacedByOrderRequest?.RequestId,
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
