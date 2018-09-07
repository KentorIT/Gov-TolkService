using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class ComplaintFilterModel
    {
        [Display(Name = "Avrops-ID")]
        public string OrderNumber { get; set; }

        public ComplaintStatus? Status { get; set; }

        internal IQueryable<ComplaintListItemModel> Apply(IQueryable<ComplaintListItemModel> items)
        {
            // OrderNumber
            items = !string.IsNullOrWhiteSpace(OrderNumber)
                ? items.Where(i => i.OrderNumber.Contains(OrderNumber))
                : items;
            // Status
            if (Status.HasValue)
            {
                items = items.Where(r => r.Status == Status);
            }

            return items;
        }
    }
}
