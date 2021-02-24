using System.ComponentModel.DataAnnotations;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class ChangeApiKeyModel : IModel
    {
        [Required]
        [DataType(DataType.Password)]
        [NoAutoComplete]
        [Display(Name = "Lösenord", Description = "Bekräfta ändringen med ditt lösenord")]
        [StringLength(100)]
        public string CurrentPassword { get; set; }

        [Required]
        [NoAutoComplete]
        [Display(Name = "API-nyckel", Description = "Lagra informationen noga, för detta blir enda gången du får se nyckeln.")]
        [StringLength(255)]
        public string ApiKey { get; set; }
    }
}
