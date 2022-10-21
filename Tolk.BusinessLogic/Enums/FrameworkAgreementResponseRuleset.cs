using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum FrameworkAgreementResponseRuleset
    {
        [Description("Första versionen avtalet")]
        [ContractDefinition(RankingRules = "För detta ramavtalsområde gäller en fastställd rangordning per län. En fast rangordning innebär att avropsförfrågan ska ställas till den först rangordnade leverantören (tolkförmedlingen) inom det aktuella länet.")]
        VersionOne = 1,
        [Description("Andra versionen av avtalet, med andra svarsregler")]
        [ContractDefinition(RankingRules = "För detta ramavtalsområde gäller Något helt annat")]
        VersionTwo = 2
    }
}
