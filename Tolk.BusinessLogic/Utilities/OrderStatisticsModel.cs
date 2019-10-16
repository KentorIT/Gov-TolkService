using System.Collections.Generic;
using System.Linq;

namespace Tolk.BusinessLogic.Utilities
{
    public class OrderStatisticsModel
    {

        private const int MaxNoOfOrderToDisplayDefault = 5;

        public IEnumerable<OrderStatisticsListItemModel> TotalListItems { get; set; }

        public IEnumerable<OrderStatisticsListItemModel> TopListItems { get => TotalListItems.Take(MaxNoOfOrderToDisplayDefault); }

        public string Name { get; set; }

        public bool MoreToDisplay { get => TotalListItems.Count() > TopListItems.Count(); }
    }
}
