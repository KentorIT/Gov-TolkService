using System.Collections.Generic;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class UserListModel
    {
        public UserFilterModel FilterModel { get; set; }

        public IEnumerable<UserListItemModel> Items { get; set; }

        public UserPageMode UserPageMode { get; set; }
    }
}
