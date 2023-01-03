using System;

namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequestModel : RequestBaseModel
    {
        public string OrderNumber { get; set; }
        public string BrokerReferenceNumber { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public bool? MealBreakIncluded { get; set; }
        public PriceInformationModel PriceInformation { get; set; }
    }
}


