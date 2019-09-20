using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class RequisitionListItemModel
    {
        [ColumnDefinitions(IsIdColumn = true, Index = 0, Name = nameof(OrderRequestId), Visible = false)]
        public int OrderRequestId { get; set; }

        [ColumnDefinitions(Index = 1, Name = nameof(OrderNumber), Title = "BokningsID")]
        public string OrderNumber { get; set; }

        [ColumnDefinitions(Index = 2, Name = nameof(StatusName), Title = "Status")]
        public string StatusName => Status.GetDescription();

        [ColumnDefinitions(Index = 3, Name = nameof(Language), Title = "Språk")]
        public string Language { get; set; }

        [ColumnDefinitions(Index = 4, Name = nameof(OrderDateAndTime), Title = "Datum för uppdrag")]
        public string OrderDateAndTime { get; set; }

        [ColumnDefinitions(Index = 5, Name = nameof(CustomerName), Title = "Myndighet", Visible = false)]
        public string CustomerName { get; set; }

        [ColumnDefinitions(Index = 6, Name = nameof(BrokerName), Title = "Förmedling", Visible = false)]
        public string BrokerName { get; set; }

        [ColumnDefinitions(IsLeftCssClassName = true, Name = nameof(ColorClassName), Visible = false)]
        public string ColorClassName { get => CssClassHelper.GetColorClassNameForRequisitionStatus(Status); }

        public RequisitionStatus Status { get; set; }
    }
}
