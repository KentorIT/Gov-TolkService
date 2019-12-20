using System.Collections.Generic;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Helpers
{
    public class InterpreterAnswerDto
    {
        public bool Accepted { get; set; } = true;
        public string DeclineMessage { get; set; }
        public InterpreterBroker Interpreter { get; set; }
        public CompetenceAndSpecialistLevel CompetenceLevel { get; set; }
        public IEnumerable<OrderRequirementRequestAnswer> RequirementAnswers { get; set; }
        public decimal? ExpectedTravelCosts { get; set; }
        public string ExpectedTravelCostInfo { get; set; }
    }
}
