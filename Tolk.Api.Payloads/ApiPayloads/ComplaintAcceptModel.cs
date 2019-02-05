using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class ComplaintAcceptModel : ApiPayloadBaseModel
    {
        [Required]
        public string OrderNumber { get; set; }
    }
}
