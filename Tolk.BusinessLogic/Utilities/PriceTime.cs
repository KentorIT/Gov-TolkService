using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Entities;

namespace Tolk.BusinessLogic.Utilities
{

    public class PriceTime
    {
        public int Minutes { get; set; }

        public PriceRowType PriceRowType { get; set; }
    }
}
