using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    public enum ComplaintType
    {
        [Description("Försening. Tolk försenad mer än 10 min. Uppdraget genomfördes")]
        LateInterpreter = 1,
        [Description("Försening. Tolk försenad mer än 15 min eller uteblev helt. Uppdraget genomfördes inte")]
        TooLateInterpreter = 2,
        [Description("Fel i tjänsten. Uppdraget genomfördes inte")]
        NoDelivery = 3,
        [Description("Fel i tjänsten. Uppdraget genomfördes, men inte i enlighet med Kammarkollegiets föreskrifter eller god tolksed")]
        BadDelivery = 4,
    }
}
