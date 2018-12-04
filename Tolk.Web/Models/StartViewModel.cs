using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class StartViewModel
    {

        public IEnumerable<StartList> StartLists { get; set; }

        public string PageTitle { get; set; } = "Aktiva bokningsförfrågningar";


        public string Message { get; set; }
        public IEnumerable<StartPageBox> Boxes { get; set; }

        public IEnumerable<ConfirmationMessage> ConfirmationMessages { get; set; }

        public class StartPageBox
        {
            public string Header { get; set; }
            public int Count { get; set; }
            public string Controller { get; set; }
            public string Action { get; set; }
            public Dictionary<string, string> Filters { get; set; } = new Dictionary<string, string>();
        }

        public class StartList
        {
            public string Header { get; set; }

            public string EmptyMessage { get; set; }

            public IEnumerable<StartListItemModel> StartListObjects { get; set; }

            public bool HasReviewAction { get; set; } = false;
        }

        public class ConfirmationMessage
        {
            public string Header { get; set; }

            public string Message { get; set; }

            public string Controller { get; set; }

            public string Action { get; set; }

            public int Id { get; set; }
        }

    }


}
