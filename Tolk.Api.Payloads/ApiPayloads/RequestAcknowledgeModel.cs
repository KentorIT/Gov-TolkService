using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class RequestAcknowledgeModel : ApiPayloadBaseModel
    {
        [Required]
        public string OrderNumber { get; set; }
    }
}
