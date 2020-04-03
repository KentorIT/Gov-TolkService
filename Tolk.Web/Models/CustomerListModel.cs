using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class CustomerListModel
    {
        public CustomerFilterModel FilterModel { get; set; }

        public IEnumerable<CustomerListItemModel> Items { get; set; }

        public bool AllowCreate { get; set; }
    }
}
