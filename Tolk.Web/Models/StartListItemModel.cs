using System;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class StartListItemModel
    {
        public string DefaulListAction { get; set; }

        public string DefaulListController { get; set; }

        public int DefaultItemId { get; set; }

        public string DefaultItemTab { get; set; }

        public string ButtonAction { get; set; }

        public string ButtonController { get; set; }

        public int ButtonItemId { get; set; }

        public string ButtonItemTab { get; set; }

        public StartListItemStatus Status { get; set; }

        public string CustomerName { get; set; }

        public string OrderNumber { get; set; }

        [NoDisplayName]
        public TimeRange Orderdate { get; set; }

        public CompetenceAndSpecialistLevel? CompetenceLevel { get; set; }

        public string Language { get; set; }

        public DateTime InfoDate { get; set; }

        public DateTime? LatestDate { get; set; }

        public string InfoDateDescription { get; set; } = "Inkommen: ";

        public string ColorClassName { get => CssClassHelper.GetColorClassNameForStartListItem(Status); }
    }

}
