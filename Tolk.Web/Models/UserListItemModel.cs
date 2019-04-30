using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class UserListItemModel
    {
        public int UserId { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Organisation { get; set; }

        public string LastLoginAt { get; set; }

        public bool IsActive { get; set; }

        public string ColorClassName  => CssClassHelper.GetColorClassNameForActiveStatus(IsActive);
    }
}
