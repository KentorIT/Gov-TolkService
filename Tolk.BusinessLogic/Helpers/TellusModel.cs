using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Helpers
{
    public class TellusModel
    {
        public int Status { get; set; }
        public int? TotalMatching { get; set; }
        public List<ITellusResultModel> Result { get; set; }
    }
}
