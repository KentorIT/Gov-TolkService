
namespace Tolk.BusinessLogic.Utilities
{
    public class ReportRequisitionRow : ReportRow
    {
        public bool HasMealbreaks { get; set; }

        public int WaisteTime { get; set; }

        public int WaisteTimeIWH { get; set; }

        public decimal Outlay { get; set; }

        public int CarCompensation { get; set; }

        public string PerDiem { get; set; }

        public string TaxCard { get; set; }

        public decimal PreliminaryCost { get; set; }
    }
}
