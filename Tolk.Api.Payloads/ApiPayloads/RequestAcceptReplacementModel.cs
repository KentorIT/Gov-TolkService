﻿namespace Tolk.Api.Payloads.ApiPayloads
{
    public class RequestAcceptReplacementModel : ApiPayloadBaseModel
    {
        public string OrderNumber { get; set; }
        public string Location { get; set; }
        public decimal? ExpectedTravelCosts { get; set; }
        public string ExpectedTravelCostInfo { get; set; }
    }
}
