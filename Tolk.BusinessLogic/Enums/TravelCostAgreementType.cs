using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum TravelCostAgreementType
    {
        [CustomName("lokalt_reseavtal")]
        [Description("Domstolarnas lokala reseavtal")]
        LocalAgreeement = 1,
        [CustomName("villkorsavtal")]
        [Description("Villkorsavtalen enligt skatteverket")]
        CentralAgreement = 2
    }
}
