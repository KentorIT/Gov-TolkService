using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class RequisitionModel
    {
        public int RequestId { get; set; }

        [Display(Name = "AvropsID")]
        public string OrderNumber { get; set; }

        [Display(Name = "Språk")]
        public string LanguageName { get; set; }

        [Display(Name = "Region")]
        public string RegionName { get; set; }

        [Display(Name = "Kundens referensnummer")]
        public string CustomerReferenceNumber { get; set; }

        [Display(Name = "Tolk")]
        [DataType(DataType.MultilineText)]
        public string InterpreterName { get; set; }

        [Display(Name = "Förmedling")]
        public string BrokerName { get; set; }

        [Display(Name = "Kund")]
        public string CustomerOrganizationName { get; set; }

        [Display(Name = "Avropare hos kund")]
        [DataType(DataType.MultilineText)]
        public string OrderCreatedBy { get; set; }

        [Display(Name = "Rekvisition registrerad av")]
        [DataType(DataType.MultilineText)]
        public string RequisitionCreatedBy { get; set; }

        [Display(Name = "Förväntad resekostnad (exkl. moms)")]
        [DataType(DataType.Currency)]
        public decimal ExpectedTravelCosts { get; set; }

        [Display(Name = "Faktisk resekostnad (exkl. moms)")]
        [DataType(DataType.Currency)]
        public decimal TravelCosts { get; set; }

        [Display(Name = "Förväntad startid")]
        public DateTimeOffset ExpectedStartedAt { get; set; }

        [Display(Name = "Förväntad sluttid")]
        public DateTimeOffset ExpectedEndedAt { get; set; }

        [Display(Name = "Ev. tillkommande tid för tidsspillan totalt antal minuter", Description = "Restid, väntetider mm som infaller vardagar 07:00-18:00")]
        public int? TimeWasteNormalTime { get; set; }

        [Display(Name = "Ev. andel av tidsspillan ovan som inträffat under obekväm arbetstid i minuter", Description = "Avser tid av todal tidsspillan som inträffar utanför vardagar 07:00-18:00")]
        public int? TimeWasteIWHTime { get; set; }

        [Display(Name = "Faktisk startid")]
        public DateTimeOffset SessionStartedAt { get; set; }

        [Display(Name = "Faktisk sluttid")]
        public DateTimeOffset SessionEndedAt { get; set; }


        [DataType(DataType.MultilineText)]
        [Required]
        [Display(Name = "Specifikation", Description = "Var tydlig med var alla tider och kostnader kommer ifrån.")]
        public string Message { get; set; }

        public int? ReplacingRequisitionId { get; set; }

        public Requisition PreviousRequisition { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Faktureringsinformation")]
        public string InvoiceInformation { get; set; }

        public List<FileModel> Files { get; set; }

        public Guid? FileGroupKey { get; set; }

        public long? CombinedMaxSizeAttachments { get; set; }

        public PriceInformationModel ResultPriceInformationModel { get; set; }

        public PriceInformationModel RequestPriceInformationModel { get; set; }

        #region methods

        public static RequisitionModel GetModelFromRequest(Request request)
        {
            return new RequisitionModel
            {
                RequestId = request.RequestId,
                BrokerName = request.Ranking.Broker.Name,
                CustomerOrganizationName = request.Order.CustomerOrganisation.Name,
                CustomerReferenceNumber = request.Order.CustomerReferenceNumber,
                OrderCreatedBy = request.Order.CreatedByUser.CompleteContactInformation,
                ExpectedEndedAt = request.Order.EndAt,
                ExpectedStartedAt = request.Order.StartAt,
                SessionEndedAt = request.Order.EndAt,
                SessionStartedAt = request.Order.StartAt,
                ExpectedTravelCosts = request.ExpectedTravelCosts ?? 0,
                InterpreterName = request.Interpreter.User.CompleteContactInformation,
                LanguageName = request.Order.OtherLanguage ?? request.Order.Language?.Name ?? "-",
                OrderNumber = request.Order.OrderNumber.ToString(),
                RegionName = request.Ranking.Region.Name,
                PreviousRequisition = request.Requisitions.SingleOrDefault(r => r.Status == RequisitionStatus.DeniedByCustomer && !r.ReplacedByRequisitionId.HasValue),
            };
        }

        #endregion
    }
}
