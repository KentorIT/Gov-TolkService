using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class ComplaintFilterModel
    {
        [Display(Name = "Boknings-ID")]
        public string OrderNumber { get; set; }

        [Display(Name = "Status")]
        public ComplaintStatus? Status { get; set; }

        [Display(Name = "Skapad av")]
        public int? CustomerContactId { get; set; }

        [Display(Name = "Myndighetens enhet")]
        public int? CustomerUnitId { get; set; }

        [Display(Name = "Visa inte reklamationer för inaktiva enheter")]
        public bool? FilterByInactiveUnits { get; set; }

        [Display(Name = "Myndighet")]
        public int? CustomerOrganisationId { get; set; }

        [Display(Name = "Förmedling")]
        public int? BrokerId { get; set; }

        [Display(Name = "Besvarad av")]
        public int? BrokerContactId { get; set; }

        public bool IsCustomerCentralAdminOrOrderHandler { get; set; }

        public bool IsBrokerUser { get; set; }

        public bool HasCustomerUnits { get; set; }

        public bool HasActiveFilters => CustomerContactId.HasValue || !string.IsNullOrWhiteSpace(OrderNumber) || BrokerContactId.HasValue || Status.HasValue || CustomerOrganisationId.HasValue || BrokerId.HasValue;

        internal IQueryable<Complaint> Apply(IQueryable<Complaint> items)
        {
            items = !string.IsNullOrWhiteSpace(OrderNumber) ? items.Where(i => i.Request.Order.OrderNumber.Contains(OrderNumber)) : items;
            items = Status.HasValue ? items.Where(c => c.Status == Status) : items;
            items = CustomerContactId.HasValue ? items.Where(c => c.CreatedBy == CustomerContactId) : items;
            items = BrokerContactId.HasValue ? items.Where(c => c.AnsweredBy == BrokerContactId) : items;
            items = BrokerId.HasValue ? items.Where(c => c.Request.Ranking.BrokerId == BrokerId) : items;
            items = CustomerOrganisationId.HasValue ? items.Where(c => c.Request.Order.CustomerOrganisationId == CustomerOrganisationId) : items;
            items = CustomerUnitId.HasValue ? items.Where(c => c.Request.Order.CustomerUnitId == CustomerUnitId) : items;
            items = FilterByInactiveUnits.HasValue ? items.Where(c => c.Request.Order.CustomerUnit == null || c.Request.Order.CustomerUnit.IsActive) : items;
            return items;
        }
    }
}
