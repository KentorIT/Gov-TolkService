using System.Collections.Generic;
using System.Linq;

namespace Tolk.BusinessLogic.Helpers
{
    public class StatusVerificationResult
    {
        public bool Success => !Items.Any(i => !i.Success);
        public IEnumerable<StatusVerificationItem> Items { get; set; }
    }
}
