using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class InterpreterFilterModel
    {
        public string Message { get; set; }

        [Display(Name = "Kammarkollegiets tolknummer")]
        public string OfficialInterpreterId { get; set; }

        [Display(Name = "Namn")]
        public string Name { get; set; }

        public bool HasActiveFilters
        {
            get => !string.IsNullOrWhiteSpace(OfficialInterpreterId) || !string.IsNullOrWhiteSpace(Name); 
        }

        internal IQueryable<InterpreterBroker> Apply(IQueryable<InterpreterBroker> items)
        {
#pragma warning disable CA1307 // if a StringComparison is provided, the filter has to be evaluated on server...
            items = !string.IsNullOrWhiteSpace(OfficialInterpreterId)
                ? items.Where(i => i.OfficialInterpreterId.Contains(OfficialInterpreterId))
                : items;
            items = !string.IsNullOrWhiteSpace(Name)
               ? items.Where(i => i.FirstName.Contains(Name) || i.LastName.Contains(Name) || (i.FirstName + i.LastName).Contains(Name.Replace(" ", "")))
               : items;
#pragma warning restore CA1307 // 
            return items;
        }
    }
}
