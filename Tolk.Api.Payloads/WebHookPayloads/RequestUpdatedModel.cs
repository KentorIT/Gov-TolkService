using System.Collections.Generic;


namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequestUpdatedModel : WebHookPayloadBaseModel
    {
        public string OrderNumber { get; set; }
        public string RequestUpdateType { get; set; }
        public CustomerInformationUpdatedModel CustomerInformationUpdated { get; set; }
        public LocationUpdatedModel LocationUpdated { get; set; }
        public string Description { get; set; }
        public IEnumerable<AttachmentInformationModel> Attachments { get; set; }
    }
}


