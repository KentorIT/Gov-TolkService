using System;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Utilities
{
    public class RequestExpiryResponse
    {
        public DateTimeOffset? LastAcceptedAt { get; set; }
        public DateTimeOffset? ExpiryAt { get; set; }
        public RequestAnswerRuleType RequestAnswerRuleType { get; set; }
    }
}
