using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    public enum CustomerChangeLogType
    {
        [Description("Inställningar")]
        Settings = 1,
        [Description("Kundspecifika Fält")]
        CustomerSpecificProperty = 2,
        [Description("Förmedlingsspecifik inställning för Order Agreement")]
        CustomerOrderAgreementBrokerSettings = 3
    }
}