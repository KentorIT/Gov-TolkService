namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class OrderAcceptedModel : WebHookPayloadBaseModel
    {
        public string OrderNumber { get; set; }
        public string BrokerKey { get; set; }
        // Vilken tolk (med info om rang)
        // resulterande priser och det
    }
    public class OrderAnsweredModel : WebHookPayloadBaseModel
    {
        public string OrderNumber { get; set; }
        public string BrokerKey { get; set; }
        // Vilken tolk (med info om rang)
        // resulterande priser och det
    }
    public class OrderDeclinedModel : WebHookPayloadBaseModel
    {
        public string OrderNumber { get; set; }
        public string Message { get; set; }
        public string BrokerKey { get; set; }
    }
}
