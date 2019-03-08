using System;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class FailedTryModel
    {
        [Display(Name = "Tidpunkt")]
        public DateTime FailedAt { get; set; }
        [Display(Name = "Felmeddelande")]
        public string ErrorMessage { get; set; }
    }
}
