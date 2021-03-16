using System.ComponentModel.DataAnnotations;
using Tolk.Web.Attributes;

namespace Tolk.Web.Models
{
    public class AdminUnitFilterModel : IModel
    {
        public int Id { get; set; }

        [Display(Name = "Sök enhet")]
        [Placeholder("Söker på delar av namn eller e-postadress")]
        public string SearchString { get; set; }

    }
}
