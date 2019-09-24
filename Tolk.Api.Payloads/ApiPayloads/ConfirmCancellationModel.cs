using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class ConfirmCancellationModel : ApiPayloadBaseModel
    {
        [Required]
        public string OrderNumber { get; set; }
    }
}
