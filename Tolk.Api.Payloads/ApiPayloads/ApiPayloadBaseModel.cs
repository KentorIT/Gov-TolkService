using System;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class ApiPayloadBaseModel
    {
        [Required]
        public string CallingUser { get; set; }
    }
}
