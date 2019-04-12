using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class OrderListItemModel
    {
        public string Action { get; set; }

        public int OrderId { get; set; }

        public OrderStatus Status { get; set; }

        public string CreatorName { get; set; }

        public string BrokerName { get; set; }

        public string CustomerName { get; set; }

        public string OrderNumber { get; set; }

        public string RegionName { get; set; }

        public string Language { get; set; }

        [NoDisplayName]
        public virtual TimeRange OrderDateAndTime { get; set; }

        public string ColorClassName { get => CssClassHelper.GetColorClassNameForOrderStatus(Status); }
    }
}
