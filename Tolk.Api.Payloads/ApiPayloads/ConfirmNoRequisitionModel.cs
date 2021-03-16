using NJsonSchema.Annotations;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    [JsonSchemaFlatten]
    public class ConfirmNoRequisitionModel : ApiPayloadBaseModel
    {
        [Required]
        public string OrderNumber { get; set; }
    }
}
