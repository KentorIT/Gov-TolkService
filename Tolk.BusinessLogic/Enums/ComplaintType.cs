using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum ComplaintType
    {
        [CustomName("late_interpreter")]
        [Description("Försening. Tolk försenad mer än 10 min. Uppdraget genomfördes")]
        LateInterpreter = 1,
        [CustomName("too_late_interpreter")]
        [Description("Försening. Tolk försenad mer än 15 min eller uteblev helt. Uppdraget genomfördes inte")]
        TooLateInterpreter = 2,
        [CustomName("no_delivery")]
        [Description("Fel i tjänsten. Uppdraget genomfördes inte")]
        NoDelivery = 3,
        [CustomName("bad_delivery")]
        [Description("Fel i tjänsten. Uppdraget genomfördes, men inte i enlighet med Kammarkollegiets föreskrifter eller god tolksed")]
        BadDelivery = 4,
    }
}
