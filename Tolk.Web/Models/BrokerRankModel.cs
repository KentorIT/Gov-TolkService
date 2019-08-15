using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class BrokerRankModel
    {
        public string BrokerName { get; set; }

        public int Rank { get; set; }

        public decimal BrokerFeePercentage { get; set; }

        public List<string> BrokerFeesPerCompetenceLevel { get; set; }

        public List<string> CompetenceDescriptions { get; set; }

        public string RegionName { get; set; }

    }
}
