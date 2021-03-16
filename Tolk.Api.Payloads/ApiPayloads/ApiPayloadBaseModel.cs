using System.ComponentModel;

namespace Tolk.Api.Payloads.ApiPayloads
{
    public class ApiPayloadBaseModel
    {
        [Description("Kopplar handlingen (via e-post eller anv-namn) som utförs till den angivna användaren. Användaren måste finnas i systemet.")]
        public string CallingUser { get; set; }
    }
}
