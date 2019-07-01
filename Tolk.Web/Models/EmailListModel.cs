using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class EmailListModel
    {
        public EmailFilterModel FilterModel { get; set; }

        public IEnumerable<EmailListItemModel> Items { get; set; }
    }
}
