using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class ContractBrokerListItemModel
    {
        public string Broker { get; set; }

        public IEnumerable<BrokerRankModel> RegionRankings { get; set; }
    }
}
