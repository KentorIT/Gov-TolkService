using System;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Attributes;
using Tolk.Web.Controllers;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class RequestListItemModel
    {
        [ColumnDefinitions(IsIdColumn = true, Index = 0, Name = nameof(RequestId), Visible = false)]
        public int RequestId { get; set; }

        [ColumnDefinitions(Index = 1, Name = nameof(OrderNumber), Title = "BokningsID")]
        public string OrderNumber { get; set; }

        [ColumnDefinitions(Index = 2, Name = nameof(StatusName), Title = "Status")]
        public string StatusName => Status.GetDescription();

        [ColumnDefinitions(Index = 3, Name = nameof(Language), Title = "Språk")]
        public string Language { get; set; }

        [ColumnDefinitions(Index = 4, Name = nameof(OrderDateAndTime), Title = "Datum för uppdrag")]
        public string OrderDateAndTime { get; set; }

        [ColumnDefinitions(Index = 5, Name = nameof(RegionName), Title = "Län")]
        public string RegionName { get; set; }

        [ColumnDefinitions(Index = 6, Name = nameof(CustomerName), Title = "Myndighet")]
        public string CustomerName { get; set; }

        [ColumnDefinitions(Index = 7, Name = nameof(ExpiresAt), SortOnWebServer = false, Title = "Svar innan")]
        public string ExpiresAt { get; set; }

        [ColumnDefinitions(IsLeftCssClassName = true, Name = nameof(ColorClassName), Visible = false)]
        public string ColorClassName => CssClassHelper.GetColorClassNameForRequestStatus(Status);

        public RequestStatus Status { get; set; }
    }

    public static class IQueryableOfRequestExtensions
    {
        public static IQueryable<RequestListItemModel> SelectRequestListItemModel(this IQueryable<Request> requests)
        {
            return requests.Select(r => new RequestListItemModel
            {
                RequestId = r.RequestId,
                Language = r.Order.OtherLanguage ?? r.Order.Language.Name,
                OrderNumber = r.Order.OrderNumber,
                CustomerName = r.Order.CustomerOrganisation.Name,
                RegionName = r.Order.Region.Name,
                OrderDateAndTime = $"{r.Order.StartAt.ToString("yyyy-MM-dd")} {r.Order.StartAt.ToString("HH\\:mm")}-{r.Order.EndAt.ToString("HH\\:mm")}",
                ExpiresAt = r.ExpiresAt.HasValue ? r.ExpiresAt.Value.ToString("yyyy-MM-dd HH:mm") : null,
                Status = r.Status
            });
        }
    }
}
