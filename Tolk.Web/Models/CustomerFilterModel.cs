using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;

namespace Tolk.Web.Models
{
    public class CustomerFilterModel : IModel
    {
        [Display(Name = "Föräldermyndighet")]
        public int? ParentId { get; set; }

        [Display(Name = "Prislista")]
        public PriceListType? PriceListType { get; set; }

        [Display(Name = "Namn")]
        [Placeholder("Sök på del av namn")]
        public string Name { get; set; }

        [Display(Name = "Organisationsnummer")]
        [Placeholder("Sök på del av organisationsnummer")]
        public string OrganisationNumber { get; set; }

        public bool HasActiveFilters => ParentId.HasValue || !string.IsNullOrWhiteSpace(Name) || PriceListType.HasValue || !string.IsNullOrWhiteSpace(OrganisationNumber);
        internal IQueryable<CustomerOrganisation> Apply(IQueryable<CustomerOrganisation> items)
        {
            items = PriceListType.HasValue
               ? items.Where(c => c.PriceListType == PriceListType)
               : items;
            items = ParentId.HasValue
               ? items.Where(c => c.ParentCustomerOrganisationId == ParentId)
               : items;
#pragma warning disable CA1307 // if a StringComparison is provided, the filter has to be evaluated on server...
            items = !string.IsNullOrWhiteSpace(OrganisationNumber)
                ? items.Where(c => c.OrganisationNumber.Contains(OrganisationNumber))
                : items;
            items = !string.IsNullOrWhiteSpace(Name)
                ? items.Where(c => c.Name.Contains(Name))
                : items;
#pragma warning restore CA1307 // 
            return items;
        }

    }
}
