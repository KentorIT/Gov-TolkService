using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class BrokerFeePerServiceTypeModel : BrokerRankModel
    {
        public IEnumerable<ContractBrokerFeeCompetenceItemModel> OnSiteBrokerFeesPerCompetence { get; set; }
        public IEnumerable<ContractBrokerFeeCompetenceItemModel> DistanceBrokerFeesPerCompetence { get; set; }
    }
}
