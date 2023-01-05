using System.Collections.Generic;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class ContractListByRegionGroupAndServiceModel
    {
        public CurrentOrLatestFrameworkAgreement ConnectedFrameworkAgreement { get; set; }

        public IEnumerable<ContractBrokerFeeCompetenceItemModel> ItemsDistanceInterpretationPerCompetence{ get; set; }
        public IEnumerable<ContractPricePerRegionGroupListItemModel> ItemsPerRegionGroup{ get; set; }

        public IEnumerable<ContractBrokerListItemModel> ItemsPerBroker { get; set; }
        public IEnumerable<ContractRegionListItemModel> ItemsPerRegion { get; set; }
    }
}
