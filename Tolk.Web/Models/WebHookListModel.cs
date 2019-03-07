using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class WebHookListModel
    {
        public WebHookFilterModel FilterModel { get; set; }

        public IEnumerable<WebHookListItemModel> Items { get; set; }
    }
}
