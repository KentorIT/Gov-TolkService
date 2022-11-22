using System.Collections.Generic;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class InterpreterGroupAcceptModel
    {
        public bool Accepted { get; set; } = true;
        public string DeclineMessage { get; set; }
        public string CompetenceLevel { get; set; }
        public IEnumerable<RequirementAnswerModel> RequirementAnswers { get; set; }
    }
}
