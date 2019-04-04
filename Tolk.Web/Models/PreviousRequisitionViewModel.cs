using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class PreviousRequisitionViewModel : RequisitionViewModel
    {
        public new int RequisitionId { get; set; }

        [Display(Name = "Tidigare angiven tidsspillan")]
        public new string TimeWasteInfo
        {
            get => (TimeWasteTotalTime != null && TimeWasteTotalTime > 0) ? $"{TimeWasteTotalTime} min varav {TimeWasteIWHTime ?? 0} min obekväm tid" : "Ingen tidsspillan har angivits";
        }

        public new int? TimeWasteIWHTime { get; set; }

        public int? TimeWasteNormalTime { get; set; }

        public new int? TimeWasteTotalTime { get => (TimeWasteNormalTime ?? 0) + (TimeWasteIWHTime ?? 0); }

        [Display(Name = "Tidigare angiven starttid")]
        public new DateTimeOffset SessionStartedAt { get; set; }

        [Display(Name = "Tidigare angiven sluttid")]
        public new DateTimeOffset SessionEndedAt { get; set; }

        [Display(Name = "Tidigare angivet utlägg")]
        [DataType(DataType.Currency)]
        public new decimal? Outlay { get; set; }

        [Display(Name = "Tidigare angiven bilersättning")]
        public new int? CarCompensation { get; set; }

        [Display(Name = "Tidigare angivet traktamente")]
        [DataType(DataType.MultilineText)]
        public new string PerDiem { get; set; }

        [Display(Name = "Tidigare angiven skattsedel")]
        public new TaxCard? InterpreterTaxCard { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Tidigare angiven specifikation")]
        public new string Message { get; set; }

        public new PriceInformationModel ResultPriceInformationModel { get; set; }

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
                PerDiem = requisition.PerDiem,
                CarCompensation = requisition.CarCompensation,
                InterpreterTaxCard = requisition.InterpretersTaxCard,
                Message = requisition.Message,
            };
        }
    }
}
