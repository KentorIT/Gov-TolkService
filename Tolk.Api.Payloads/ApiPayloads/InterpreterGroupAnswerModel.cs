using System.Collections.Generic;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class InterpreterGroupAnswerModel
    {
        public bool Accepted { get; set; }
        public string DeclineMessage { get; set; }
        public InterpreterModel Interpreter { get; set; }
        public string Location { get; set; }
        public string CompetenceLevel { get; set; }
        public decimal? ExpectedTravelCosts { get; set; }
        public string ExpectedTravelCostInfo { get; set; }
        public IEnumerable<RequirementAnswerModel> RequirementAnswers { get; set; }

        public bool IsValid => Accepted ?
            Interpreter != null && !string.IsNullOrEmpty(Location) && !string.IsNullOrEmpty(CompetenceLevel) :
            !string.IsNullOrEmpty(DeclineMessage);
    }
}
