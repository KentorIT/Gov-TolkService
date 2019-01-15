using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class PreviousRequisitionViewModel
    {
        public int RequisitionId { get; set; }

        [Display(Name = "Tidigare angiven tidsspillan")]
        public string TimeWasteInfo
        {
            get => (TimeWasteTotalTime != null && TimeWasteTotalTime > 0) ? $"{TimeWasteTotalTime} min varav {TimeWasteIWHTime ?? 0} min obekväm tid" : "Ingen tidsspillan har angivits";
        }

        public int? TimeWasteIWHTime { get; set; }

        public int? TimeWasteNormalTime { get; set; }

        public int? TimeWasteTotalTime { get => (TimeWasteNormalTime ?? 0) + (TimeWasteIWHTime ?? 0); }

        [Display(Name = "Tidigare angiven starttid")]
        public DateTimeOffset SessionStartedAt { get; set; }

        [Display(Name = "Tidigare angiven sluttid")]
        public DateTimeOffset SessionEndedAt { get; set; }

        [Display(Name = "Tidigare angivet utlägg")]
        [DataType(DataType.Currency)]
        public decimal? Outlay { get; set; }

        [Display(Name = "Tidigare angiven bilersättning")]
        [DataType(DataType.Currency)]
        public decimal? CarCompensation { get; set; }

        [Display(Name = "Tidigare angivet traktamente")]
        [DataType(DataType.Currency)]
        public decimal? PerDiem { get; set; }

        public PriceInformationModel ResultPriceInformationModel { get; set; }

        public static PreviousRequisitionViewModel GetViewModelFromPreviousRequisition(Requisition requisition)
        {
            if (requisition == null)
            {
                return null;
            }
            return new PreviousRequisitionViewModel
            {
                RequisitionId = requisition.RequisitionId,
                SessionEndedAt = requisition.SessionEndedAt,
                SessionStartedAt = requisition.SessionStartedAt,
                TimeWasteIWHTime = requisition.TimeWasteIWHTime,
                TimeWasteNormalTime = requisition.TimeWasteNormalTime,
                Outlay = requisition.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.Outlay)?.Price,
                PerDiem = requisition.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.PerDiem)?.Price,
                CarCompensation = requisition.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.CarCompensation)?.Price
            };
        }
    }
}
