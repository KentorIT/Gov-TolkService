using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class BrokerFeeByRankingModel : BrokerRankModel
    {
        public decimal BrokerFeePercentage { get; set; }
        public string BrokerFeePercentageDisplay => BrokerFeePercentage.ToString("P");        
        public IEnumerable<string> BrokerFeesPerCompetenceLevel { get; set; }

        public IEnumerable<string> CompetenceDescriptions { get; set; }

    }
}
