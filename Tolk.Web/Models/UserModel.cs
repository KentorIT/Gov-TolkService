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
        public int? Id { get; set; }

        [Display(Name = "Namn")]
        public string NameFull => $"{NameFirst} {NameFamily}";

        [Required]
        [EmailAddress(ErrorMessage = "Felaktig epostadress")]
        [Display(Name = "E-post")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Förnamn")]
        public string NameFirst { get; set; }

        [Required]
        [Display(Name = "Efternamn")]
        public string NameFamily { get; set; }

        [Display(Name = "Telefonnummer (arbete)")]
        public string PhoneWork { get; set; }

        [Display(Name = "Telefonnummer (mobil)")]
        public string PhoneCellphone { get; set; }

        [Display(Name = "Lokal administratör")]
        public bool IsSuperUser { get; set; }

        [Display(Name = "Aktiv")]
        public bool IsActive { get; set; }

        [Display(Name = "Senaste inloggning")]
        public string LastLoginAt { get; set; }

        [Display(Name = "Organisation")]
        public string Organisation { get; set; }

        [Display(Name = "Organisation")]
        public string OrganisationIdentifier { get; set; }

        public bool EditorIsSystemAdministrator { get; set; }

        /// <summary>
        /// If set, the server code found some error that slipped through the client sides fingers.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
