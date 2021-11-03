using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class CustomerOrganisationHistoryEntry
    {
        internal CustomerOrganisationHistoryEntry(CustomerOrganisation customer)
        {
            Name = customer.Name;
            PriceListType = customer.PriceListType;
            EmailDomain = customer.EmailDomain;
            OrganisationPrefix = customer.OrganisationPrefix;
            OrganisationNumber = customer.OrganisationNumber;
            PeppolId = customer.PeppolId;
            TravelCostAgreementType = customer.TravelCostAgreementType;
            UseOrderAgreementsFromDate = customer.UseOrderAgreementsFromDate;
        }

        private CustomerOrganisationHistoryEntry() { }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerOrganisationHistoryEntryId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public PriceListType PriceListType { get; set; }

        [MaxLength(50)]
        public string EmailDomain { get; set; }

        [MaxLength(8)]
        public string OrganisationPrefix { get; set; }

        [Required]
        [MaxLength(32)]
        public string OrganisationNumber { get; set; }

        [MaxLength(50)]
        public string PeppolId { get; set; }

        [Required]
        public TravelCostAgreementType TravelCostAgreementType { get; set; }
        public DateTime? UseOrderAgreementsFromDate { get; set; }

        public int CustomerChangeLogEntryId { get; set; }

        [ForeignKey(nameof(CustomerChangeLogEntryId))]
        public CustomerChangeLogEntry CustomerChangeLogEntry { get; set; }
    }
}
