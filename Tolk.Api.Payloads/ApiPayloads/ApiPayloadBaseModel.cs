using System;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class ApiPayloadBaseModel
    {
        public string CallingUser { get; set; }
    }
}
