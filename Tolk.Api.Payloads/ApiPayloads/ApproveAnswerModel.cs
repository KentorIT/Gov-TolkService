namespace Tolk.Api.Payloads.ApiPayloads
{
    public class ApproveAnswerModel : ApiPayloadBaseModel
    {
        public string OrderNumber { get; set; }
        public string BrokerIdentifier { get; set; }
    }
}
