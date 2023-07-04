using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class ContractBrokerFeeCompetenceItemModel
    {
        public string CompetenceDescription { get; set; }
        public string BrokerFee { get; set; }

        public ContractBrokerFeeCompetenceItemModel( BrokerFeeByRegionAndServiceType brokerFeeModel)
        {
            CompetenceDescription = brokerFeeModel.CompetenceLevel.GetShortDescription();
            BrokerFee = brokerFeeModel.BrokerFee.ToSwedishString("#,0.00");
        }
    }
}