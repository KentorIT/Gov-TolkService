using NJsonSchema.Annotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    [JsonSchemaFlatten]
    [Description("Hämta senaste rekvisition för ett visst avrop")]
    public class RequisitionGetDetailsModel : ApiPayloadBaseModel
    {
        [Required]
        [Description("Avrop man vill hämta rekvisition för")]
        public string OrderNumber { get; set; }

        [Description("Flagga för om man vill ha med tidigare rekvisitoner, eller bara den aktuella")]
        public bool IncludePreviousRequisitions { get; set; }
    }
}
