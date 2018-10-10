using System.Collections.Generic;
using System.Linq;

namespace Tolk.BusinessLogic.Utilities
{
    public class PriceInformation
    {
        public List<PriceRowBase> PriceRows { get; set; }

        public decimal TotalPrice { get => PriceRows.Sum(p => p.RoundedTotalPrice); }
    }
}
