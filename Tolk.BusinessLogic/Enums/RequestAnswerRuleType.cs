using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum RequestAnswerRuleType
    {
        [Parent(RequiredAnswerLevel.Acceptance)]
        RequestCreatedMoreThanTwentyDaysBefore = 1,
        [Parent(RequiredAnswerLevel.Acceptance)]
        RequestCreatedMoreThanTenDaysBefore = 2,
        [Parent(RequiredAnswerLevel.Full)]
        AnswerRequiredNextDay = 3,
        [Parent(RequiredAnswerLevel.Full)]
        RequestCreatedOneDayBefore = 4,
        [Parent(RequiredAnswerLevel.Full)]
        ResponseSetByCustomer = 5,
        [Parent(RequiredAnswerLevel.AcceptReplacement)]
        ReplacedOrder = 6,
        [Parent(RequiredAnswerLevel.None)]
        ReplacedInterpreter = 7
    }
}
