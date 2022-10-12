using System.ComponentModel;


namespace Tolk.BusinessLogic.Enums
{
    public enum FrameworkAgreementResponseRuleset
    {
        [Description("Första versionen avtalet")]
        VersionOne = 1,
        [Description("Andra versionen av avtalet, med andra svarsregler")]
        VersionTwo = 2
    }
}
