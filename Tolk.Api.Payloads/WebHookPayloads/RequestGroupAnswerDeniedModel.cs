namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequestGroupAnswerDeniedModel : WebHookPayloadBaseModel
    {
        public string OrderGroupNumber { get; set; }

        public string Message { get; set; }
    }
}


