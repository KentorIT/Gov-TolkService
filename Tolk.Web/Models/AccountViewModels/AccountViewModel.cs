using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Models.AccountViewModels
{
    public class AccountViewModel
    {
        [Display(Name = "Namn")]
        public string NameFull { get; set; }

        [Display(Name = "E-postadress")]
        public string Email { get; set; }

        [Display(Name = "Telefonnummer (arbete)")]
        public string PhoneWork { get; set; }

        [Display(Name = "Telefonnummer (mobil)")]
        public string PhoneCellphone { get; set; }
    }
}
