using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models.AccountViewModels
{
    public class AccountViewModel
    {
        [Display(Name = "Användarnamn")]
        public string UserName { get; set; }

        [Display(Name = "Namn")]
        public string NameFull { get; set; }

        [ClientRequired]
        [Display(Name = "Förnamn")]
        [StringLength(255)]
        public string NameFirst { get; set; }

        [ClientRequired]
        [Display(Name = "Efternamn")]
        [StringLength(255)]
        public string NameFamily { get; set; }

        [Display(Name = "E-postadress")]
        public string Email { get; set; }

        [Display(Name = "Telefonnummer (arbete)")]
        public string PhoneWork { get; set; }

        [Display(Name = "Telefonnummer (mobil)")]
        public string PhoneCellphone { get; set; }

        public bool AllowDefaultSettings { get; set; }

        public IEnumerable<UnitUserModel> CustomerUnits { get; set; }
    }
}
