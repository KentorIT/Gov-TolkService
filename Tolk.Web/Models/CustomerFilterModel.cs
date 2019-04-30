using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;

namespace Tolk.Web.Models
{
    public class CustomerFilterModel
    {
        [Display(Name = "Föräldermyndighet")]
        public int? ParentId { get; set; }

        [Display(Name = "Prislista")]
        public PriceListType? PriceListType { get; set; }

        [Display(Name = "Namn")]
        [Placeholder("Sök på del av namn")]
        public string Name { get; set; }
        public bool HasActiveFilters => ParentId.HasValue || !string.IsNullOrWhiteSpace(Name) || PriceListType.HasValue;
        internal IQueryable<CustomerOrganisation> Apply(IQueryable<CustomerOrganisation> items)
        {
            items = PriceListType.HasValue
               ? items.Where(c => c.PriceListType == PriceListType)
               : items;
            items = ParentId.HasValue
               ? items.Where(c => c.ParentCustomerOrganisationId == ParentId)
               : items;
            items = !string.IsNullOrWhiteSpace(Name)
                ? items.Where(c => c.Name.Contains(Name))
                : items;
           return items;
        }

    }
}
