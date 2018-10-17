using System.Collections.Generic;
using System.Linq;

namespace Tolk.BusinessLogic.Utilities
{
    public class DisplayPriceInformation
    {
        public List<DisplayPriceRow> DisplayPriceRows { get; set; } = new List<DisplayPriceRow>();

        public List<DisplayPriceInformation> SeparateSubTotal { get; set; } = new List<DisplayPriceInformation>();

        public decimal TotalPrice { get => DisplayPriceRows.Sum(p => p.RoundedPrice); }

        public string HeaderDescription { get; set; }
    }
}
