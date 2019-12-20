using System.ComponentModel.DataAnnotations;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class UnitUserModel
    {
        public int CustomerUnitId { get; set; }

        [Display(Name = "Lokal administratör")]
        [SubItem]
        public bool IsLocalAdmin { get; set; }

        public string Name { get; set; }

        public bool IsActive { get; set; }

        [Display(Name = "Koppla användare")]
        [SubItem]
        public bool UserIsConnected { get; set; }

        public string ColorClassName => CssClassHelper.GetColorClassNameForItemStatus(IsActive);
    }
}
