using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class StartList
    {
        public string Header { get; set; }

        public string EmptyMessage { get; set; }

        public IEnumerable<StartListItemModel> StartListObjects { get; set; }

        public bool HasReviewAction { get; set; } = false;

        public bool DisplayCustomer { get; set; } = false;
    }

}
