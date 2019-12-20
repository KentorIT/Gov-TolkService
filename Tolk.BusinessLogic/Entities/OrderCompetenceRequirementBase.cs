using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderCompetenceRequirementBase
    {
        public CompetenceAndSpecialistLevel CompetenceLevel { get; set; }

        public int? Rank { get; set; }
    }
}
