using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class ComplaintListItemModel
    {
        [ColumnDefinitions(IsIdColumn = true, Index = 0, Name = nameof(OrderRequestId), Visible = false)]
        public int OrderRequestId { get; set; }

        [ColumnDefinitions(Index = 1, Name = nameof(OrderNumber), Title = "BokningsID")]
        public string OrderNumber { get; set; }

        [ColumnDefinitions(Index = 2, Name = nameof(CreatedAt), Title = "Skapad")]
        public string CreatedAt { get; set; }

        [ColumnDefinitions(Index = 3, Name = nameof(TypeName), Title = "Typ av reklamation")]
        public string TypeName => ComplaintType.GetDescription();
        
        [ColumnDefinitions(Index = 4, Name = nameof(RegionName), Title = "Län")]
        public string RegionName { get; set; }

        [ColumnDefinitions(Index = 5, Name = nameof(StatusName), Title = "Status")]
        public string StatusName => Status.GetDescription();

        [ColumnDefinitions(Index = 6, Name = nameof(CustomerName), Title = "Myndighet", Visible = false)]
        public string CustomerName { get; set; }

        [ColumnDefinitions(Index = 7, Name = nameof(BrokerName), Title = "Förmedling", Visible = false)]
        public string BrokerName { get; set; }

        [ColumnDefinitions(IsLeftCssClassName = true, Name = nameof(ColorClassName), Visible = false)]
        public string ColorClassName => CssClassHelper.GetColorClassNameForComplaintStatus(Status);

        public ComplaintStatus Status { get; set; }

        public ComplaintType ComplaintType { get; set; }

    }
}
