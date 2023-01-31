using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum ComplaintType
    {
        [CustomName("late_interpreter")]
        [Description("Försening. Tolk försenad mer än 10 min. Uppdraget genomfördes")]
        [Parent(FrameworkAgreementResponseRuleset.VersionOne)]
        LateInterpreter = 1,
        [CustomName("too_late_interpreter")]
        [Description("Försening. Tolk försenad mer än 15 min eller uteblev helt. Uppdraget genomfördes inte")]
        [Parent(FrameworkAgreementResponseRuleset.VersionOne)]
        TooLateInterpreter = 2,
        [CustomName("no_delivery")]
        [Description("Fel i tjänsten. Uppdraget genomfördes inte")]
        [Parent(FrameworkAgreementResponseRuleset.VersionOne)]
        NoDelivery = 3,
        [CustomName("bad_delivery")]
        [Description("Fel i tjänsten. Uppdraget genomfördes, men inte i enlighet med Kammarkollegiets föreskrifter eller god tolksed")]
        [Parent(FrameworkAgreementResponseRuleset.VersionOne)]
        BadDelivery = 4,
        [CustomName("late_or_too_late_interpreter")]
        [Description("Sen eller utebliven tolk till uppdrag")]
        [Parent(FrameworkAgreementResponseRuleset.VersionTwo)]
        LateOrTooLateInterpreter = 5,
        [CustomName("bad_quality_delivery")]
        [Description("Kvalitetsbrister i utförd tjänst")]
        [Parent(FrameworkAgreementResponseRuleset.VersionTwo)]
        BadQualityDelivery = 6,
        [CustomName("bad_service_delivery")]
        [Description("Brister i service och bemötande")]
        [Parent(FrameworkAgreementResponseRuleset.VersionTwo)]
        BadServiceDelivery = 7,
        [CustomName("other_deviation")]
        [Description("Annan avvikelse")]
        [Parent(FrameworkAgreementResponseRuleset.VersionTwo)]
        OtherDeviation = 8,
    }
}
