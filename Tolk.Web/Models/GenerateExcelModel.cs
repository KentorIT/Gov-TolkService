using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class GenerateExcelModel
    {
        public ReportType SelectedReportType { get; set; }

        public string StartDate { get; set; }

        public string EndDate { get; set; }
    }
}
