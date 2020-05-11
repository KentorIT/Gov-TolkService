using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Utilities
{
    public class ReportRequirementModel
    {
        public int OrderId { get; set; }

        public int OrderRequirementId { get; set; }

        public RequirementType RequirementType { get; set; }

        public bool IsRequired { get; set; }

        public string Description { get; set; }

    }
}
