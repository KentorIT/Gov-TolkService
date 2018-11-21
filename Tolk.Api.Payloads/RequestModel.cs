
using System;
using System.Collections.Generic;

namespace Tolk.Api.Payloads
{

    public class RequestModel : WebHookPayloadModel
    {
        public DateTimeOffset CreatedAt { get; set; }
        public string OrderNumber { get; set; }
        public string Customer { get; set; }
        public string Region { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public string Language { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public IEnumerable<LocationModel> Locations { get; set; }
        public IEnumerable<CompetenceModel> CompetenceLevels { get; set; }
        public bool CompetenceLevelsAreRequired { get; set; }
        public bool AllowMoreThanTwoHoursTravelTime { get; set; }
        public string Description { get; set; }
        public string AssignentType { get; set; }
        // Files should probably be handled with a flag, and a separate getter for these if needed.
    }

    public class RequestAssignModel : ApiPayloadModel
    {
        public string OrderNumber { get; set; }
        public string Interpreter { get; set; }
        public string Location { get; set; }
        public string CompetenceLevel { get; set; }
        public decimal? ExpectedTravelCosts { get; set; }
        //Files
        //RequirementAnswers
    }

    public class LocationModel
    {
        public string Key { get; set; }
        public int Rank { get; set; }
        public string ContactInformation { get; set; }
    }

    public class CompetenceModel
    {
        public string Key { get; set; }
        public int Rank { get; set; }
    }
}
