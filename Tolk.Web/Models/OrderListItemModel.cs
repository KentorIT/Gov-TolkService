using System;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class OrderListItemModel
    {
        [ColumnDefinitions(IsIdColumn = true, Index = 0, Name = nameof(EntityId), Visible = false)]
        public int EntityId { get; set; }

        [ColumnDefinitions(Index = 1, Name = nameof(OrderDescriptor), ColumnName = "CreatedAt", SortOnWebServer = false, Title = "BokningsID")]
        public string OrderDescriptor => !string.IsNullOrEmpty(ParentOrderNumber) ? $"{OrderNumber}<br /><span class=\"startlist-subrow\">Del av: {ParentOrderNumber}</span>" : OrderNumber;

        [ColumnDefinitions(Index = 2, Name = nameof(StatusName), Title = "Status")]
        public string StatusName => Status.GetDescription();

        [ColumnDefinitions(Index = 3, Name = nameof(Language), Title = "Språk")]
        public string Language { get; set; }

        [ColumnDefinitions(Index = 4, Name = nameof(OrderDateAndTime), ColumnName = "StartAt", SortOnWebServer = false, Title = "Datum för uppdrag")]
        public string OrderDateAndTime => $"{StartAt.ToSwedishString("yyyy-MM-dd")} {StartAt.ToSwedishString("HH\\:mm")}-{EndAt.ToSwedishString("HH\\:mm")}";

        [ColumnDefinitions(Index = 5, Name = nameof(RegionName), Title = "Län")]
        public string RegionName { get; set; }

        [ColumnDefinitions(Index = 6, Name = nameof(CustomerName), Title = "Myndighet")]
        public string CustomerName { get; set; }

        [ColumnDefinitions(Index = 7, Name = nameof(BrokerName), Title = "Förmedling")]
        public string BrokerName { get; set; }

        [ColumnDefinitions(Index = 8, Name = nameof(CreatorName), Title = "Skapad av")]
        public string CreatorName { get; set; }

        public string OrderNumber { get; set; }
        public string ParentOrderNumber { get; set; }

        public OrderRowType RowType { get; set; }

        public OrderStatus Status { get; set; }

        public string CustomerReferenceNumber { get; set; }

        public int RegionId { get; set; }
        public int? CustomerUnitId { get; set; }
        public int? LanguageId { get; set; }
        public int CreatedBy { get; set; }
        public int CustomerOrganisationId { get; set; }
        public int? BrokerId { get; set; }
        public bool CustomerUnitIsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }

        [ColumnDefinitions(IsOverrideClickLinkUrlColumn = true, Name = nameof(LinkOverride), Visible = false)]
        public string LinkOverride { get; set; }

        [ColumnDefinitions(IsLeftCssClassName = true, Name = nameof(ColorClassName), Visible = false)]
        public string ColorClassName => CssClassHelper.GetColorClassNameForOrderStatus(Status);
    }
}
