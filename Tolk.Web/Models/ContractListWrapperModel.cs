using System.Collections.Generic;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class ContractListWrapperModel
    {
        public BrokerFeeCalculationType ListType { get; set; }
        public ContractListByRegionAndBrokerModel ContractListByRegionAndBrokerModel { get; set; }
        public ContractListByRegionGroupAndServiceModel ContractListByRegionGroupAndServiceModel { get; set; }
        public IEnumerable<FrameworkAgreementNumberIdModel> FrameworkAgreementList { get; set; }
    }
}
