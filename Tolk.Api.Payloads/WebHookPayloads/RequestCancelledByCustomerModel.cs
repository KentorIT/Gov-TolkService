namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequestCancelledByCustomerModel : WebHookPayloadBaseModel
    {
        public string OrderNumber { get; set; }

        public string Message { get; set; }
    }
}


