using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;
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

        [Display(Name = "Person med rätt att granska rekvisition")]
        [DataType(DataType.MultilineText)]
        public string ContactPerson { get; set; }

        [Display(Name = "Myndighetens kommentar")]
        [DataType(DataType.MultilineText)]
        [Required]
        [Placeholder("Skriv kommentar.")]
        public string CustomerComment { get; set; }

        public AttachmentListModel AttachmentListModel { get; set; }

        public bool AllowCreateNewRequisition => UserCanCreate && CanReplaceRequisition;

        public bool CanReplaceRequisition { get; set; }

        public bool CanConfirmNoReview { get; set; }

        public bool CanProcess { get; set; }

        public bool UserCanAccept { get; set; }

        public bool UserCanCreate { get; set; }

        public bool AllowProcessing => UserCanAccept && CanProcess;

        public bool AllowConfirmNoReview => UserCanAccept && CanConfirmNoReview;

        [Display(Name = "Total summa")]
        [DataType(DataType.Currency)]
        public decimal TotalPrice => ResultPriceInformationModel?.TotalPriceToDisplay ?? 0; 

        public EventLogModel EventLog { get; set; }

        public string ColorClassName => CssClassHelper.GetColorClassNameForRequisitionStatus(Status); 

        public RequisitionViewModel PreviousRequisitionView { get; set; }

        public override decimal ExpectedTravelCosts
        {
            get => ResultPriceInformationModel.ExpectedTravelCosts;
            set => base.ExpectedTravelCosts = value;
        }

        public IEnumerable<int> RelatedRequisitions { get; set; }

        #region methods

        internal static RequisitionViewModel GetViewModelFromRequisition(Requisition requisition)
        {
            if (requisition == null)
            {
                return null;
            }
            return new RequisitionViewModel
            {
                RequisitionId = requisition.RequisitionId,
                RequestId = requisition.RequestId,
                ReplacingRequisitionId = requisition.ReplacedByRequisitionId,
                BrokerName = requisition.Request.Ranking.Broker.Name,
                BrokerOrganizationnumber = requisition.Request.Ranking.Broker.OrganizationNumber,
                CustomerOrganizationName = requisition.Request.Order.CustomerOrganisation.Name,
                CustomerReferenceNumber = requisition.Request.Order.CustomerReferenceNumber,
                ExpectedEndedAt = requisition.Request.Order.EndAt,
                ExpectedStartedAt = requisition.Request.Order.StartAt,
                SessionEndedAt = requisition.SessionEndedAt,
                SessionStartedAt = requisition.SessionStartedAt,
                PerDiem = requisition.PerDiem,
                CarCompensation = requisition.CarCompensation,
                TimeWasteTotalTime = requisition.TimeWasteTotalTime,
                TimeWasteIWHTime = requisition.TimeWasteIWHTime,
                InterpreterTaxCard = requisition.InterpretersTaxCard,
                RequisitionCreatedBy = requisition.CreatedByUser.FullName,
                CreatedAt = requisition.CreatedAt,
                Message = requisition.Message,
                Status = requisition.Status,
                CustomerComment = requisition.CustomerComment,
                ContactPerson = requisition.Request.Order.ContactPersonUser?.CompleteContactInformation,
                MealBreakIncluded = requisition.Request.Order.MealBreakIncluded,
            };
        }

        internal static RequisitionViewModel GetPreviousRequisitionView(Request request)
        {
            if (request.Requisitions == null || request.Requisitions.Count < 2)
            {
                return null;
            }
            var requisition = request.Requisitions
                .Where(r => r.Status == RequisitionStatus.Commented || r.Status == RequisitionStatus.DeniedByCustomer)
                .OrderByDescending(r => r.CreatedAt)
                .First();
            var model = RequisitionViewModel.GetViewModelFromRequisition(requisition);
            model.RequestOrReplacingOrderPricesAreUsed = requisition.RequestOrReplacingOrderPeriodUsed;
            return model;
        }

        #endregion
    }
}
