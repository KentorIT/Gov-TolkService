using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class ComplaintListModel
    {
        public ComplaintFilterModel FilterModel { get; set; }

        public IEnumerable<ComplaintListItemModel> Items { get; set; }
    }
}
