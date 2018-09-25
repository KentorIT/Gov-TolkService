using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class UserModel
    {
        public int Id { get; set; }

        [Display(Name = "Namn")]
        public string NameFull => $"{NameFirst} {NameFamily}";

        [Display(Name = "Email")]
        public string Email { get; set; }

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

        [Display(Name = "Lokal administratör")]
        public bool IsSuperUser { get; set; }
    }
}
