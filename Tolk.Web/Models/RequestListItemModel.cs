using System;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Controllers;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class RequestListItemModel
    {
        public string Action { get; set; }

        public int RequestId { get; set; }

        public RequestStatus Status { get; set; }

        public string OrderNumber { get; set; }

        public string RegionName { get; set; }

        public int RegionId { get; set; }

        public string CustomerName { get; set; }

        public int CustomerId { get; set; }

        public string Language { get; set; }

        public int? LanguageId { get; set; }

        [NoDisplayName]
        public virtual TimeRange OrderDateAndTime { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }

        public string ColorClassName { get => CssClassHelper.GetColorClassNameForRequestStatus(Status); }
    }

    public static class IQueryableOfRequestExtensions
    {
        public static IQueryable<RequestListItemModel> SelectRequestListItemModel(this IQueryable<Request> requests)
        {
            return requests.Select(r => new RequestListItemModel
            {
                RequestId = r.RequestId,
                Language = r.Order.OtherLanguage ?? r.Order.Language.Name,
                LanguageId = r.Order.LanguageId,
                OrderNumber = r.Order.OrderNumber.ToString(),
                CustomerName = r.Order.CustomerOrganisation.Name,
                CustomerId = r.Order.CustomerOrganisationId,
                RegionName = r.Order.Region.Name,
                RegionId = r.Order.RegionId,
                OrderDateAndTime = new TimeRange
                {
                    StartDateTime = r.Order.StartAt,
                    EndDateTime = r.Order.EndAt
                },
                ExpiresAt = r.ExpiresAt,
                Status = r.Status,
                Action = r.IsToBeProcessedByBroker ? nameof(RequestController.Process) : nameof(RequestController.View)
            });
        }
    }
}
