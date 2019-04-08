using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Utilities
{
    public class WeeklyStatisticsModel
    {
        public int NoOfItems { get; set; }

        public decimal DiffPercentage { get; set; }

        public StatisticsChangeType ChangeType { get; set; }

        public string Name { get; set; }
    }
}
