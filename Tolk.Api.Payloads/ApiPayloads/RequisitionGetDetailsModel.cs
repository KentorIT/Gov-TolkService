using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class RequisitionGetDetailsModel : ApiPayloadBaseModel
    {
        [Required]
        public string OrderNumber { get; set; }

        public bool IncludePreviousRequisitions { get; set; }
    }
}
