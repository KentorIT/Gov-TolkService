using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class RequisitionViewModel : RequisitionModel
    {
        public int RequisitionId { get; set; }

        [Display(Name = "Rekvisition registrerad")]
        public DateTimeOffset CreatedAt { get; set; }

        [Display(Name = "Status")]
        public RequisitionStatus Status { get; set; }

        [Display(Name = "Annan kontaktperson")]
        [DataType(DataType.MultilineText)]
        public string ContactPerson { get; set; }

        [Display(Name = "Anledning till underkännande av rekvisition")]
        [DataType(DataType.MultilineText)]
        [Required]
        public string DenyMessage { get; set; }

        public AttachmentListModel AttachmentListModel { get; set; }

        public bool AllowCreation { get; set; }

        public bool AllowProcessing { get; set; }

        [Display(Name = "Total summa")]
        [DataType(DataType.Currency)]
        public decimal TotalPrice { get => ResultPriceInformationModel.TotalPriceToDisplay; }

        public EventLogModel EventLog { get; set; }

        public string ColorClassName { get => CssClassHelper.GetColorClassNameForRequisitionStatus(Status); }

        #region methods

        public static RequisitionViewModel GetViewModelFromRequisition(Requisition requisition)
        {
            if (requisition == null)
            {
                return null;
            }
            return new RequisitionViewModel
            {
                RequisitionId = requisition.RequisitionId,
                RequestId = requisition.RequestId,
                PreviousRequisition = PreviousRequisitionViewModel.GetViewModelFromPreviousRequisition(requisition.Request.Requisitions.SingleOrDefault(r => r.ReplacedByRequisitionId == requisition.RequisitionId)),
                ReplacingRequisitionId = requisition.ReplacedByRequisitionId,
                BrokerName = requisition.Request.Ranking.Broker.Name,
                BrokerOrganizationnumber = requisition.Request.Ranking.Broker.OrganizationNumber,
                CustomerOrganizationName = requisition.Request.Order.CustomerOrganisation.Name,
                CustomerReferenceNumber = requisition.Request.Order.CustomerReferenceNumber,
                ExpectedEndedAt = requisition.Request.Order.EndAt,
                ExpectedStartedAt = requisition.Request.Order.StartAt,
                SessionEndedAt = requisition.SessionEndedAt,
                SessionStartedAt = requisition.SessionStartedAt,
                ExpectedTravelCosts = requisition.Request.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0,
                Outlay = requisition.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.Outlay)?.Price ?? 0,
                PerDiem = requisition.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.PerDiem)?.Price ?? 0,
                CarCompensation = requisition.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.CarCompensation)?.Price ?? 0,
                TimeWasteTotalTime = requisition.TimeWasteTotalTime,
                TimeWasteIWHTime = requisition.TimeWasteIWHTime,
                InterpreterTaxCard = requisition.InterpretersTaxCard,
                RequisitionCreatedBy = requisition.CreatedByUser.FullName,
                CreatedAt = requisition.CreatedAt,
                Message = requisition.Message,
                Status = requisition.Status,
                DenyMessage = requisition.DenyMessage,
                ContactPerson = requisition.Request.Order.ContactPersonUser?.CompleteContactInformation,
                AttachmentListModel = new AttachmentListModel
                {
                    AllowDelete = false,
                    AllowDownload = true,
                    AllowUpload = false,
                    DisplayFiles = requisition.Attachments.Select(a => new FileModel
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
