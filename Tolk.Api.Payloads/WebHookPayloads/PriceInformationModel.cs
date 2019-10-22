using System.Collections.Generic;
using System.Linq;

namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class PriceInformationModel
    {
        public IEnumerable<PriceRowModel> PriceRows { get; set; }

        public decimal TotalPrice => PriceRows.Sum(p => p.RoundedPrice);

        public string PriceCalculatedFromCompetenceLevel { get; set; }
    }
}
