using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Tolk.BusinessLogic.Utilities
{
    public class DisplayPriceInformation
    {

        public List<DisplayPriceRow> DisplayPriceRows { get; set; } = new List<DisplayPriceRow>();

        public decimal TotalPrice
        {
            get
            {
                return DisplayPriceRows.Sum(p => p.Price);
            }
        }
    }
}
