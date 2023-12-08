using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class ReportSearchModel : IModel
    {
        [Display(Name = "Välj datumintervall")]
        [NoAutoComplete]
        public DateRange ReportDate { get; set; }

        [Required]
        [Display(Name = "Rapport")]
        public ReportType ReportType { get; set; }

        [Display(Name = "Rapportresultat")]
        public string ReportResult { get => ReportItems == null ? string.Empty : ReportItems > 0 ? $"Din sökning gav {ReportItems} träffar, klicka på länken nedan för att exportera resultatet till Excel" : "Din sökning gav inga träffar, kontrollera datumintervallet och försök igen."; }

        public int? ReportItems { get; set; }

        public string StartDate { get; set; }

        public string EndDate { get; set; }

        public ReportType SelectedReportType { get; set; }

    }
}
