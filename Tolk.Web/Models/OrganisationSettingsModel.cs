using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class OrganisationSettingsModel
    {
        [Display(Name = "Användarnamnet kopplat till apinyckel-inloggning")]
        public string UserName { get; set; }
        [Required]
        public string Email { get; set; }

        [Display(Name = "Authentisera med certifikat")]
        public bool UseCertificateAuthentication { get; set; }

        [Display(Name = "Authenticera med apinyckel")]
        public bool UseApiKeyAuthentication { get; set; }

        [Display(Name = "Apinyckel")]
        [DataType(DataType.Password)]
        public string ApiKey { get; set; }

        [Display(Name = "Certifikatets serienummer")]
        public string CertificateSerialNumber { get; set; }

        [Display(Name = "Organisationsnummer")]
        public string OrganisationNumber { get; set; }

        //TO BE REMOVED!! SHOULD BE HANDLED IN SEPARATE LIST UI!!

        [Display(Name = "Använd web hook för skapad förfrågan")]
        public bool UseWebHook { get; set; }

        [Display(Name = "Web hook för skapad förfrågan(request_created)")]
        public string RequestCreatedWebHook { get; set; }

    }
}
