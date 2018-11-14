
using System;


namespace Tolk.Api.Payloads
{

    public class RequestModel : PayloadModel
    {
        public DateTimeOffset CreatedAt { get; set; }
        public string OrderNumber { get; set; }
        public string Customer { get; set; }
        public string Region { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public string Language { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }

    }

    public class RequestAssignModel : PayloadModel
    {
        public string Handler { get; set; }
        public string OrderNumber { get; set; }
        public string Interpreter { get; set; }
        public string Location { get; set; }
        public string CompetenceLevel { get; set; }
        public decimal? ExpectedTravelCosts { get; set; }
        //Files
        //RequirementAnswers
    }
}
