using System.Collections.Generic;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class ContractListByRegionAndServiceModel
    {
        public CurrentOrLatestFrameworkAgreement ConnectedFrameworkAgreement { get; set; }
                
        public IEnumerable<ContractBrokerListItemModel> ItemsPerBroker { get; set; }
        public IEnumerable<ContractRegionListItemModel> ItemsPerRegion { get; set; }



    }
}
