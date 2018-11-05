using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class AssignmentModel
    {
        public int RequestId { get; set; }

        public int OrderId { get; set; }

        public int BrokerId { get; set; }

        [Display(Name = "AvropsID")]
        public string OrderNumber { get; set; }

        [Display(Name = "Språk")]
        public string LanguageName { get; set; }

        [Display(Name = "Förväntad resekostnad (exkl. moms)")]
        public decimal ExpectedTravelCosts { get; set; }

        [Display(Name = "Inställelsesätt")]
        public InterpreterLocation InterpreterLocation { get; set; }

        [Display(Name = "Kontaktinformation för distanstolkning")]
        public string OffSiteContactInformation { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Adress")]
        public string Address { get; set; }

        [Display(Name = "Myndighet")]
        public string CustomerName { get; set; }

        [Display(Name = "Börjar")]
        public DateTimeOffset StartDateTime { get; set; }

        [Display(Name = "Tar slut")]
        public DateTimeOffset EndDateTime { get; set; }

        [Display(Name = "Förmedlat av")]
        public string BrokerName { get; set; }

        public int? RequisitionId { get; set; }

        public string ReplacedByOrderNumber { get; set; }

        public OrderStatus? ReplacedByOrderStatus { get; set; }

        public string ReplacingOrderNumber { get; set; }

        public bool AllowRequisitionRegistration { get; set; } = false;

        public AttachmentListModel OrderAttachmentListModel { get; set; }

        public AttachmentListModel RequestAttachmentListModel { get; set; }

        #region methods

        public static AssignmentModel GetModelFromRequest(Request request, DateTimeOffset timeNow)
        {
            int? requisitionId = request.Requisitions.SingleOrDefault(r => r.Status == RequisitionStatus.Created || r.Status == RequisitionStatus.Approved)?.RequisitionId;
            var location = request.Order.InterpreterLocations.Single(l => (int)l.InterpreterLocation == request.InterpreterLocation.Value);
            return new AssignmentModel
            {
                OrderId = request.OrderId,
                OrderNumber = request.Order.OrderNumber.ToString(),
                ExpectedTravelCosts = request.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0,
                InterpreterLocation = (InterpreterLocation)request.InterpreterLocation.Value,
                Address = $"{location.Street}\n{location.ZipCode} {location.City}",
                OffSiteContactInformation = location.OffSiteContactInformation,
                CustomerName = request.Order.CustomerOrganisation.Name,
                StartDateTime = request.Order.StartAt,
                EndDateTime = request.Order.EndAt,
                BrokerName = request.Ranking.Broker.Name,
                LanguageName = request.Order.OtherLanguage ?? request.Order.Language?.Name ?? "-",
                RequestId = request.RequestId,
                RequisitionId = request.Requisitions.SingleOrDefault(r => r.Status == RequisitionStatus.Created || r.Status == RequisitionStatus.Approved)?.RequisitionId,
                AllowRequisitionRegistration = (request.Order.StartAt < timeNow && !requisitionId.HasValue && request.Status == RequestStatus.Approved),
                ReplacedByOrderNumber = request.Order.ReplacedByOrder?.OrderNumber,
                ReplacedByOrderStatus = request.Order.ReplacedByOrder?.Status,
                ReplacingOrderNumber = request.Order.ReplacingOrder?.OrderNumber,
                RequestAttachmentListModel = new AttachmentListModel
                {
                    AllowDelete = false,
                    AllowDownload = true,
                    AllowUpload = false,
                    Title = "Bifogade filer från förmedling",
                    Files = request.Attachments.Select(a => new FileModel
                    {
                        Id = a.Attachment.AttachmentId,
                        FileName = a.Attachment.FileName,
                        Size = a.Attachment.Blob.Length
                    }).ToList()
                },
                OrderAttachmentListModel = new AttachmentListModel
                {
                    AllowDelete = false,
                    AllowDownload = true,
                    AllowUpload = false,
                    Title = "Bifogade filer från myndighet",
                    Files = request.Order.Attachments.Select(a => new FileModel
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
