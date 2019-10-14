﻿using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class OrderOccasionDisplayModel: OrderOccasionModel
    {
        public OrderOccasionDisplayModel()
        {
        }

        internal OrderOccasionDisplayModel(OrderOccasionModel occasion)
        {
            OrderOccasionId = occasion.OrderOccasionId;
            OccasionStartDateTime = occasion.OccasionStartDateTime;
            OccasionEndDateTime = occasion.OccasionEndDateTime;
            ExtraInterpreter = occasion.ExtraInterpreter;
        }

        public int ExtraInterpreterFor { get; set; }

        public string OrderNumber { get; set; }

        public string Information => $"{OccasionStartDateTime.ToSwedishString("yyyy-MM-dd")} {OccasionStartDateTime.ToSwedishString("HH\\:mm")} - {OccasionEndDateTime.ToSwedishString("HH\\:mm")}";

        public PriceInformationModel PriceInformationModel { get; set; }

        internal static OrderOccasionDisplayModel GetModelFromOrder(Order order, PriceInformationModel priceInformationModel)
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
