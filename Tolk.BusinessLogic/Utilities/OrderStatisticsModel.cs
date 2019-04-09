using System.Collections.Generic;
using System.Linq;

namespace Tolk.BusinessLogic.Utilities
{
    public class OrderStatisticsModel
    {
        private static readonly int MaxNoOfOrderToDisplayDefault = 5;

        public IDictionary<string, decimal> TopListItems { get => TotalListItems.Take(MaxNoOfOrderToDisplayDefault).ToDictionary(n => n.Key, n => n.Value); }

        public IDictionary<string, decimal> TotalListItems { get; set; }

        public string Name { get; set; }
    }
}
