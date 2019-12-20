using System.Collections.Generic;
using System.Linq;

namespace Tolk.BusinessLogic.Utilities
{
    public class DisplayPriceInformation
    {
        public List<DisplayPriceRow> DisplayPriceRows { get; } = new List<DisplayPriceRow>();

        public List<DisplayPriceInformation> SeparateSubTotal { get; } = new List<DisplayPriceInformation>();

        public decimal TotalPrice { get => DisplayPriceRows.Sum(p => p.RoundedPrice); }

        public string PriceListTypeDescription { get; set; }

        public string CompetencePriceDescription { get; set; }

        public string SubPriceHeader { get; set; }

        public IEnumerable<MealBreakInformation> MealBreaks { get; set; }

    }
}
