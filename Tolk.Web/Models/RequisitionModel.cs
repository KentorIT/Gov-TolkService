using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

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

        [Display(Name = "Myndighetens referensnummer")]
        public string CustomerReferenceNumber { get; set; }

        [Display(Name = "Tolk")]
        [DataType(DataType.MultilineText)]
        public string InterpreterName { get; set; }

        [Display(Name = "Tolkens skattsedel")]
        [ClientRequired]
        public TaxCard? InterpreterTaxCard { get; set; }

        [Display(Name = "Förmedling")]
        public string BrokerName { get; set; }

        [Display(Name = "Avropande myndighet")]
        public string CustomerOrganizationName { get; set; }

        [Display(Name = "Avropande myndighet")]
        [DataType(DataType.MultilineText)]
        public string CustomerCompactInfo
        { get => CustomerOrganizationName + (string.IsNullOrWhiteSpace(CustomerReferenceNumber) ? string.Empty : "\nReferensnummer: " + CustomerReferenceNumber); }

        [Display(Name = "Avrop skapat av")]
        [DataType(DataType.MultilineText)]
        public string OrderCreatedBy { get; set; }

        [Display(Name = "Rekvisition registrerad av")]
        [DataType(DataType.MultilineText)]
        public string RequisitionCreatedBy { get; set; }

        [Display(Name = "Förväntad resekostnad (exkl. moms)")]
        [DataType(DataType.Currency)]
        public decimal ExpectedTravelCosts { get; set; }

        [Display(Name = "Faktisk resekostnad (exkl. moms)")]
        [Range(0, 100000, ErrorMessage = "Ange ett värde mellan 0 och 100 000 kronor")]
        [DataType(DataType.Currency)]
        public decimal TravelCosts { get; set; }

        [Display(Name = "Förväntad startid")]
        public DateTimeOffset ExpectedStartedAt { get; set; }

        [Display(Name = "Förväntad sluttid")]
        public DateTimeOffset ExpectedEndedAt { get; set; }

        [Range(31, 600, ErrorMessage = "Ange ett värde mellan 31 och 600 minuter")]
        [Display(Name = "Tid för eventuell tidsspillan i minuter (som inträffat utanför förväntad start- och sluttid)", Description = "Totalt antal minuter för restid, väntetider mm som överstiger 30 minuter")]
        public int? TimeWasteNormalTime { get; set; }

        [Range(0, 600, ErrorMessage = "Ange ett värde mellan 0 och 600 minuter")]
        [Display(Name = "Andel av tidsspillan ovan som inträffat under obekväm arbetstid i minuter", Description = "Avser tid i minuter av total tidsspillan som inträffar utanför vardagar 07:00-18:00")]
        public int? TimeWasteIWHTime { get; set; }

        [Display(Name = "Angiven tidsspillan")]
        public string TimeWasteInfo
        {
            get => (TimeWasteNormalTime != null && TimeWasteNormalTime > 0) ? $"Totalt angiven tidsspillan {TimeWasteNormalTime} minuter varav {TimeWasteIWHTime ?? 0} minuter under obekväm arbetstid" : "Ingen tidsspillan har angivits";
        }

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
