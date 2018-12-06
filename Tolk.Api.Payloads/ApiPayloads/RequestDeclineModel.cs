using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class RequestDeclineModel : ApiPayloadBaseModel
    {
        [Required]
        public string OrderNumber { get; set; }
        [Required]
        public string Message { get; set; }
    }
}
