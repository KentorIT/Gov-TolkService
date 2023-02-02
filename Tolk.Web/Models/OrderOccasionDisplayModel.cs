using System;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class OrderOccasionDisplayModel : OrderOccasionModel
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
            MealBreakIncluded = occasion.MealBreakIncluded;
        }

        public int ExtraInterpreterFor { get; set; }

        public string OrderNumber { get; set; }

        public bool DisplayDetails { get; set; } = false;

        public OrderStatus OrderStatus { get; set; } = OrderStatus.ResponseAccepted;

        public RequestStatus? RequestStatus { get; set; } = null;

        public int RouteId { get; set; }

        public string ControllerName { get; set; }

        public string ColorClassName => RequestStatus.HasValue ? CssClassHelper.GetColorClassNameForRequestStatus(RequestStatus.Value) : CssClassHelper.GetColorClassNameForOrderStatus(OrderStatus);

        public string StatusName => RequestStatus.HasValue ? RequestStatus.Value.GetDescription() : OrderStatus.GetDescription();

        public string Information => $"{OccasionStartDateTime.ToSwedishString("yyyy-MM-dd")} {OccasionStartDateTime.ToSwedishString("HH\\:mm")}-{OccasionEndDateTime.ToSwedishString("HH\\:mm")}";
        
        public TimeSpan Duration => OccasionEndDateTime - OccasionStartDateTime;

        public PriceInformationModel PriceInformationModel { get; set; }

        public string MealBreakTextToDisplay => ((int)(OccasionEndDateTime - OccasionStartDateTime).TotalMinutes > 300) ? MealBreakIncluded.HasValue ? MealBreakIncluded.Value ? "Måltidspaus beräknas ingå" : "Måltidspaus beräknas inte ingå" : "Ej angivet om måltidspaus beräknas ingå" : null;

        internal static OrderOccasionDisplayModel GetModelFromOrder(Order order, PriceInformationModel priceInformationModel = null, Request request = null)
        {
            return new OrderOccasionDisplayModel
            {
                OrderNumber = order.OrderNumber,
                OccasionStartDateTime = order.StartAt.DateTime,
                OccasionEndDateTime = order.EndAt.DateTime,
                ExtraInterpreter = order.IsExtraInterpreterForOrderId.HasValue,
                PriceInformationModel = priceInformationModel,
                RouteId = request == null ? order.OrderId : request.RequestId,
                ControllerName = request == null ? "Order" : "Request",
                OrderStatus = order.Status,
                RequestStatus = request?.Status,
                MealBreakIncluded = order.MealBreakIncluded
            };
        }
    }
}
