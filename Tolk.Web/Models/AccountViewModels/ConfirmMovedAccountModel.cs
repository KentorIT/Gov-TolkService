using System.ComponentModel.DataAnnotations;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models.AccountViewModels
{
    public class ConfirmMovedAccountModel
    {
        public int UserId { get; set; }

        [Required]
        [Display(Name = "Förnamn")]
        [StringLength(255)]
        public string NameFirst { get; set; }

        [Required]
        [Display(Name = "Efternamn")]
        [StringLength(255)]
        public string NameFamily { get; set; }

        [NoAutoComplete]
        [Display(Name = "Telefonnummer (arbete)")]
        [StringLength(32)]
        public string PhoneWork { get; set; }

        [NoAutoComplete]
        [Display(Name = "Telefonnummer (mobil)")]
        [StringLength(32)]
        public string PhoneCellphone { get; set; }

        [Display(Name = "Ny organisation")]
        public string CustomerOrganisationName { get; set; }
    }
}
