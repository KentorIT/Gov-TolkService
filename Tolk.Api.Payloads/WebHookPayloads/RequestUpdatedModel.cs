using System.Collections.Generic;


namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequestUpdatedModel : WebHookPayloadBaseModel
    {
        public string OrderNumber { get; set; }
        public CustomerInformationModel CustomerInformation { get; set; }
        public LocationModel Location { get; set; }
        public string Description { get; set; }
        public IEnumerable<AttachmentInformationModel> Attachments { get; set; }
    }
}


