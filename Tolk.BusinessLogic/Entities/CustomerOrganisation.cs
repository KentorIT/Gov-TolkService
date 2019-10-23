using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class CustomerOrganisation
    {
        public int CustomerOrganisationId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public List<AspNetUser> Users { get; set; }

        public PriceListType PriceListType { get; set; }

        [MaxLength(50)]
        public string EmailDomain { get; set; }

        public int? ParentCustomerOrganisationId { get; set; }

        [ForeignKey(nameof(ParentCustomerOrganisationId))]
        [InverseProperty(nameof(SubCustomerOrganisations))]
        public CustomerOrganisation ParentCustomerOrganisation { get; set; }

        [InverseProperty(nameof(ParentCustomerOrganisation))]
        public List<CustomerOrganisation> SubCustomerOrganisations { get; set; }

        [MaxLength(8)]
        public string OrganisationPrefix { get; set; }

        [Required]
        [MaxLength(32)]
        public string OrganisationNumber { get; set; }

        [Required]
        public TravelCostAgreementType TravelCostAgreementType { get; set; }
    }
}
