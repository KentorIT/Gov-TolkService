namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class ComplaintMessageModel : WebHookPayloadBaseModel
    {
        public string OrderNumber { get; set; }

        public string ComplaintType { get; set; }

        public string Message { get; set; }
    }
}
