using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum RequiredAnswerLevel
    {
        [CustomName("request_requires_interpreter")]
        [Description("Svar med tillsättning av tolk efterfrågad")]
        Full = 1,
        [CustomName("request_requires_accept")]
        [Description("Svar behöver innehålla bekräftelse, men inte tillsatt tolk")]
        Acceptance = 2,
        [CustomName("request_is_replacement")]
        [Description("Begäran av bekräftelse på ersättningsuppdrag")]
        AcceptReplacement = 3,
        [CustomName("no_action_needed")]
        [Description("Förfrågan är inte i ett tillstånd som kräver handling från förmedling")]
        None = 4
    }
}
