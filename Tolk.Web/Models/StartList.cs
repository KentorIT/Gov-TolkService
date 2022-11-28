using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class StartList
    {
        public string HeaderClass { get; set; }

        public string HeaderLoading { get; set; }

        public string Header { get; set; }

        public string EmptyHeader { get; set; }

        public string EmptyMessage { get; set; }

        public bool HasReviewAction { get; set; } = false;

        public ActionDefinition TableDataPath { get; set; }

        public ActionDefinition TableColumnDefinitionPath { get; set; }

        public ActionDefinition DefaultLinkPath { get; set; }
    }
    public class ActionDefinition
    {
        public string Controller { get; set; }
        public string Action { get; set; }
    }
}
