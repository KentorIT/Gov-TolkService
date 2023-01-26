using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum FrameworkAgreementResponseRuleset
    {


        [Description("Första versionen avtalet")]
        [ContractDefinition(ReplacementError = "Uppdraget måste ske inom tiden för det ersatta uppdraget",
            TravelConditionKilometers = "100", TravelConditionHours = "2")]
        VersionOne = 1,

        [Description("Andra versionen av avtalet, med andra svarsregler")]
        [ContractDefinition(ReplacementError = "Uppdraget måste överlappa med minst fem minuter för det ersatta uppdraget, och får inte vara längre",
            TravelConditionKilometers = "250", TravelConditionHours = "3")]
        VersionTwo = 2
    }
}
