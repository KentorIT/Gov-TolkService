using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models.AccountViewModels
{
    public class RegisterNewUserViewModel
    {
        public string UserId { get; set; }

        [ClientRequired]
        [Display(Name = "Nytt lösenord")]
        public string Password { get; set; }

        [ClientRequired]
        [Display(Name = "Bekräfta lösenord")]
        public string PasswordRetype { get; set; }

        [ClientRequired]
        [Display(Name = "Förnamn")]
        public string NameFirst { get; set; }

        [ClientRequired]
        [Display(Name = "Efternamn")]
        public string NameFamily { get; set; }

        [Display(Name = "Telefonnummer (arbete)")]
        public string PhoneWork { get; set; }

        [Display(Name = "Telefonnummer (mobil)")]
        public string PhoneCellphone { get; set; }
    }
}
