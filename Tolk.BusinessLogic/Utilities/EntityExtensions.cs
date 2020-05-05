using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Api.Payloads.WebHookPayloads;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Utilities
{
    public static class EntityExtensions
    {
        #region lists

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

        #endregion

        #region lists connected to order

        public static IQueryable<OrderStatusConfirmation> GetStatusConfirmationsForOrder(this IQueryable<OrderStatusConfirmation> confirmations, int id)
            => confirmations.Where(o => o.OrderId == id);

        public static IQueryable<OrderStatusConfirmation> GetStatusConfirmationsForOrderEventLog(this IQueryable<OrderStatusConfirmation> confirmations, int id)
            => confirmations.GetStatusConfirmationsForOrder(id)
                .Include(c => c.ConfirmedByUser);

        public static IQueryable<OrderChangeLogEntry> GetOrderChangeLogEntitesForOrderEventLog(this IQueryable<OrderChangeLogEntry> rows, int id)
            => rows.GetOrderChangeLogEntitesForOrder(id)
                    .Include(ch => ch.UpdatedByUser)
                    .Include(ch => ch.Broker)
                    .Include(ch => ch.OrderContactPersonHistory).ThenInclude(h => h.PreviousContactPersonUser)
                    .Include(ch => ch.OrderChangeConfirmation).ThenInclude(c => c.ConfirmedByUser)
                    .OrderBy(ch => ch.LoggedAt);

        public static IQueryable<OrderInterpreterLocation> GetOrderedInterpreterLocationsForOrder(this IQueryable<OrderInterpreterLocation> locations, int id)
             => locations.Where(r => r.OrderId == id).OrderBy(r => r.Rank);

        public static IQueryable<OrderCompetenceRequirement> GetOrderedCompetenceRequirementsForOrder(this IQueryable<OrderCompetenceRequirement> requirements, int id)
            => requirements.Where(r => r.OrderId == id).OrderBy(r => r.Rank);

        public static IQueryable<OrderRequirement> GetRequirementsForOrder(this IQueryable<OrderRequirement> requirements, int id)
            => requirements.Where(r => r.OrderId == id);

        public static IQueryable<Attachment> GetAttachmentsForOrderAndGroup(this IQueryable<Attachment> attachments, int id, int? orderGroupId)
            => attachments.Where(a =>
                        a.OrderGroups.Any(g => g.OrderGroupId == orderGroupId) &&
                            !a.OrderAttachmentHistoryEntries.Any(h => h.OrderGroupAttachmentRemoved && h.OrderChangeLogEntry.OrderId == id) ||
                        a.Orders.Any(o => o.OrderId == id));

        public static IQueryable<Attachment> GetAttachmentsForOrder(this IQueryable<Attachment> attachments, int id)
            => attachments.Where(a => a.Orders.Any(o => o.OrderId == id));

        public static IQueryable<OrderAttachment> GetAttachmentsForOrder(this IQueryable<OrderAttachment> attachments, int id)
            => attachments.Include(a => a.Attachment).Where(a => a.Attachment.Orders.Any(o => o.OrderId == id));

        public static IQueryable<OrderGroupAttachment> GetAttachmentsForOrderGroup(this IQueryable<OrderGroupAttachment> attachments, int id)
            => attachments.Include(a => a.Attachment).Where(a => a.Attachment.OrderGroups.Any(g => g.OrderGroupId == id));

        public static IQueryable<Request> GetLostRequestsForOrder(this IQueryable<Request> requests, int id)
            => requests.Where(r => r.OrderId == id &&
                       (
                           r.Status == RequestStatus.DeclinedByBroker ||
                           r.Status == RequestStatus.DeniedByTimeLimit ||
                           r.Status == RequestStatus.DeniedByCreator ||
                           r.Status == RequestStatus.LostDueToQuarantine
                       )
                );

        public static IQueryable<Request> GetRequestsForOrder(this IQueryable<Request> requests, int id)
            => requests.Include(r => r.Ranking).Where(r => r.OrderId == id);

        public static IQueryable<Request> GetRequestsForOrderForEventLog(this IQueryable<Request> requests, int id, int? brokerId = null)
        {
            var list = requests
                .Include(r => r.ReceivedByUser)
                .Include(r => r.AnsweringUser)
                .Include(r => r.ProcessingUser)
                .Include(r => r.CancelledByUser)
                .Include(r => r.Interpreter)
                .Include(r => r.ReplacedByRequest).ThenInclude(r => r.Interpreter)
                .Include(r => r.ReplacedByRequest).ThenInclude(r => r.AnsweringUser)
                .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder)
                .Include(r => r.Order).ThenInclude(o => o.ReplacedByOrder).ThenInclude(r => r.CreatedByUser)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.RequestUpdateLatestAnswerTime).ThenInclude(r => r.UpdatedByUser)
                .Where(r => r.OrderId == id);
            if (brokerId.HasValue)
            {
                list = list.Where(r => r.Ranking.BrokerId == brokerId);
            }
            return list;
        }

        public static IQueryable<RequestStatusConfirmation> GetRequestStatusConfirmationsForOrder(this IQueryable<RequestStatusConfirmation> confirmations, int id)
            => confirmations.Include(c => c.ConfirmedByUser).Where(c => c.Request.OrderId == id);


        public static IQueryable<OrderPriceRow> GetPriceRowsForOrder(this IQueryable<OrderPriceRow> rows, int id)
            => rows.Include(p => p.PriceListRow).Where(p => p.OrderId == id);

        public static IQueryable<OrderChangeLogEntry> GetOrderChangeLogEntitesForOrder(this IQueryable<OrderChangeLogEntry> rows, int id)
            => rows.Include(c => c.OrderChangeConfirmation).Where(c => c.OrderId == id);

        public static IQueryable<OrderHistoryEntry> GetOrderHistoriesForOrderChangeConfirmations(this IQueryable<OrderHistoryEntry> rows, List<int> ids)
          => rows.Where(c => ids.Contains(c.OrderChangeLogEntryId));

        #endregion

        #region lists connected to requests

        public static IQueryable<RequestStatusConfirmation> GetStatusConfirmationsForRequest(this IQueryable<RequestStatusConfirmation> confirmations, int id)
            => confirmations.Where(o => o.RequestId == id);

        public static IQueryable<RequestPriceRow> GetPriceRowsForRequest(this IQueryable<RequestPriceRow> rows, int id)
            => rows.Include(p => p.PriceListRow).Where(o => o.RequestId == id);

        public static IQueryable<Attachment> GetAttachmentsForRequest(this IQueryable<Attachment> attachments, int id, int? requestGroupId)
            => attachments.Where(a => a.RequestGroups.Any(g => g.RequestGroupId == (requestGroupId ?? -1)) ||
                    a.Requests.Any(r => r.RequestId == id));

        public static IQueryable<OrderRequirementRequestAnswer> GetRequirementAnswersForRequest(this IQueryable<OrderRequirementRequestAnswer> answers, int id)
           => answers.Include(a => a.OrderRequirement).Where(a => a.RequestId == id);

        public static IQueryable<RequestView> GetActiveViewsForRequest(this IQueryable<RequestView> views, int id)
           => views.Include(a => a.ViewedByUser).Where(a => a.RequestId == id);

        public static IQueryable<RequestView> GetRequestViewsForRequest(this IQueryable<RequestView> views, int id)
           => views.Where(v => v.RequestId == id);

        public static IQueryable<Requisition> GetRequisitionsForRequest(this IQueryable<Requisition> requisitions, int id)
            => requisitions
                .Include(r => r.CreatedByUser)
                .Include(r => r.ProcessedUser)
                .Where(r => r.RequestId == id);

        public static IQueryable<Requisition> GetRequisitionsForOrder(this IQueryable<Requisition> requisitions, int id, int? brokerId = null)
        {
            var list = requisitions
                .Include(r => r.CreatedByUser)
                .Include(r => r.ProcessedUser)
                .Where(r => r.Request.OrderId == id);
            if (brokerId.HasValue)
            {
                list = list.Where(r => r.Request.Ranking.BrokerId == brokerId);
            }
            return list;
        }

        public static IQueryable<RequisitionStatusConfirmation> GetRequisitionsStatusConfirmationsByRequest(this IQueryable<RequisitionStatusConfirmation> confirmations, int id)
            => confirmations.Include(c => c.ConfirmedByUser).Where(r => r.Requisition.RequestId == id);

        public static IQueryable<Complaint> GetComplaintsForRequest(this IQueryable<Complaint> complaints, int id)
           => complaints.Where(c => c.RequestId == id);

        public static IQueryable<RequisitionStatusConfirmation> GetRequisitionsStatusConfirmationsForOrder(this IQueryable<RequisitionStatusConfirmation> confirmations, int id, int? brokerId = null)
        {
            var list = confirmations.Include(c => c.ConfirmedByUser).Where(r => r.Requisition.RequestId == id);
            if (brokerId.HasValue)
            {
                list = list.Where(c => c.Requisition.Request.Ranking.BrokerId == brokerId);
            }
            return list;
        }

        #endregion

        #region lists connected to requsition

        public static IQueryable<RequisitionPriceRow> GetPriceRowsForRequisition(this IQueryable<RequisitionPriceRow> rows, int id)
            => rows.Include(p => p.PriceListRow).Where(p => p.RequisitionId == id);

        public static IQueryable<MealBreak> GetMealBreksForRequisition(this IQueryable<MealBreak> rows, int id)
            => rows.Where(p => p.RequisitionId == id);

        public static IQueryable<RequisitionStatusConfirmation> GetRequisitionStatusConfirmationsForRequisition(this IQueryable<RequisitionStatusConfirmation> confirmations, int id)
            => confirmations.Include(c => c.ConfirmedByUser).Where(c => c.RequisitionId == id);

        #endregion

        #region single entities by id

        public static async Task<Request> GetLastRequestForOrder(this IQueryable<Request> requests, int id)
            => await requests.Where(r => r.OrderId == id).OrderBy(r => r.RequestId).LastAsync();

        public static async Task<Order> GetOrderForEventLog(this IQueryable<Order> orders, int id)
            => await orders
                .Include(o => o.ReplacingOrder)
                .Include(o => o.CreatedByUser)
                .Include(o => o.ContactPersonUser)
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.CustomerUnit)
                .Include(o => o.ReplacedByOrder).ThenInclude(o => o.CreatedByUser)
                .SingleOrDefaultAsync(o => o.OrderId == id);

        public static async Task<Order> GetFullOrderById(this IQueryable<Order> orders, int id)
            => await orders.GetOrdersWithInclude().SingleAsync(o => o.OrderId == id);

        public static async Task<Order> GetFullOrderByRequestId(this IQueryable<Order> orders, int id)
            => await orders.GetOrdersWithInclude().SingleAsync(o => o.Requests.Any(r => r.RequestId == id));

        public static async Task<Order> GetOrderWithContactsById(this IQueryable<Order> orders, int id)
            => await orders
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.CustomerUnit)
                .Include(o => o.CreatedByUser)
                .Include(o => o.ContactPersonUser)
                .SingleAsync(o => o.OrderId == id);

        public static async Task<Request> GetRequestById(this IQueryable<Request> requests, int id)
            => await requests
                .Include(r => r.Interpreter)
                .Include(r => r.Ranking).ThenInclude(ra => ra.Broker)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.AnsweringUser)
                .SingleAsync(r => r.RequestId == id);

        public static async Task<Request> GetSimpleRequestById(this IQueryable<Request> requests, int id)
            => await requests
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order)
                .SingleAsync(r => r.RequestId == id);

        public static async Task<Request> GetRequestForRequisitionCreateById(this IQueryable<Request> requests, int id)
           => await requests.GetRequestsWithBaseIncludes()
                   .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder)
                   .SingleAsync(r => r.RequestId == id);

        public static async Task<Request> GetRequestForOtherViewsById(this IQueryable<Request> requests, int id)
              => await requests
                   .Include(r => r.Interpreter)
                   .Include(r => r.Order).ThenInclude(o => o.CreatedByUser)
                   .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                   .Include(r => r.Order).ThenInclude(o => o.Language)
                   .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                   .Include(r => r.Ranking).ThenInclude(r => r.Region)
                   .SingleAsync(r => r.RequestId == id);

        public static async Task<Request> GetRequestsForAcceptById(this IQueryable<Request> requests, int id)
            => await requests.GetRequestsWithBaseIncludes()
                    .Include(r => r.RequestGroup)
                    .Include(r => r.Order).ThenInclude(o => o.Language)
                    .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder)
                    .Include(r => r.Order).ThenInclude(o => o.ExtraInterpreterOrder)
                    .Include(r => r.Order).ThenInclude(o => o.IsExtraInterpreterForOrder)
                    .SingleAsync(r => r.RequestId == id);

        public static async Task<Request> GetRequestsWithContactsById(this IQueryable<Request> requests, int id)
             => await requests.GetRequestsWithBaseIncludes().SingleAsync(r => r.RequestId == id);

        public static async Task<Request> GetRequestsForChangeInterpreterWithBrokerAndOrderNumber(this IQueryable<Request> requests, string orderNumber, int brokerId)
           => await requests.GetRequestsWithBaseIncludes()
                   .Include(r => r.RequestGroup)
                   .Include(r => r.Order).ThenInclude(o => o.Language)
                   .Include(r => r.Order).ThenInclude(o => o.Region)
                   .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder)
                   .Include(r => r.Order).ThenInclude(o => o.ExtraInterpreterOrder)
                   .Include(r => r.Order).ThenInclude(o => o.IsExtraInterpreterForOrder)
                   .SingleOrDefaultAsync(r => r.Order.OrderNumber == orderNumber &&
                   r.Ranking.BrokerId == brokerId && r.ReplacingRequest == null);

        public static async Task<Order> GetOrderWithBrokerAndOrderNumber(this IQueryable<Order> orders, string orderNumber, int brokerId)
           => await orders
                .SingleOrDefaultAsync(o => o.OrderNumber == orderNumber && o.Requests.Any(r => r.Ranking.BrokerId == brokerId));

        public static async Task<Request> GetActiveRequestForApiWithBrokerAndOrderNumber(this IQueryable<Request> requests, string orderNumber, int brokerId)
             => await requests.GetRequestsWithBaseIncludes()
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.Region)
                .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder)
                .SingleOrDefaultAsync(r => r.Order.OrderNumber == orderNumber &&
                    r.Ranking.BrokerId == brokerId && r.ReplacingRequest == null);

        public static async Task<Request> GetConfirmedRequestForApiWithBrokerAndOrderNumber(this IQueryable<Request> requests, string orderNumber, int brokerId, IEnumerable<RequestStatus> statuses)
            => await requests.GetRequestsWithBaseIncludesForApi()
                .SingleOrDefaultAsync(r => r.Order.OrderNumber == orderNumber &&
                    r.Ranking.BrokerId == brokerId && statuses.Contains(r.Status));

        public static async Task<Request> GetSimpleActiveRequestForApiWithBrokerAndOrderNumber(this IQueryable<Request> requests, string orderNumber, int brokerId)
           => await requests.GetRequestsWithBaseIncludesForApi()
               .SingleOrDefaultAsync(r => r.Order.OrderNumber == orderNumber &&
                   r.Ranking.BrokerId == brokerId && r.ReplacingRequest == null);

        public static async Task<Request> GetActiveRequestByOrderId(this IQueryable<Request> requests, int orderId, bool includeNotAnsweredByCreator = true)
        {
            var request = await requests
                .Include(r => r.Order)
                .Include(r => r.AnsweringUser)
                .Include(r => r.Interpreter)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .SingleOrDefaultAsync(r =>
                                    r.OrderId == orderId &&
                                    r.Status != RequestStatus.InterpreterReplaced &&
                                    r.Status != RequestStatus.DeniedByTimeLimit &&
                                    r.Status != RequestStatus.DeniedByCreator &&
                                    r.Status != RequestStatus.DeclinedByBroker &&
                                    r.Status != RequestStatus.LostDueToQuarantine &&
                                    (includeNotAnsweredByCreator || r.Status != RequestStatus.ResponseNotAnsweredByCreator));
            return request;
        }

        public static async Task<Complaint> GetFullComplaintById(this IQueryable<Complaint> complaints, int id)
                => await complaints.GetComplaintsWithBaseIncludes()
                .Include(r => r.Request).ThenInclude(r => r.Interpreter)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerUnit)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .SingleOrDefaultAsync(o => o.ComplaintId == id);

        public static async Task<Complaint> GetComplaintForEventLogByOrderId(this IQueryable<Complaint> complaints, int id, int? brokerId = null)
            => await complaints.GetComplaintsWithBaseIncludes()
                .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(c => c.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .SingleOrDefaultAsync(c => c.Request.OrderId == id && (brokerId == null || c.Request.Ranking.BrokerId == brokerId));

        public static async Task<Complaint> GetComplaintForEventLogByRequestId(this IQueryable<Complaint> complaints, int id)
            => await complaints.GetComplaintsWithBaseIncludes()
                .SingleOrDefaultAsync(c => c.RequestId == id);

        public static async Task<Requisition> GetRequisitionById(this IQueryable<Requisition> requisitions, int id)
            => await requisitions.GetRequisitionsWithBaseIncludes()
                .SingleOrDefaultAsync(o => o.RequisitionId == id);

        public static async Task<Requisition> GetFullRequisitionById(this IQueryable<Requisition> requisitions, int id)
            => await requisitions.GetRequisitionsWithBaseIncludes()
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CreatedByUser)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.ContactPersonUser)
                .SingleOrDefaultAsync(o => o.RequisitionId == id);

        public static async Task<Requisition> GetActiveRequisitionWithBrokerAndOrderNumber(this IQueryable<Requisition> requisitions, string orderNumber, int brokerId)
            => await requisitions
                .Include(r => r.Request)
                .SingleOrDefaultAsync(r => r.Request.Order.OrderNumber == orderNumber &&
                r.Request.Ranking.BrokerId == brokerId && r.ReplacedByRequisitionId == null);

        public static async Task<Complaint> GetComplaintWithBrokerAndOrderNumber(this IQueryable<Complaint> complaints, string orderNumber, int brokerId)
            => await complaints
                .Include(c => c.CreatedByUser)
                .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerUnit)
                .SingleOrDefaultAsync(c => c.Request.Order.OrderNumber == orderNumber && c.Request.Ranking.BrokerId == brokerId);

        public static async Task<AspNetUser> GetUser(this IQueryable<AspNetUser> users, int id)
            => await users.Include(u => u.CustomerOrganisation).SingleOrDefaultAsync(u => u.Id == id);

        public static async Task<OutboundWebHookCall> GetOutboundWebHookCall(this IQueryable<OutboundWebHookCall> outboundWebHookCalls, int id)
            => await outboundWebHookCalls.Include(c => c.RecipientUser).SingleOrDefaultAsync(c => c.OutboundWebHookCallId == id);

        #endregion

        public static DateTimeOffset ClosestStartAt(this IEnumerable<Request> requests)
        {
#warning detta KAN ge konstig sql...
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

        public static PriceInformationModel GetPriceInformationModel(this IEnumerable<PriceRowBase> priceRows, string competenceLevel, decimal brokerFee)
        {
#warning detta KAN ge konstig sql
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

        private static int? TryGetNullableInt(this string value)
        {
            return int.TryParse(value, out var i) ? (int?)i : null;
        }

        private static IQueryable<Order> GetOrdersWithInclude(this IQueryable<Order> orders)
            => orders
                .Include(o => o.ReplacedByOrder)
                .Include(o => o.ReplacingOrder).ThenInclude(o => o.CreatedByUser)
                .Include(o => o.CreatedByUser)
                .Include(o => o.ContactPersonUser)
                .Include(o => o.Region)
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.CustomerUnit)
                .Include(o => o.Language)
                .Include(o => o.Group);

        private static IQueryable<Request> GetRequestsWithBaseIncludes(this IQueryable<Request> requests)
            => requests
                .Include(r => r.Interpreter)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.CustomerUnit)
                .Include(r => r.Order.CreatedByUser)
                .Include(r => r.Order.ContactPersonUser);

        private static IQueryable<Request> GetRequestsWithBaseIncludesForApi(this IQueryable<Request> requests)
                => requests
                .Include(r => r.Ranking)
                .Include(r => r.Order);

        private static IQueryable<Requisition> GetRequisitionsWithBaseIncludes(this IQueryable<Requisition> requisitions)
                 => requisitions
                 .Include(r => r.CreatedByUser)
                 .Include(r => r.ProcessedUser)
                 .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                 .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker);

        private static IQueryable<Complaint> GetComplaintsWithBaseIncludes(this IQueryable<Complaint> complaints)
            => complaints.Include(c => c.CreatedByUser)
                .Include(c => c.AnsweringUser)
                .Include(c => c.AnswerDisputingUser)
                .Include(c => c.TerminatingUser);


        #region Liv

        public static async Task<CustomerOrganisation> GetCustomerById(this IQueryable<CustomerOrganisation> customers, int id)
           => await customers
               .Include(c => c.ParentCustomerOrganisation)
               .SingleAsync(c => c.CustomerOrganisationId == id);

        public static IQueryable<CustomerSetting> GetCustomerSettingsForCustomer(this IQueryable<CustomerSetting> customerSettings, int id)
           => customerSettings.Where(c => c.CustomerOrganisationId == id);

        public static async Task<Requisition> GetPreviosRequisitionByRequestId(this IQueryable<Requisition> requisitions, int id)
            => await requisitions
                .Include(r => r.CreatedByUser)
                .Include(r => r.ProcessedUser)
                .Include(r => r.Request).ThenInclude(r => r.Order)
                .SingleOrDefaultAsync(r => r.RequestId == id && r.Status == RequisitionStatus.Commented && !r.ReplacedByRequisitionId.HasValue);

        public static IQueryable<Attachment> GetAttachmentsForRequisition(this IQueryable<Attachment> attachments, int id)
            => attachments.Where(a => a.Requisitions.Any(r => r.RequisitionId == id));

        public static IQueryable<RequisitionAttachment> GetRequisitionAttachmentsForRequisition(this IQueryable<RequisitionAttachment> attachments, int id)
            => attachments.Include(a => a.Attachment).Where(a => a.RequisitionId == id);

        public static async Task<Request> GetLastRequestWithRankingForOrder(this IQueryable<Request> requests, int id)
            => await requests.Where(r => r.OrderId == id)
            .Include(r => r.Ranking)
            .OrderBy(r => r.RequestId).LastAsync();

        public static IQueryable<CustomerOrganisation> GetAllCustomers(this IQueryable<CustomerOrganisation> customerOrganisations)
            => customerOrganisations.Include(c => c.ParentCustomerOrganisation).OrderBy(c => c.Name);

        public static IQueryable<FaqDisplayUserRole> GetAllFaqWithFaqDisplayUserRoles(this IQueryable<FaqDisplayUserRole> faqDisplayUserRoles)
            => faqDisplayUserRoles.Include(f => f.Faq);

        public static IQueryable <FaqDisplayUserRole> GetFaqWithFaqDisplayUserRolesByFaqId(this IQueryable<FaqDisplayUserRole> faqDisplayUserRoles, int id)
          => faqDisplayUserRoles.Include(f => f.Faq).Where(f => f.FaqId == id);

        public static IQueryable<FaqDisplayUserRole> GetFaqDisplayUserRolesByDisplayUserRoles(this IQueryable<FaqDisplayUserRole> faqDisplayUserRoles, IEnumerable<DisplayUserRole> displayUserRoles)
          => faqDisplayUserRoles.Where(f => displayUserRoles.Contains(f.DisplayUserRole));
        
        public static IQueryable<Faq> GetPublishedFaqWithFaqIds(this IQueryable<Faq> faqs, IQueryable<int> faqIds)
            => faqs.Where(f => faqIds.Contains(f.FaqId) && f.IsDisplayed);

        #endregion


        #region Added by johan

        public static async Task<RequestGroup> GetRequestGroupById(this IQueryable<RequestGroup> groups, int id)
                    => await groups
                        .Include(r => r.Ranking).ThenInclude(ra => ra.Broker)
                        .Include(r => r.OrderGroup).ThenInclude(o => o.CustomerOrganisation)
                        .SingleAsync(r => r.RequestGroupId == id);

        public static async Task<RequestGroup> GetActiveRequestGroupByOrderGroupId(this IQueryable<RequestGroup> groups, int id)
            => await groups
            .Include(r => r.Ranking)
            .Include(r => r.OrderGroup).ThenInclude(og => og.CustomerOrganisation)
            .SingleOrDefaultAsync(r => r.OrderGroupId == id && r.Status == RequestStatus.Created || r.Status == RequestStatus.Received);

        public static IQueryable<Request> GetRequestsForRequestGroup(this IQueryable<Request> requests, int id)
             => requests.Include(o => o.Order).Where(r => r.RequestGroupId == id);

        public static IQueryable<Order> GetOrdersForOrderGroup(this IQueryable<Order> orders, int id)
            => orders.Where(r => r.OrderGroupId == id);


        public async static Task<OrderGroup> GetOrderGroupById(this IQueryable<OrderGroup> groups, int id)
            => await groups.SingleOrDefaultAsync(og => og.OrderGroupId == id);

        public static IQueryable<OrderGroupStatusConfirmation> GetStatusConfirmationsForOrderGroup(this IQueryable<OrderGroupStatusConfirmation> confirmations, int id)
            => confirmations.Where(o => o.OrderGroupId == id);
        public static IQueryable<RequestGroup> GetRequestGroupsForOrderGroup(this IQueryable<RequestGroup> requestGroups, int id)
            => requestGroups
                .Include(r => r.OrderGroup).ThenInclude(og => og.CreatedByUser)
                .Include(r => r.OrderGroup).ThenInclude(og => og.CustomerOrganisation)
                .Include(r => r.OrderGroup).ThenInclude(og => og.Region)
                .Include(r => r.OrderGroup).ThenInclude(og => og.Language)
                .Include(r => r.OrderGroup).ThenInclude(og => og.CustomerUnit)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker);

        public static IQueryable<OrderGroupCompetenceRequirement> GetOrderedCompetenceRequirementsForOrderGroup(this IQueryable<OrderGroupCompetenceRequirement> requirements, int id)
            => requirements.Where(o => o.OrderGroupId == id);

        public static IQueryable<OrderGroupRequirement> GetRequirementsForOrderGroup(this IQueryable<OrderGroupRequirement> requirements, int id)
            => requirements.Where(o => o.OrderGroupId == id);

        public static IQueryable<Attachment> GetAttachmentsForOrderGroup(this IQueryable<Attachment> attachments, int id)
            => attachments.Where(a => a.OrderGroups.Any(g => g.OrderGroupId == id));

        public static IQueryable<OrderPriceRow> GetPriceRowsForOrderInOrderGroup(this IQueryable<OrderPriceRow> rows, int id)
            => rows.Include(p => p.PriceListRow).Where(p => p.Order.OrderGroupId == id);

        #endregion
    }

}


