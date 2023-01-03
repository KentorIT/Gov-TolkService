using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum RequestAnswerRuleType
    {
        [CustomName("more_than_twenty_workdays_before_occasion")]
        [Description("Förfrågan inkom mer än tjugo dagar innan tillfället. Bekräftelse krävs inom fyra helgfira vardagar, och tolk måste tillsättas mer än sju helgfira vardagar innan tillfällets start. Om förfrågan skapas på en helgdag så anses förfrågan ha skapas direkt efter midnatt första följande helgfria vardag.")]
        [Parent(RequiredAnswerLevel.Acceptance)]
        RequestCreatedMoreThanTwentyDaysBefore = 1,

        [CustomName("more_than_ten_workdays_before_occasion")]
        [Description("Förfrågan inkom mer än tjugo dagar innan tillfället. Bekräftelse krävs inom fyra helgfira vardagar, och tolk måste tillsättas mer än sju helgfira vardagar innan tillfällets start. Om förfrågan skapas på en helgdag så anses förfrågan ha skapas direkt efter midnatt första följande helgfria vardag.")]
        [Parent(RequiredAnswerLevel.Acceptance)]
        RequestCreatedMoreThanTenDaysBefore = 2,

        [CustomName("answer_required_next_day")]
        [Description("Förfrågan behöver tillsättas senast 15:00 nästa helgfira vardag efter förfrågan skapades. Om förfrågan skapas på en helgdag så anses förfrågan ha skapas direkt efter midnatt första följande helgfria vardag.")]
        [Parent(RequiredAnswerLevel.Full)]
        AnswerRequiredNextDay = 3,

        [CustomName("answer_required_same_day")]
        [Description("Förfrågan skapades innan 14:00 dagen innan tillfället. Förfrågan behöver tillsättas senast 16:30 samma helgfira vardag efter förfrågan skapades. Om förfrågan skapas på en helgdag så anses förfrågan ha skapas direkt efter midnatt första följande helgfria vardag.")]
        [Parent(RequiredAnswerLevel.Full)]
        RequestCreatedOneDayBefore = 4,

        [CustomName("answer_time_set_by_customer")]
        [Description("Förfrågan skapades efter 14:00 dagen innan tillfället eller samma dag. Tiden som är satt för svar är manuellt satt av användaren som skapade förfrågan. Om förfrågan skapas på en helgdag så anses förfrågan ha skapas direkt efter midnatt första följande helgfria vardag.")]
        [Parent(RequiredAnswerLevel.Full)]
        ResponseSetByCustomer = 5,

        [CustomName("accept_replacement")]
        [Description("Förfrågan gäller ett ersättningsuppdrag som avbokats av användare mindre än 48 timmar (två fulla helgfira vardagar), och behöver besvaras innan uppdraget startar.")]
        [Parent(RequiredAnswerLevel.Full)]
        ReplacedOrder = 6,

        [CustomName("not_used_in_api", useInApi: false)]
        [Parent(RequiredAnswerLevel.None)]
        ReplacedInterpreter = 7
    }
}
