using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class DynamicUserListItemModel
    {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string IsActive { get; set; }
            public string CombinedId { get; set; }
            public string IsLocalAdmin { get; set; }
            public string ColorClassName => CssClassHelper.GetColorClassNameForItemStatus(IsActive == "Aktiv");
    }
}
