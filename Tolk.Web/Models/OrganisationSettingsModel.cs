using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class OrganisationSettingsModel : IModel
    {
        public string Message { get; set; }

        [Display(Name = "Användarnamnet kopplat till inloggning med API-nyckel")]
        public string ApiUserName { get; set; }

        [Display(Name = "E-postadress bokningar", Description = "Till denna e-postadress skickas Tolktjänstens automatiska notifieringar (t ex inkomna bokningar, avbokningar m.m.) såvida ingen annan specifik e-postadress finns angiven under Notifieringsinställningar")]
        [Required]
        [EmailAddress]
        [RegularExpression(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*@((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$", ErrorMessage = "Felaktig e-postadress")]
        public string EmailRequests { get; set; }

        [Display(Name = "E-postadress", Description = "E-postadressen visas på bokningar så att myndigheten kan ta kontakt vid eventuella frågor")]
        [EmailAddress]
        [RegularExpression(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*@((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$", ErrorMessage = "Felaktig e-postadress")]
        public string ContactEmail{ get; set; }

        [Display(Name = "Namn")]
        public string  BrokerName { get; set; }

        [Display(Name = "Telefon", Description = "Telefonnumret visas på bokningar så att myndigheten kan ta kontakt vid eventuella frågor")]
        public string ContactPhone { get; set; }

        [Display(Name = "Autentisera med certifikat")]
        public bool UseCertificateAuthentication { get; set; }

        [Display(Name = "Autentisera med API-nyckel")]
        public bool UseApiKeyAuthentication { get; set; }

        [Display(Name = "Certifikatets serienummer")]
        public string CertificateSerialNumber { get; set; }

        [Display(Name = "Organisationsnummer")]
        [Required]
        public string OrganisationNumber { get; set; }

        [Display(Name = "API-nyckel i webhook-anrop", Description = "Denna nyckel kommer läggas till som header i alla webhook-anrop från systemet till er.")]
        [RegularExpression(@"[ -~]*$", ErrorMessage = "API-nyckeln kan bara innehålla ascii-tecken")]
        [StringLength(1000)]
        public string CallbackApiKey { get; set; }

        public IEnumerable<NotificationSettingsDetailsModel> NotificationSettings { get; set; }
    }
}
