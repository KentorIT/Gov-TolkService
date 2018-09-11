﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models.AccountViewModels
{
    public class RegisterNewUserViewModel
    {
        [ClientRequired]
        [Display(Name = "Lösenord")]
        public string Password { get; set; }

        [ClientRequired]
        [Display(Name = "Lösenord (igen)")]
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
