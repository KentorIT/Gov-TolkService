using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum FrameworkAgreementResponseRuleset
    {


        [Description("Första versionen avtalet")]
        [ContractDefinition(RankingRules = "För detta ramavtalsområde gäller en fastställd rangordning per län. En fast rangordning innebär att avropsförfrågan ska ställas till den först rangordnade leverantören (tolkförmedlingen) inom det aktuella länet.",
            ReplacementError = "Uppdraget måste ske inom tiden för det ersatta uppdraget", TravelConditionKilometers = "100", TravelConditionHours = "2")]
        VersionOne = 1,

        [Description("Andra versionen av avtalet, med andra svarsregler")]
        [ContractDefinition(RankingRules = "För detta ramavtalsområde gäller Något helt annat", ReplacementError = "Uppdraget måste överlappa med minst fem minuter för det ersatta uppdraget, och får inte vara längre",
            TravelConditionKilometers = "250", TravelConditionHours = "3")]
        VersionTwo = 2
    }
}
