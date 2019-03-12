namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequisitionCommentedModel : WebHookPayloadBaseModel
    {
        public string OrderNumber { get; set; }
        public string Message { get; set; }
    }
}
