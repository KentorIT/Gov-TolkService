using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Utilities
{
    public class ReportOrderRow : ReportRow
    {

        public string Dialect { get; set; }

        public bool DialectIsRequirement { get; set; }

        public string OrderedInterpreterLocation1 { get; set; }

        public string OrderedInterpreterLocation2 { get; set; }

        public string OrderedInterpreterLocation3 { get; set; }

    }
}
