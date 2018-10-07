using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Entities;

namespace Tolk.BusinessLogic.Utilities
{

    public class PriceRow : PriceRowBase
    {

        public int Minutes
        {
            get
            {
                TimeSpan totalMinutes = EndAt - StartAt;
                return (int)totalMinutes.TotalMinutes;
            }
        }

        public decimal TotalPrice
        {
            get
            {
                return Price * Quantity;
            }
        }
    }
}
