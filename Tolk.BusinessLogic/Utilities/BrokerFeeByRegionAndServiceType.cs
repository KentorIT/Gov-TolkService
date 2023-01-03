using System;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Utilities
{
    [Serializable]
    public class BrokerFeeByRegionAndServiceType
    {
        public DateTimeOffset StartDate { get; set; }

        public DateTimeOffset EndDate { get; set; }

        public CompetenceLevel CompetenceLevel { get; set; }

        public InterpreterLocation InterpreterLocation { get; set; }

        public int RegionId { get; set; }

        public decimal BrokerFee { get; set; }
    }
}
