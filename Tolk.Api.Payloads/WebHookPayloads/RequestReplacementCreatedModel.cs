namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequestReplacementCreatedModel : WebHookPayloadBaseModel
    {
        public RequestModel OriginalRequest { get; set; }
        public RequestModel ReplacementRequest { get; set; }
    }
}
