namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequestGroupLostDueToNoAnswerFromCustomerModel : WebHookPayloadBaseModel
    {
        public string OrderGroupNumber { get; set; }
    }
}
