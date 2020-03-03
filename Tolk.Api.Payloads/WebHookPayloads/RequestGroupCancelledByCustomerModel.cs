namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequestGroupCancelledByCustomerModel : WebHookPayloadBaseModel
    {
        public string OrderGroupNumber { get; set; }

        public string Message { get; set; }
    }
}
