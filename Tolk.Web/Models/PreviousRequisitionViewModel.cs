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
            get => (TimeWasteNormalTime != null && TimeWasteNormalTime > 0) ? $"Totalt angiven tidsspillan {TimeWasteNormalTime} minuter varav {TimeWasteIWHTime ?? 0} minuter under obekväm arbetstid" : "Ingen tidsspillan har angivits";
        }

        public int? TimeWasteIWHTime { get; set; }

        public int? TimeWasteNormalTime { get; set; }

        [Display(Name = "Tidigare angiven starttid")]
        public DateTimeOffset SessionStartedAt { get; set; }

        [Display(Name = "Tidigare angiven sluttid")]
        public DateTimeOffset SessionEndedAt { get; set; }

        [Display(Name = "Tidigare angiven resekostnad (exkl. moms)")]
        public decimal? TravelCosts { get; set; }

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
                TravelCosts = requisition.Request.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price
            };
        }
    }
}
