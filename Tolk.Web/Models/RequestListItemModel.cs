using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Controllers;

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

        public DateTimeOffset Start { get; set; }

        public DateTimeOffset End { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }
    }

    public static class IQueryableOfRequestExtensions
    {
        public static IQueryable<RequestListItemModel> SelectRequestListItemModel(
            this IQueryable<Request> requests, bool isCustomer)
        {
            return requests.Select(r => new RequestListItemModel
            {
                RequestId = r.RequestId,
                Language = r.Order.OtherLanguage ?? r.Order.Language.Name ?? "(Tolkanvändarutbildning)",
                LanguageId = r.Order.LanguageId,
                OrderNumber = r.Order.OrderNumber.ToString(),
                CustomerName = r.Order.CustomerOrganisation.Name,
                CustomerId = r.Order.CustomerOrganisationId,
                RegionName = r.Order.Region.Name,
                RegionId = r.Order.RegionId,
                Start = r.Order.StartAt,
                End = r.Order.EndAt,
                ExpiresAt = r.ExpiresAt,
                Status = r.Status,
                Action = ((!isCustomer && (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received)) || (isCustomer && r.Status == RequestStatus.Accepted) ? nameof(RequestController.Process) : nameof(RequestController.View))
            });
        }
    }
}
