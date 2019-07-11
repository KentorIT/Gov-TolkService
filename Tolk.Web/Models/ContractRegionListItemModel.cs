using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class ContractRegionListItemModel
    {
        public string Region { get; set; }

        public List<BrokerRankModel> Brokers { get; set; }
    }
}
