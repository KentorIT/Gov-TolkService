using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class ContractPricePerRegionGroupListItemModel
    {
        public string RegionGroupName { get; set; }
        public IEnumerable<ContractBrokerFeeCompetenceItemModel> BrokerFeePerCompentence { get; set; }        
    }
}