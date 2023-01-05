using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class ContractListWrapperModel
    {
        public BrokerFeeCalculationType ListType { get; set; }
        public ContractListByRegionAndBrokerModel ContractListByRegionAndBrokerModel { get; set; }
        public ContractListByRegionGroupAndServiceModel ContractListByRegionGroupAndServiceModel { get; set; }
    }
}
