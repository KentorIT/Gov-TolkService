using Tolk.Web.Helpers;


namespace Tolk.Web.Models
{
    public class UnitUserModel
    {
        public bool IsLocalAdmin { get; set; }

        public string Name { get; set; }

        public bool IsActive { get; set; }

        public string ColorClassName => CssClassHelper.GetColorClassNameForItemStatus(IsActive);
    }
}
