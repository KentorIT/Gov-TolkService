using System;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class RequestListItemModel
    {

        [ColumnDefinitions(IsIdColumn = true, Index = 0, Name = nameof(RequestId), Visible = false)]
        public int RequestId { get; set; }

        [ColumnDefinitions(Index = 1, Name = nameof(OrderDescriptor), ColumnName = "CreatedAt", SortOnWebServer = false, Title = "BokningsID")]
        public string OrderDescriptor => !string.IsNullOrEmpty(ParentOrderNumber) ? $"{OrderNumber}<br /><span class=\"startlist-subrow\">Del av: {ParentOrderNumber}</span>" : OrderNumber;

        [ColumnDefinitions(Index = 2, Name = nameof(StatusName), Title = "Status")]
        public string StatusName => Status.GetDescription();

        [ColumnDefinitions(Index = 3, Name = nameof(LanguageName), Title = "Språk")]
        public string LanguageName { get; set; }

        [ColumnDefinitions(Index = 4, Name = nameof(OrderDateAndTime), ColumnName = nameof(StartAt), SortOnWebServer = false, Title = "Datum för uppdrag")]
        public string OrderDateAndTime => $"{StartAt.ToSwedishString("yyyy-MM-dd")} {StartAt.ToSwedishString("HH\\:mm")}-{EndAt.ToSwedishString("HH\\:mm")}";

        [ColumnDefinitions(Index = 5, Name = nameof(RegionName), Title = "Län")]
        public string RegionName { get; set; }

        [ColumnDefinitions(Index = 6, Name = nameof(CustomerName), Title = "Myndighet")]
        public string CustomerName { get; set; }

        [ColumnDefinitions(Index = 7, Name = nameof(ExpiresAtDisplay), ColumnName = nameof(ExpiresAt), SortOnWebServer = false, Title = "Svar innan")]
        public string ExpiresAtDisplay => ExpiresAt.HasValue ? ExpiresAt.Value.ToSwedishString("yyyy-MM-dd HH:mm") : null;

        [ColumnDefinitions(IsLeftCssClassName = true, Name = nameof(ColorClassName), Visible = false)]
        public string ColorClassName => CssClassHelper.GetColorClassNameForRequestStatus(Status);

        public string OrderNumber { get; set; }
        public string ParentOrderNumber { get; set; }
        public RequestStatus Status { get; set; }

        [ColumnDefinitions(IsOverrideClickLinkUrlColumn = true, Name = nameof(LinkOverride), Visible = false)]
        public string LinkOverride { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }
    }

    public static class IQueryableOfRequestExtensions
    {
        public static IQueryable<RequestListItemModel> SelectRequestListItemModel(this IQueryable<RequestListRow> requests)
        {
            return requests.Select(r => new RequestListItemModel
            {
                RequestId = r.EntityId,
                LanguageName = r.LanguageName,
                OrderNumber = r.EntityNumber,
                ParentOrderNumber = r.EntityParentNumber,
                CustomerName = r.CustomerName,
                RegionName = r.RegionName,
                ExpiresAt = r.ExpiresAt,
                StartAt = r.StartAt,
                EndAt = r.EndAt,
                LinkOverride = r.RowType == OrderRowType.OrderGroup ? "/RequestGroup/View" : string.Empty,
                Status = r.Status
            });
        }
    }
}
