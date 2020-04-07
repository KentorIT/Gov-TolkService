using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;

namespace Tolk.Web.Models
{
    public class AdminUnitFilterModel
    {
        public int Id { get; set; }

        [Display(Name = "Sök enhet")]
        [Placeholder("Söker på delar av namn eller e-postadress")]
        public string SearchString { get; set; }

    }
}
