namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequestGroupLostDueToInactivityModel : WebHookPayloadBaseModel
    {
        public string OrderGroupNumber { get; set; }
    }
}
