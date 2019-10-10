using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class OrderListItemModel
    {
        [ColumnDefinitions(IsIdColumn = true, Index = 0, Name = nameof(OrderId), Visible = false)]
        public int OrderId { get; set; }

        [ColumnDefinitions(Index = 1, Name = nameof(OrderNumber), Title = "BokningsID")]
        public string OrderNumber { get; set; }

        [ColumnDefinitions(Index = 2, Name = nameof(StatusName), Title = "Status")]
        public string StatusName => Status.GetDescription();

        [ColumnDefinitions(Index = 3, Name = nameof(Language), Title = "Språk")]
        public string Language { get; set; }

        [ColumnDefinitions(Index = 4, Name = nameof(OrderDateAndTime), ColumnName = "StartAt", SortOnWebServer = false, Title = "Datum för uppdrag")]
        public string OrderDateAndTime { get; set; }

        [ColumnDefinitions(Index = 5, Name = nameof(RegionName), Title = "Län")]
        public string RegionName { get; set; }

        [ColumnDefinitions(Index = 6, Name = nameof(CustomerName), Title = "Myndighet")]
        public string CustomerName { get; set; }

        [ColumnDefinitions(Index = 7, Name = nameof(BrokerName), Title = "Förmedling")]
        public string BrokerName { get; set; }

        [ColumnDefinitions(Index = 8, Name = nameof(CreatorName), Title = "Skapad av")]
        public string CreatorName { get; set; }

        public OrderStatus Status { get; set; }

        [ColumnDefinitions(IsLeftCssClassName = true, Name = nameof(ColorClassName), Visible = false)]
        public string ColorClassName => CssClassHelper.GetColorClassNameForOrderStatus(Status);
    }
}
