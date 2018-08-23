using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class ComplaintFilterModel
    {
        [Display(Name = "Avrops-ID")]
        public string OrderNumber { get; set; }

        [Display(Name = "Språk")]
        public int? LanguageId { get; set; }

        [Display(Name = "Startdatum")]
        public DateRange StartTimeRange { get; set; }

        public ComplaintStatus? Status { get; set; }
    }
}
