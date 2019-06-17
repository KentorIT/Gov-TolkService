using System;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class OrderOccasionDisplayModel: OrderOccasionModel
    {
        public OrderOccasionDisplayModel(OrderOccasionModel occasion)
        {
            OrderOccasionId = occasion.OrderOccasionId;
            OccasionStartDateTime = occasion.OccasionStartDateTime;
            OccasionEndDateTime = occasion.OccasionEndDateTime;
            ExtraInterpreter = occasion.ExtraInterpreter;
        }

        public string Information => $"{OccasionStartDateTime.ToString("yyyy-MM-dd")} {OccasionStartDateTime.ToString("hh\\:mm")} - {OccasionEndDateTime.ToString("hh\\:mm")}";

        public PriceInformationModel PriceInformationModel { get; set; }
    }
}
