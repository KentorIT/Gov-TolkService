using System;

namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class OccasionModel
    {
        public string OrderNumber { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public PriceInformationModel PriceInformation { get; set; }
        public string IsExtraInterpreterForOrderNumber { get; set; }
    }
}
