using System.Collections.Generic;
using System.Linq;

namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class PriceInformationModel
    {
        public IEnumerable<PriceRowModel> PriceRows { get; set; }

        public decimal TotalPrice { get => PriceRows.Sum(p => p.Price); }

        public string PriceCalculatedFromCompetenceLevel { get; set; }
    }
}
