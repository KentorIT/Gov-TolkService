using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class RequestGroupAnswerModel : ApiPayloadBaseModel
    {
        public string OrderGroupNumber { get; set; }
        public InterpreterModel Interpreter { get; set; }
        public InterpreterModel ExtraInterpreter { get; set; }
        public string Location { get; set; }
        public string CompetenceLevel { get; set; }
        public string ExtraInterpreterCompetenceLevel { get; set; }
        public decimal? ExpectedTravelCosts { get; set; }
        public string ExpectedTravelCostInfo { get; set; }
        public IEnumerable<RequirementAnswerModel> RequirementAnswers { get; set; }
        public IEnumerable<OccasionAnswerModel> OccasionAnswers { get; set; }
        //Files
    }
}
