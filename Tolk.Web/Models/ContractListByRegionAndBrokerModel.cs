using System.Collections.Generic;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class ContractListByRegionAndBrokerModel
    {
        public CurrentOrLatestFrameworkAgreement ConnectedFrameworkAgreement { get; set; }
        public IEnumerable<ContractRegionListItemModel> ItemsPerRegion { get; set; }

        public IEnumerable<ContractBrokerListItemModel> ItemsPerBroker { get; set; }
    }
}
