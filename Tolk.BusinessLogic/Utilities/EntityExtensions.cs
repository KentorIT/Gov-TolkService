using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.Api.Payloads.WebHookPayloads;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Utilities
{
    public static class EntityExtensions
    {
        public static IQueryable<OrderGroup> CustomerOrderGroups(this IQueryable<OrderGroup> orderGroups, int customerOrganisationId, int userId, IEnumerable<int> customerUnits, bool isCentralAdminOrOrderHandler = false)
        {
            var filteredOrderGroups = orderGroups.Where(o => o.CustomerOrganisationId == customerOrganisationId);
            return isCentralAdminOrOrderHandler ? filteredOrderGroups :
                filteredOrderGroups.Where(o => (o.CreatedBy == userId && o.CustomerUnitId == null) || customerUnits.Contains(o.CustomerUnitId ?? -1));
        }
        public static IQueryable<OrderListRow> CustomerOrderListRows(this IQueryable<OrderListRow> entities, int customerOrganisationId, int userId, IEnumerable<int> customerUnits, bool isCentralAdminOrOrderHandler = false)
        {
            var filteredOrderGroups = entities.Where(o => o.CustomerOrganisationId == customerOrganisationId);
            return isCentralAdminOrOrderHandler ? filteredOrderGroups :
                filteredOrderGroups.Where(o => (o.CreatedBy == userId && o.CustomerUnitId == null) || o.ContactPersonId == userId || customerUnits.Contains(o.CustomerUnitId ?? -1));
        }

        public static IQueryable<Order> CustomerOrders(this IQueryable<Order> orders, int customerOrganisationId, int userId, IEnumerable<int> customerUnits, bool isCentralAdminOrOrderHandler = false, bool includeContact = false, bool includeOrderGroupOrders = false)
        {
            var filteredOrders = orders.Where(o => o.CustomerOrganisationId == customerOrganisationId && (includeOrderGroupOrders || o.OrderGroupId == null));
            return isCentralAdminOrOrderHandler ? filteredOrders :
                filteredOrders.Where(o => (o.CreatedBy == userId && o.CustomerUnitId == null) || (includeContact && o.ContactPersonId == userId) ||
                    customerUnits.Contains(o.CustomerUnitId ?? -1));
        }

        public static IQueryable<Request> BrokerRequests(this IQueryable<Request> requests, int brokerId)
        {
            return requests.Where(r => r.Ranking.BrokerId == brokerId &&
                    r.Status != RequestStatus.AwaitingDeadlineFromCustomer &&
                    r.Status != RequestStatus.NoDeadlineFromCustomer &&
                    r.Status != RequestStatus.InterpreterReplaced);
        }

        /// <summary>
        /// Expires due to:
        /// 1. ExpiresAt has passed 
        /// 2. Customer has not set new expire time and order is starting 
        /// 3. Customer has not answered a responded request within latest answer time
        /// Also include requests that belong to approved requestgroup if LatestAnswerTimeForCustomer is set (interpreter changed)
        /// </summary>
        public static IQueryable<Request> ExpiredRequests(this IQueryable<Request> requests, DateTimeOffset now)
        {
            return requests.Where(r => (r.RequestGroupId == null || (r.RequestGroupId.HasValue && r.LatestAnswerTimeForCustomer.HasValue && r.RequestGroup.Status == RequestStatus.Approved)) &&
                    ((r.ExpiresAt <= now && (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received)) ||
                    (r.Order.StartAt <= now && r.Status == RequestStatus.AwaitingDeadlineFromCustomer) ||
                    (r.LatestAnswerTimeForCustomer.HasValue && r.LatestAnswerTimeForCustomer <= now && (r.Status == RequestStatus.Accepted || r.Status == RequestStatus.AcceptedNewInterpreterAppointed))));
        }

        /// <summary>
        /// Expires due to:
        /// 1. ExpiresAt has passed for group 
        /// 2. Customer has not set new expire time and one order is starting 
        /// 3. Customer has not answered a responded requestgroup within latest answer time
        /// </summary>
        public static IQueryable<RequestGroup> ExpiredRequestGroups(this IQueryable<RequestGroup> requestGroups, DateTimeOffset now)
        {
            return requestGroups.Where(rg => (rg.ExpiresAt <= now && (rg.Status == RequestStatus.Created || rg.Status == RequestStatus.Received)) ||
                    (rg.OrderGroup.Orders.Any(o => o.Status == OrderStatus.AwaitingDeadlineFromCustomer && o.StartAt <= now) && rg.Status == RequestStatus.AwaitingDeadlineFromCustomer) ||
                    (rg.LatestAnswerTimeForCustomer.HasValue && rg.LatestAnswerTimeForCustomer <= now && (rg.Status == RequestStatus.Accepted || rg.Status == RequestStatus.AcceptedNewInterpreterAppointed)));
        }

        /// <summary>
        /// Requests that are responded but not answered by customer and order is starting 
        /// Also include accepted requests that belong to approved requestgroup (interpreter changed)
        /// </summary>
        public static IQueryable<Request> NonAnsweredRespondedRequests(this IQueryable<Request> requests, DateTimeOffset now)
        {
            return requests.Where(r => (r.RequestGroupId == null || (r.RequestGroupId.HasValue && r.RequestGroup.Status == RequestStatus.Approved)) &&
                    (r.Order.Status == OrderStatus.RequestResponded || r.Order.Status == OrderStatus.RequestRespondedNewInterpreter) &&
                    r.Order.StartAt <= now && (r.Status == RequestStatus.Accepted || r.Status == RequestStatus.AcceptedNewInterpreterAppointed));
        }

        /// <summary>
        /// Requestgroups that are responded but not answered by customer and first order is starting 
        /// </summary>
        public static IQueryable<RequestGroup> NonAnsweredRespondedRequestGroups(this IQueryable<RequestGroup> requestGroups, DateTimeOffset now)
        {
            return requestGroups.Where(rg => (rg.OrderGroup.Status == OrderStatus.RequestResponded || rg.OrderGroup.Status == OrderStatus.RequestRespondedNewInterpreter) &&
                    rg.OrderGroup.Orders.Any(o => o.StartAt <= now) && (rg.Status == RequestStatus.Accepted || rg.Status == RequestStatus.AcceptedNewInterpreterAppointed));
        }

        public static IQueryable<Request> CompletedRequests(this IQueryable<Request> requests, DateTimeOffset now)
        {
            return requests.Where(r => (r.Order.EndAt <= now && r.Order.Status == OrderStatus.ResponseAccepted) &&
                     r.Status == RequestStatus.Approved && !(r.CompletedNotificationIsHandled ?? false));
        }

        public static DateTimeOffset ClosestStartAt(this IEnumerable<Request> requests)
        {
            return requests.GetRequestOrders().OrderBy(o => o.StartAt).First().StartAt;
        }

        public static IEnumerable<Order> GetRequestOrders(this IEnumerable<Request> requests)
        {
            return requests.Select(r => r.Order);
        }

        public static int? GetIntValue(this AspNetUser user, DefaultSettingsType type)
        {
            return user?.GetValue(type).TryGetNullableInt();
        }

        public static string GetValue(this AspNetUser user, DefaultSettingsType type)
        {
            return user?.DefaultSettings.SingleOrDefault(d => d.DefaultSettingType == type)?.Value;
        }

        public static T? TryGetEnumValue<T>(this AspNetUser user, DefaultSettingsType type) where T : struct
        {
            //First test if the value is a null then try to get the Int and of that os not ok, check if it is a string representation of the enum
            string value = user?.DefaultSettings.SingleOrDefault(d => d.DefaultSettingType == type)?.Value;
            if (value != null)
            {
                int? i = value.TryGetNullableInt();
                return (i == null ? (T?)null : (T)(object)i.Value) ?? (T?)EnumHelper.Parse<T>(value);
            }
            return null;

        }

        private static int? TryGetNullableInt(this string value)
        {
            return int.TryParse(value, out var i) ? (int?)i : null;
        }

        public static PriceInformationModel GetPriceInformationModel(this IEnumerable<PriceRowBase> priceRows, string competenceLevel, decimal brokerFee)
        {
            return new PriceInformationModel
            {
                PriceCalculatedFromCompetenceLevel = competenceLevel,
                PriceRows = priceRows.GroupBy(r => r.PriceRowType)
                    .Select(p => new PriceRowModel
                    {
                        Description = p.Key.GetDescription(),
                        PriceRowType = p.Key.GetCustomName(),
                        Price = p.Count() == 1 ? p.Sum(s => s.TotalPrice) : 0,
                        CalculationBase = p.Count() == 1 ? p.Key == PriceRowType.BrokerFee ? brokerFee : p.Single()?.PriceCalculationCharge?.ChargePercentage : null,
                        CalculatedFrom = p.Key == PriceRowType.BrokerFee ? "Note that this is rounded to SEK, no decimals, when calculated" : EnumHelper.Parent<PriceRowType, PriceRowType?>(p.Key)?.GetCustomName(),
                        PriceListRows = p.Where(l => l.PriceListRowId != null).Select(l => new PriceRowListModel
                        {
                            PriceListRowType = l.PriceListRow.PriceListRowType.GetCustomName(),
                            Description = l.PriceListRow.PriceListRowType.GetDescription(),
                            Price = l.Price,
                            Quantity = l.Quantity
                        })
                    })
            };
        }
    }
}
