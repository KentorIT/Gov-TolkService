using System.Collections.Generic;

namespace Tolk.BusinessLogic.Helpers
{
    public class TellusResponse
    {
        public IEnumerable<TellusInterpreterModel> Result { get; set; }
        public int Status { get; set; }
        public int TotalMatching { get; set; }
    }
}
