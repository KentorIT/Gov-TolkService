namespace Tolk.Api.Payloads.ApiPayloads
{
    public class DenyAnswerModel : ApiPayloadBaseModel
    {
        public string OrderNumber { get; set; }
        public string Message { get; set; }
        public string BrokerIdentifier { get; set; }
    }
}
