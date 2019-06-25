using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Models
{
    public class OrderGroupSummaryModel
    {
        public string OrderGroupNumber { get; internal set; }

        public IEnumerable<OrderOccasionDisplayModel> OrderOccasionDisplayModels { get; internal set; }

        public decimal TotalPrice
        {
            get => OrderOccasionDisplayModels?.Sum(o => o.PriceInformationModel.TotalPriceToDisplay) ?? 0;
        }
    }
}
