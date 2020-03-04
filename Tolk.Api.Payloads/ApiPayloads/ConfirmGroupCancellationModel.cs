using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads 
{
    public class ConfirmGroupCancellationModel : ApiPayloadBaseModel
    {
        [Required]
        public string OrderGroupNumber { get; set; }
    }
}
