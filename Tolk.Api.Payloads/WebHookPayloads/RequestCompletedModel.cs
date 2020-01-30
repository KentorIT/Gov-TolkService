namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequestCompletedModel : WebHookPayloadBaseModel
    {
        public string OrderNumber { get; set; }
    }
}