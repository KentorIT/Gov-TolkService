using System.Collections.Generic;

namespace Tolk.BusinessLogic.Helpers
{
    public class TellusInterpreterResponse
    {
        public int Status { get; set; }
        public int TotalMatching { get; set; }
        public IEnumerable<TellusInterpreterModel> Result { get; set; }
    }
}
