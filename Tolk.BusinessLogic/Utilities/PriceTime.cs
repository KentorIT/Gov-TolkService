using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Entities;

namespace Tolk.BusinessLogic.Utilities
{

    public class PriceTime
    {
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public int Minutes
        {
            get
            {
                TimeSpan totalMinutes = EndAt - StartAt;
                return (int)totalMinutes.TotalMinutes;
            }
        }

        public bool IsBrokerFee { get; set; } = false;
        public PriceRowType PriceRowType { get; set; }

        public int PriceListRowId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice
        {
            get
            {
                return Price * Quantity;
            }
        }
    }
}
