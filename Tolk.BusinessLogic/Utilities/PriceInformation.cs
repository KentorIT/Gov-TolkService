using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Entities;

namespace Tolk.BusinessLogic.Utilities
{

    public class PriceInformation
    {
        public List<PriceTime> PriceRows { get; set; }

        public DateTimeOffset StartAt { get; set; }

        public DateTimeOffset EndAt { get; set; }

        public decimal TotalPrice
        {
            get
            {
                return PriceRows.Sum(p => p.TotalPrice);
            }
        }
    }
}
