using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Utilities
{
    public class ReportComplaintHelperModel : ReportBaseHelperModel
    {
        public int ComplaintId { get; set; }

        public ComplaintStatus ComplaintStatus { get; set; }

        public ComplaintType ComplaintType { get; set; }

    }
}
