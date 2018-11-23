using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class RequestAssignModel : ApiPayloadBaseModel
    {
        public string OrderNumber { get; set; }
        public string Interpreter { get; set; }
        public string Location { get; set; }
        public string CompetenceLevel { get; set; }
        public decimal? ExpectedTravelCosts { get; set; }
        //Files
        //RequirementAnswers
    }
}
