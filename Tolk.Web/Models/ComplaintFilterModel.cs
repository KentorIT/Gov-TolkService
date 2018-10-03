using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class ComplaintFilterModel
    {
        [Display(Name = "Avrops-ID")]
        public string OrderNumber { get; set; }

        [Display(Name = "Status")]
        public ComplaintStatus? Status { get; set; }

        [Display(Name = "Skapad av")]
        public int? CustomerContactId { get; set; }

        public bool IsCustomerSuperUser { get; set; }

        internal IQueryable<Complaint> Apply(IQueryable<Complaint> items)
        {
            // OrderNumber
            items = !string.IsNullOrWhiteSpace(OrderNumber) ? 
                items.Where(i => i.Request.Order.OrderNumber.Contains(OrderNumber)) : 
                items;
            // Status
            items = Status.HasValue ? items.Where(c => c.Status == Status) : items;

            items = CustomerContactId.HasValue ? items.Where(c => c.CreatedBy == CustomerContactId) : items;
            return items;
        }
    }
}
