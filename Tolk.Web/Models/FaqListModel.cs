using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class FaqListModel
    {
        public FaqFilterModel FilterModel { get; set; }

        public bool IsBroker { get; set; }

        public IEnumerable<FaqListItemModel> Items { get; set; }
    }
}
