using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Utilities
{
    public class ReportRequisitionHelperModel : ReportBaseHelperModel
    {
        public int RequisitionId { get; set; }

        public RequisitionStatus RequisitionStatus { get; set; }

        public int WaisteTime { get; set; }

        public int WaisteTimeIWH { get; set; }

        public int CarCompensation { get; set; }

        public string PerDiem { get; set; }

        public TaxCardType? TaxCard { get; set; }

    }
}
