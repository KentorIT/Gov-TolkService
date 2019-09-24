using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class ConfirmDenialModel : ApiPayloadBaseModel
    {
        [Required]
        public string OrderNumber { get; set; }
    }
}
