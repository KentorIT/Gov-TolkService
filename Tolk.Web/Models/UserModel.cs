using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tolk.Web.Helpers;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class UserModel
    {
        public int? Id { get; set; }

        [Display(Name = "Användarnamn")]
        public string UserName { get; set; }

        [Display(Name = "Namn")]
        public string NameFull => $"{NameFirst} {NameFamily}";

        [ClientRequired]
        [EmailAddress(ErrorMessage = "Felaktig e-postadress")]
        [RegularExpression(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*@((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$", ErrorMessage = "Felaktig e-postadress")]
        [Display(Name = "E-post")]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Förnamn")]
        [StringLength(255)]
        public string NameFirst { get; set; }

        [Required]
        [Display(Name = "Efternamn")]
        [StringLength(255)]
        public string NameFamily { get; set; }

        [Display(Name = "Telefonnummer (arbete)")]
        [StringLength(32)]
        public string PhoneWork { get; set; }

        [Display(Name = "Telefonnummer (mobil)")]
        [StringLength(32)]
        public string PhoneCellphone { get; set; }

        [Display(Name = "Central administratör")]
        public bool IsOrganisationAdministrator { get; set; }

        [Display(Name = "Rätt att hantera alla myndighetens avrop")]
        public bool IsCentralOrderHandler { get; set; }

        [Display(Name = "Applikationsadministratör")]
        public bool IsApplicationAdministrator { get; set; }

        [Display(Name = "Systemadministratör")]
        public bool IsSystemAdministrator { get; set; }

        [Display(Name = "Kan överta annan användare")]
        public bool IsImpersonator { get; set; }

        [Display(Name = "Aktiv")]
        public bool IsActive { get; set; }

        [Display(Name = "Senaste inloggning")]
        public string LastLoginAt { get; set; }

        [Display(Name = "Organisation")]
        [StringLength(20)]
        public string Organisation { get; set; }

        [ClientRequired]
        [Display(Name = "Organisation")]
        public string OrganisationIdentifier { get; set; }

        public UserType UserType { get; set; }

        public List<UnitUserModel> UnitUsers { get; set; }

        public List<UnitUserModel> NonEditableUnitUsers { get; set; }

        public bool HasSelectedOrganisation { get; set; }

        public bool HasSelectedCustomerunit { get; set; }

        public bool DisplayCentralOrderHandler { get; set; }

        public bool DisplayCentralAdmin{ get; set; }

        public bool DisplayForAdminUser { get; set; }

        /// <summary>
        /// If set, the server code found some error that slipped through the client sides fingers.
        /// </summary>
        public string ErrorMessage { get; set; }

        public UserPageMode UserPageMode { get; set; }
    }
}
