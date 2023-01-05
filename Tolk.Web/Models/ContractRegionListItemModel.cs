using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class ContractRegionListItemModel
    {
        public string Region { get; set; }
        public string RegionGroup { get; set; }

        public IEnumerable<BrokerRankModel> Brokers { get; set; }
    }
}
