namespace Tolk.BusinessLogic.Utilities
{
    public class ReportOrderRow : ReportRow
    {

        public string Dialect { get; set; }

        public bool DialectIsRequirement { get; set; }

        public string DialectIsRequirementAsString { get; set; }

        public bool FulfilledDialectRequirement { get; set; }

        public string FulfilledDialectRequirementAsString { get; set; }

        public string OrderedInterpreterLocation1 { get; set; }

        public string OrderedInterpreterLocation2 { get; set; }

        public string OrderedInterpreterLocation3 { get; set; }

        public string CompetenceLevelDesired1 { get; set; }

        public string CompetenceLevelDesired2 { get; set; }

        public string CompetenceLevelRequired1 { get; set; }

        public string CompetenceLevelRequired2 { get; set; }

        public int OrderRequirements { get; set; }

        public int OrderDesiredRequirements { get; set; }

        public int FulfilledOrderDesiredRequirements { get; set; }

        public int FulfilledOrderRequirements { get; set; }

    }
}
