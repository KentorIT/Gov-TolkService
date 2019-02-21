using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class OrganisationSettingsModel
    {
        public string Message { get; set; }

        [Display(Name = "Användarnamnet kopplat till apinyckel-inloggning")]
        public string UserName { get; set; }
        [Required]
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

        public IEnumerable<NotificationSettingsDetailsModel> NotificationSettings { get; set; }
    }
}
