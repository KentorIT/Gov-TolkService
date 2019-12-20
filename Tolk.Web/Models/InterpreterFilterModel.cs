using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;

namespace Tolk.Web.Models
{
    public class InterpreterFilterModel
    {

        [Display(Name = "Kammarkollegiets tolknummer")]
        public string OfficialInterpreterId { get; set; }

        [Display(Name = "Namn")]
        public string Name { get; set; }

        [Display(Name = "E-postadress")]
        public string Email { get; set; }

        internal IQueryable<InterpreterBroker> Apply(IQueryable<InterpreterBroker> items)
        {
#pragma warning disable CA1307 // if a StringComparison is provided, the filter has to be evaluated on server...
            items = !string.IsNullOrWhiteSpace(OfficialInterpreterId)
                ? items.Where(i => i.OfficialInterpreterId.Contains(OfficialInterpreterId))
                : items;
            items = !string.IsNullOrWhiteSpace(Name)
               ? items.Where(i => i.FirstName.Contains(Name) || i.LastName.Contains(Name) || (i.FirstName + i.LastName).Contains(Name.Replace(" ", "")))
               : items;
            items = !string.IsNullOrWhiteSpace(Email)
              ? items.Where(i => i.Email.Contains(Email))
              : items;
#pragma warning restore CA1307 // 
            return items;
        }
    }
}
