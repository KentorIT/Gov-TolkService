using System;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class OrderOccasionDisplayModel: OrderOccasionModel
    {
        public OrderOccasionDisplayModel()
        {
        }

        public OrderOccasionDisplayModel(OrderOccasionModel occasion)
        {
            OrderOccasionId = occasion.OrderOccasionId;
            OccasionStartDateTime = occasion.OccasionStartDateTime;
            OccasionEndDateTime = occasion.OccasionEndDateTime;
            ExtraInterpreter = occasion.ExtraInterpreter;
        }

        public int ExtraInterpreterFor { get; set; }

        public string OrderNumber { get; set; }

        public string Information => $"{OccasionStartDateTime.ToString("yyyy-MM-dd")} {OccasionStartDateTime.ToString("hh\\:mm")} - {OccasionEndDateTime.ToString("hh\\:mm")}";

        public PriceInformationModel PriceInformationModel { get; set; }

        public static OrderOccasionDisplayModel GetModelFromOrder(Order order, PriceInformationModel priceInformationModel)
        {
            return new OrderOccasionDisplayModel
            {
                OrderNumber = order.OrderNumber,
                OccasionStartDateTime = order.StartAt.DateTime,
                OccasionEndDateTime = order.EndAt.DateTime,
                ExtraInterpreter = order.IsExtraInterpreterForOrderId.HasValue,
                PriceInformationModel = priceInformationModel
            };
        }
    }
}
