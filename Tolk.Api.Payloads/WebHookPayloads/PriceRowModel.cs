using System.Collections.Generic;
using System.Linq;

namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class PriceRowModel
    {
        private decimal price;

        public string PriceRowType { get; set; }

        public string Description { get; set; }

        public decimal Price
        {
            get
            {
                if (PriceListRows != null && PriceListRows.Any())
                {
                    return PriceListRows.Sum(r => r.TotalPrice);
                }
                return price;
            }
            set
            {
                price = value;
            }
        }

        public IEnumerable<PriceRowListModel> PriceListRows { get; set; }

        public string CalculatedFrom { get; set; }

        public decimal? CalculationBase { get; set; }
    }
}
