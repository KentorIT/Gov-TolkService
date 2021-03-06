﻿using System.ComponentModel.DataAnnotations;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models.AccountViewModels
{
    public class RegisterNewAccountViewModel : NewPasswordModelBase
    {
        public string UserId { get; set; }

        public string PasswordToken { get; set; }

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

        public string Email { get; set; }

        public bool IsCustomer { get; set; }
    }
}
