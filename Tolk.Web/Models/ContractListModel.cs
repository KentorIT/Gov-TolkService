using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class ContractListModel
    {
        public string ContractNumber { get; set; }

        public IEnumerable<ContractRegionListItemModel> ItemsPerRegion { get; set; }

        public IEnumerable<ContractBrokerListItemModel> ItemsPerBroker { get; set; }
    }
}
