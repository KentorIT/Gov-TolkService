using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class Broker
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int BrokerId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        [MaxLength(255)]
        public string EmailAddress { get; set; }

        [MaxLength(255)]
        public string EmailDomain { get; set; }

        [MaxLength(32)]
        public string OrganizationNumber { get; set; }

        [MaxLength(8)]
        public string OrganizationPrefix { get; set; }

        [MaxLength(32)]
        public string PeppolId { get; set; }

        #region navigation properites

        public List<Ranking> Rankings { get; private set; }

        public List<AspNetUser> Users { get; set; }

        [MaxLength(100)]
        public string ContactPhoneNumber { get; set; }

        [MaxLength(255)]
        public string ContactEmailAddress { get; set; }

        public string BrokerContactInformation => $"{Name}{ContactEmailAddressToDisplay}{ContactPhoneNumberToDisplay}";

        private string ContactEmailAddressToDisplay => string.IsNullOrWhiteSpace(ContactEmailAddress) ? string.Empty : $"\n{ContactEmailAddress}";
        private string ContactPhoneNumberToDisplay => string.IsNullOrWhiteSpace(ContactPhoneNumber) ? string.Empty : $"\nTel: {ContactPhoneNumber}";
    }

    #endregion
}
