using NJsonSchema.Annotations;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    [JsonSchemaFlatten]
    public class RequestGroupDeclineModel : ApiPayloadBaseModel
    {
        [Required]
        public string OrderGroupNumber { get; set; }
        [Required]
        public string Message { get; set; }
    }
}
