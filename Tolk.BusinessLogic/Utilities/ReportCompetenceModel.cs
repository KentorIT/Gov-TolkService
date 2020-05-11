using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Utilities
{
    public  class ReportCompetenceModel
    {
        public int OrderId { get; set; }

        public CompetenceAndSpecialistLevel CompetenceLevel { get; set; }

        public int? Rank { get; set; }

    }
}
