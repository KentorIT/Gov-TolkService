﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class OrganisationSettingsModel
    {
        public string Message { get; set; }

        [Display(Name = "Användarnamnet kopplat till apinyckel-inloggning")]
        public string UserName { get; set; }
        [Required]
        [EmailAddress]
        [RegularExpression(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*@((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$", ErrorMessage = "Felaktig e-postadress")]
        public string Email { get; set; }

        [Display(Name = "Autentisera med certifikat")]
        public bool UseCertificateAuthentication { get; set; }

        [Display(Name = "Autentisera med apinyckel")]
        public bool UseApiKeyAuthentication { get; set; }

        [Display(Name = "Certifikatets serienummer")]
        public string CertificateSerialNumber { get; set; }

        [Display(Name = "Organisationsnummer")]
        [Required]
        public string OrganisationNumber { get; set; }

        [Display(Name = "Apinyckel i webhook anrop", Description = "Denna nyckel kommer läggas till som header i alla webhook anrop från systemet till er.")]
        [RegularExpression(@"[ -~]*$", ErrorMessage = "Apinyckeln kan bara innehålla ascii-tecken")]
        [StringLength(1000)]
        public string CallbackApiKey { get; set; }

        public IEnumerable<NotificationSettingsDetailsModel> NotificationSettings { get; set; }
    }
}
