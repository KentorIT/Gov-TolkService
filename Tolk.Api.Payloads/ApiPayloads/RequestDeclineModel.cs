using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class RequestDeclineModel : ApiPayloadBaseModel
    {
        [Required]
        public string OrderNumber { get; set; }
        public string Message { get; set; }
    }
}
