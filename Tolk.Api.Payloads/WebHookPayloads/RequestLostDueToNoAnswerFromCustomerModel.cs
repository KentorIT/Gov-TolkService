namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequestLostDueToNoAnswerFromCustomerModel : WebHookPayloadBaseModel
    {
        public string OrderNumber { get; set; }
    }
}
