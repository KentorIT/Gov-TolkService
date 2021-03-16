using NJsonSchema.Annotations;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    [JsonSchemaFlatten]
    public class ConfirmGroupDenialModel : ApiPayloadBaseModel
    {
        [Required]
        public string OrderGroupNumber { get; set; }
    }
}
