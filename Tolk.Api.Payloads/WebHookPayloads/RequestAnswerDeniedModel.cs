namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequestAnswerDeniedModel : WebHookPayloadBaseModel
    {
        public string OrderNumber { get; set; }

        public string Message { get; set; }
    }
}


