using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class ComplaintDisputeModel : ApiPayloadBaseModel
    {
        [Required]
        public string OrderNumber { get; set; }
        [Required]
        public string Message { get; set; }
    }
}
