using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{

    public enum AllowExceedingTravelCost
    {
        [Parent(TrueFalse.Yes)]
        [Description("Ja, och jag vill godkänna bedömd resekostnad i förväg")]
        YesShouldBeApproved = 1,

        [Parent(TrueFalse.Yes)]
        [Description("Ja, men jag behöver inte godkänna bedömd resekostnad i förväg")]
        YesShouldNotBeApproved = 2,

        [Parent(TrueFalse.No)]
        [Description("Nej")]
        No = 3
    }
}
