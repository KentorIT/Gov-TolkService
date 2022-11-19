using System.Collections.Generic;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Helpers
{
    public class InterpreterAcceptDto
    {
        public bool Accepted { get; set; } = true;
        public string DeclineMessage { get; set; }
        public CompetenceAndSpecialistLevel? CompetenceLevel { get; set; }
        public IEnumerable<OrderRequirementRequestAnswer> RequirementAnswers { get; set; }
    }
}
