
namespace Tolk.BusinessLogic.Utilities
{
    public class ReportRequirementAnswerModel
    {
        public int RequestId { get; set; }

        public int OrderRequirementId { get; set; }

        public bool CanSatisfyRequirement { get; set; }
    }
}
