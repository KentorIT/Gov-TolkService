using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models.AccountViewModels
{
    public class RegisterNewAccountViewModel : NewPasswordModelBase
    {
        public string UserId { get; set; }

        public string PasswordToken { get; set; }

        [ClientRequired]
        [Display(Name = "Förnamn")]
        [StringLength(255)]
        public string NameFirst { get; set; }

        [ClientRequired]
        [Display(Name = "Efternamn")]
        [StringLength(255)]
        public string NameFamily { get; set; }

        [Display(Name = "Telefonnummer (arbete)")]
        [StringLength(32)]
        public string PhoneWork { get; set; }

        [Display(Name = "Telefonnummer (mobil)")]
        [StringLength(32)]
        public string PhoneCellphone { get; set; }
    }
}
