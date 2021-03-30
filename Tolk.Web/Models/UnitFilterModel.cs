using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class UnitFilterModel
    {
        [Display(Name = "Namn")]
        public string Name { get; set; }

        [Display(Name = "Status")]
        public ActiveStatus? Status { get; set; }

        internal IQueryable<CustomerUnit> Apply(IQueryable<CustomerUnit> units)
        {
            units = !string.IsNullOrWhiteSpace(Name) ? units.Where(u => u.Name.Contains(Name)) : units;
            if (Status.HasValue)
            {
                units = Status.Value == ActiveStatus.Active ? units.Where(u => u.IsActive) : units.Where(u => !u.IsActive);
            }
            return units;
        }
    }
}
