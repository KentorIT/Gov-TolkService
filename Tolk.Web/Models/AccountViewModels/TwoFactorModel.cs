using System;
using System.ComponentModel.DataAnnotations;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models.AccountViewModels
{
    public class TwoFactorModel
    {
        [Required(ErrorMessage = "Valideringskod måste anges")]
        [NoAutoComplete]
        [Display(Name = "Valideringskod", Description = "Detta är en kod som levererats till dig via e-post")]
        public string Code { get; set; }
        public Uri ReturnUrl { get; set; }
        public string Message { get; set; }
    }
}
