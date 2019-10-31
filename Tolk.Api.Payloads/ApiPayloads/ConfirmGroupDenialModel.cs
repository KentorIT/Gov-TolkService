using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class ConfirmGroupDenialModel : ApiPayloadBaseModel
    {
        [Required]
        public string OrderGroupNumber { get; set; }
    }
}
