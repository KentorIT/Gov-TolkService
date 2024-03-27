using Microsoft.AspNetCore.Identity;
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

        // CHECK      
        public static IQueryable<PeppolPayload> ListOrderAgreements(this IQueryable<PeppolPayload> entities, int? customerOrganisationId)
            => customerOrganisationId.HasValue ? entities.Where(o => o.Request.Order.CustomerOrganisationId == customerOrganisationId) : entities;


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

        public static IQueryable<CustomerStartListRow> CustomerStartListRows(this IQueryable<CustomerStartListRow> entities, int customerOrganisationId, int userId, IEnumerable<int> customerUnits, bool includeContact = false, bool includeOrderGroupOrders = false)
        {
            return entities.Where(o => o.CustomerOrganisationId == customerOrganisationId && (includeOrderGroupOrders || o.OrderGroupId == null)
                && ((o.CreatedBy == userId && o.CustomerUnitId == null) || (includeContact && o.ContactPersonId == userId) ||
                    customerUnits.Contains(o.CustomerUnitId ?? -1)));
        }

        public static IQueryable<BrokerStartListRow> BrokerStartListRows(this IQueryable<BrokerStartListRow> entities, int brokerId)
        {
            return entities.Where(r => r.BrokerId == brokerId);
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
                    r.Status != RequestStatus.InterpreterReplaced &&
                    r.Status != RequestStatus.ReplacedAtAnswerAfterAccept &&
                    r.Status != RequestStatus.ReplacedAfterAcceptOfFlexible &&
                    r.Status != RequestStatus.ReplacedAfterPriceUpdate &&
                    r.Status != RequestStatus.LostDueToQuarantine);

        }

        public static FrameworkAgreement GetFrameworkAgreementByDate(this IQueryable<FrameworkAgreement> agreements, DateTime today)
            => agreements.Where(f => f.FirstValidDate <= today && f.LastValidDate >= today).SingleOrDefault();

        public static FrameworkAgreement GetCurrentOrLatestActiveFrameworkAgreementByDate(this IQueryable<FrameworkAgreement> agreements, DateTime today)
            => agreements.GetFrameworkAgreementByDate(today) ?? agreements.Where(a => a.FirstValidDate <= today).OrderBy(a => a.LastValidDate).Last();

        /// <summary>
        /// Expires due to:
        /// 1. ExpiresAt has passed 
        /// 2. LastAcceptAt has passed 
        /// 3. Customer has not set new expire time and order is starting 
        /// 4. Customer has not answered a responded request within latest answer time
        /// Also include requests that belong to approved requestgroup if LatestAnswerTimeForCustomer is set (interpreter changed)
        /// </summary>
        public static IQueryable<Request> ExpiredRequests(this IQueryable<Request> requests, DateTimeOffset now)
            => requests.Where(r =>
                    (r.RequestGroupId == null ||
                        (r.RequestGroupId.HasValue && r.LatestAnswerTimeForCustomer.HasValue && r.RequestGroup.Status == RequestStatus.Approved)
                    ) && (
                        (r.ExpiresAt <= now &&
                            (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received || r.Status == RequestStatus.AcceptedAwaitingInterpreter)
                        ) ||
                        (r.LastAcceptAt <= now &&
                            (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received)
                        ) ||
                        ((r.RespondedStartAt ?? r.Order.StartAt) <= now &&
                            r.Status == RequestStatus.AwaitingDeadlineFromCustomer
                        ) ||
                        (r.LatestAnswerTimeForCustomer.HasValue && r.LatestAnswerTimeForCustomer <= now &&
                            (r.Status == RequestStatus.AnsweredAwaitingApproval || r.Status == RequestStatus.AcceptedNewInterpreterAppointed)
                        )
                    )
                );

        public static Task<Request> GetExpiredRequest(this IQueryable<Request> requests, DateTimeOffset now, int requestId)
            => requests
                .ExpiredRequests(now)
                .Include(r => r.Ranking)
                    .ThenInclude(ra => ra.FrameworkAgreement)
                .Include(r => r.Order)
                    .ThenInclude(o => o.CustomerOrganisation)
                .SingleAsync(r => r.RequestId == requestId);

        public static IQueryable<Request> GetRequestsFromTerminatedFrameworkAgreement(this IQueryable<Request> requests, int frameworkAgreementId, IEnumerable<RequestStatus> openRequestStatuses)
            => requests
            .Where(r => r.Ranking.FrameworkAgreementId == frameworkAgreementId &&
                openRequestStatuses.Contains(r.Status));

        public static Request GetTerminatedRequest(this IQueryable<Request> requests, int frameworkAgreementId, IEnumerable<RequestStatus> openRequestStatuses, int requestId)
            => requests
                .Include(r => r.Ranking).ThenInclude(r => r.FrameworkAgreement)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.CreatedByUser)
                .Include(r => r.Order).ThenInclude(o => o.CustomerUnit)
                .Include(r => r.Order).ThenInclude(o => o.ContactPersonUser)
                .GetRequestsFromTerminatedFrameworkAgreement(frameworkAgreementId, openRequestStatuses)
                .Where(r => r.RequestId == requestId)
                .SingleOrDefault();

        public static IQueryable<RequestGroup> GetRequestGroupsFromTerminatedFrameworkAgreement(this IQueryable<RequestGroup> groups, int frameworkAgreementId, IEnumerable<RequestStatus> openRequestStatuses)
            => groups
            .Where(r => r.Ranking.FrameworkAgreementId == frameworkAgreementId &&
                openRequestStatuses.Contains(r.Status));

        public static RequestGroup GetTerminatedRequestGroup(this IQueryable<RequestGroup> groups, int frameworkAgreementId, IEnumerable<RequestStatus> openRequestStatuses, int requestGroupId)
            => groups
                .Include(r => r.Ranking).ThenInclude(r => r.FrameworkAgreement)
                 .Include(r => r.OrderGroup).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.OrderGroup).ThenInclude(o => o.CreatedByUser)
                .Include(r => r.OrderGroup).ThenInclude(o => o.CustomerUnit)
                .Include(r => r.OrderGroup)
                .GetRequestGroupsFromTerminatedFrameworkAgreement(frameworkAgreementId, openRequestStatuses)
                .Where(r => r.RequestGroupId == requestGroupId)
                .SingleOrDefault();

        /// <summary>
        /// Expires due to:
        /// 1. ExpiresAt has passed for group 
        /// 2. Customer has not set new expire time and one order is starting 
        /// 3. Customer has not answered a responded requestgroup within latest answer time
        /// </summary>
        public static IQueryable<RequestGroup> ExpiredRequestGroups(this IQueryable<RequestGroup> requestGroups, DateTimeOffset now)
            => requestGroups.Where(rg =>
                (rg.ExpiresAt <= now &&
                    (rg.Status == RequestStatus.Created || rg.Status == RequestStatus.Received || rg.Status == RequestStatus.AcceptedAwaitingInterpreter)
                ) ||
                (rg.LastAcceptAt <= now &&
                    (rg.Status == RequestStatus.Created || rg.Status == RequestStatus.Received)
                ) ||
                (rg.OrderGroup.Orders.Any(o => o.Status == OrderStatus.AwaitingDeadlineFromCustomer && o.StartAt <= now) &&
                    rg.Status == RequestStatus.AwaitingDeadlineFromCustomer) ||
                (rg.LatestAnswerTimeForCustomer.HasValue && rg.LatestAnswerTimeForCustomer <= now &&
                    (rg.Status == RequestStatus.AnsweredAwaitingApproval || rg.Status == RequestStatus.AcceptedNewInterpreterAppointed)
                )
            );

        public static Task<RequestGroup> GetExpiredRequestGroup(this IQueryable<RequestGroup> requestGroups, DateTimeOffset now, int requestGroupId)
            => requestGroups
                .ExpiredRequestGroups(now)
                .Include(r => r.Ranking)
                    .ThenInclude(r => r.FrameworkAgreement)
                .Include(g => g.OrderGroup)
                    .ThenInclude(o => o.CustomerOrganisation)
                .SingleAsync(r => r.RequestGroupId == requestGroupId);

        /// <summary>
        /// Requests that are responded but not answered by customer and order is starting 
        /// Also include accepted requests that belong to approved requestgroup (interpreter changed)
        /// </summary>
        public static IQueryable<Request> NonAnsweredRespondedRequests(this IQueryable<Request> requests, DateTimeOffset now)
        {
            return requests.Where(r => (r.RequestGroupId == null || (r.RequestGroupId.HasValue && r.RequestGroup.Status == RequestStatus.Approved)) &&
                    (r.Order.Status == OrderStatus.RequestRespondedAwaitingApproval || r.Order.Status == OrderStatus.RequestRespondedNewInterpreter) &&
                    (r.RespondedStartAt ?? r.Order.StartAt) <= now &&
                    (r.Status == RequestStatus.AnsweredAwaitingApproval || r.Status == RequestStatus.AcceptedNewInterpreterAppointed));
        }

        public static Task<Request> GetNonAnsweredRespondedRequest(this IQueryable<Request> requests, DateTimeOffset now, int requestId)
        {
            return requests
                .NonAnsweredRespondedRequests(now)
                .Include(r => r.Ranking)
                    .ThenInclude(ra => ra.FrameworkAgreement)
                .Include(r => r.Order)
                .ThenInclude(o => o.CustomerOrganisation)
                .SingleAsync(r => r.RequestId == requestId);
        }

        /// <summary>
        /// Requestgroups that are responded but not answered by customer and first order is starting 
        /// </summary>
        public static IQueryable<RequestGroup> NonAnsweredRespondedRequestGroups(this IQueryable<RequestGroup> requestGroups, DateTimeOffset now)
        {
            return requestGroups.Where(rg => (rg.OrderGroup.Status == OrderStatus.RequestRespondedAwaitingApproval || rg.OrderGroup.Status == OrderStatus.RequestRespondedNewInterpreter) &&
                    rg.OrderGroup.Orders.Any(o => o.StartAt <= now) && (rg.Status == RequestStatus.AnsweredAwaitingApproval || rg.Status == RequestStatus.AcceptedNewInterpreterAppointed));
        }

        public static Task<RequestGroup> GetNonAnsweredRespondedRequestGroup(this IQueryable<RequestGroup> requestGroups, DateTimeOffset now, int requestGroupId)
        {
            return requestGroups
                .NonAnsweredRespondedRequestGroups(now)
                .Include(r => r.Ranking)
                    .ThenInclude(ra => ra.FrameworkAgreement)
                .Include(g => g.OrderGroup)
                    .ThenInclude(o => o.CustomerOrganisation)
                .SingleAsync(r => r.RequestGroupId == requestGroupId);
        }

        /// <summary>
        /// Requests that are responded but not answered by customer and customer should get reminder email
        /// Also include accepted requests that belong to approved requestgroup (interpreter changed)
        /// </summary>
        public static IQueryable<Request> NonAnsweredRespondedRequestsToBeReminded(this IQueryable<Request> requests, DateTimeOffset now)
        {
            return requests.GetRequestsWithBaseIncludes().Where(r => (r.RequestGroupId == null || (r.RequestGroupId.HasValue && r.RequestGroup.Status == RequestStatus.Approved))
                  && r.Order.StartAt > now && (r.Status == RequestStatus.AnsweredAwaitingApproval || r.Status == RequestStatus.AcceptedNewInterpreterAppointed));
        }

        /// <summary>
        /// Requestgroups that are responded but not answered by customer and first order is starting 
        /// </summary>
        public static IQueryable<RequestGroup> NonAnsweredRespondedRequestGroupsToBeReminded(this IQueryable<RequestGroup> requestGroups, DateTimeOffset now)
        {
            return requestGroups.GetRequestGroupsWithBaseIncludes().Where(rg => rg.OrderGroup.Orders.Any(o => o.StartAt > now) && (rg.Status == RequestStatus.AnsweredAwaitingApproval || rg.Status == RequestStatus.AcceptedNewInterpreterAppointed));
        }

        public static IQueryable<Request> CompletedRequests(this IQueryable<Request> requests, DateTimeOffset now)
        {
            return requests.Where(r => (r.Order.EndAt <= now && r.Order.Status == OrderStatus.ResponseAccepted) &&
                     r.Status == RequestStatus.Approved && !(r.CompletedNotificationIsHandled ?? false));
        }

        public static Task<Request> GetCompletedRequestById(this IQueryable<Request> requests, DateTimeOffset now, int id)
        {
            return requests.CompletedRequests(now)
                .Include(r => r.Order)
                .Include(r => r.Ranking).ThenInclude(ra => ra.FrameworkAgreement)
                .SingleAsync(r => r.RequestId == id);
        }

        public static IQueryable<Request> GetRequestsForOrderAgreementCreation(this IQueryable<Request> requests, int customerOrganisationId,int brokerId,DateTimeOffset? enabledAt)
            => requests.Where(r =>
                    (r.Status == RequestStatus.Delivered || r.Status == RequestStatus.Approved) &&
                     r.Order.CustomerOrganisationId == customerOrganisationId &&
                     r.Ranking.Broker.BrokerId == brokerId &&
                     !r.PeppolPayloads.Any() &&
                     !r.Requisitions.Any() &&
                     r.Order.StartAt >= enabledAt);

        public static IQueryable<Requisition> GetRequisitionForPeppolMessageCreation(this IQueryable<Requisition> requisitions, int customerOrganisationId,int brokerId, DateTimeOffset? enabledAt)
            => requisitions.Where(r =>
                            (r.Status == RequisitionStatus.Approved ||
                            r.Status == RequisitionStatus.Created ||
                            r.Status == RequisitionStatus.Reviewed) &&
                        !r.ReplacedByRequisitionId.HasValue &&
                        r.Request.Order.CustomerOrganisationId == customerOrganisationId &&
                        r.Request.Ranking.Broker.BrokerId == brokerId &&
                        r.PeppolPayload == null &&
                        r.Request.Order.StartAt >= enabledAt);

        public static IQueryable<Requisition> GetAutoGeneratedRequisitionForOrderAgreementCreation(this IQueryable<Requisition> requisitions, int customerOrganisationId, int brokerId, DateTimeOffset? enabledAt)
            => requisitions.Where(r =>
                    r.Status == RequisitionStatus.AutomaticGeneratedFromCancelledOrder &&
                    r.PeppolPayload == null &&
                    r.Request.Order.CustomerOrganisationId == customerOrganisationId &&
                    r.Request.Ranking.Broker.BrokerId == brokerId &&
                    r.CreatedAt > enabledAt);

        public static async Task<Request> GetRequestForPeppolMessageCreation(this IQueryable<Request> requests, int requestId)
             => await requests
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.PeppolPayloads)
                .Include(r => r.Ranking).ThenInclude(o => o.Broker)
                .Include(r => r.Ranking).ThenInclude(o => o.FrameworkAgreement)
                .Include(r => r.Requisitions).ThenInclude(r => r.CreatedByUser)
                .Include(r => r.Requisitions).ThenInclude(r => r.ProcessedUser)
                .SingleOrDefaultAsync(o => o.RequestId == requestId);
        #endregion

        #region lists connected to order

        public static IQueryable<OrderStatusConfirmation> GetStatusConfirmationsForOrder(this IQueryable<OrderStatusConfirmation> confirmations, int id)
            => confirmations.Where(o => o.OrderId == id);

        public static IQueryable<OrderStatusConfirmation> GetStatusConfirmationsForOrderEventLog(this IQueryable<OrderStatusConfirmation> confirmations, int id)
            => confirmations.GetStatusConfirmationsForOrder(id)
                .Include(c => c.ConfirmedByUser);

        public static IQueryable<OrderChangeLogEntry> GetOrderChangeLogEntitesForOrderEventLog(this IQueryable<OrderChangeLogEntry> rows, int id)
            => rows.GetOrderChangeLogEntitesWithUserIncludes(id)
                    .Include(ch => ch.Broker)
                    .Include(ch => ch.OrderContactPersonHistory).ThenInclude(h => h.PreviousContactPersonUser)
                    .OrderBy(ch => ch.LoggedAt);

        public static IQueryable<OrderChangeLogEntry> GetOrderChangeLogEntitesWithUserIncludes(this IQueryable<OrderChangeLogEntry> rows, int id)
            => rows.GetOrderChangeLogEntitesForOrder(id)
                .Include(ch => ch.UpdatedByUser)
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

        public static IQueryable<OrderGroupAttachment> GetAttachmentsForOrderGroup(this IQueryable<OrderGroupAttachment> attachments, int ordergroupId, int? orderId = null)
            => attachments.Include(a => a.Attachment).Where(a => a.Attachment.OrderGroups.Any(g => g.OrderGroupId == ordergroupId)
            && (!orderId.HasValue || (!a.Attachment.OrderAttachmentHistoryEntries.Any(h => h.OrderGroupAttachmentRemoved && h.OrderChangeLogEntry.OrderId == orderId))));

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
            => requests.Include(r => r.Ranking).ThenInclude(ra => ra.FrameworkAgreement).Where(r => r.OrderId == id);

        public static IQueryable<Request> GetRequestsForOrderForEventLog(this IQueryable<Request> requests, int id, int? brokerId = null)
        {
            var list = requests
                .Include(r => r.ReceivedByUser)
                .Include(r => r.AnsweringUser)
                .Include(r => r.AcceptingUser)
                .Include(r => r.ProcessingUser)
                .Include(r => r.CancelledByUser)
                .Include(r => r.Interpreter)
                .Include(r => r.ReplacedByRequest).ThenInclude(r => r.Interpreter)
                .Include(r => r.ReplacedByRequest).ThenInclude(r => r.AnsweringUser)
                .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder)
                .Include(r => r.Order).ThenInclude(o => o.ReplacedByOrder).ThenInclude(r => r.CreatedByUser)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.RequestUpdateLatestAnswerTime).ThenInclude(r => r.UpdatedByUser)
                .Include(r => r.ReplacingRequest)
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

        public static IQueryable<Order> GetOrdersForStatistics(this IQueryable<Order> orders)
          => orders
                .Include(o => o.Region)
                .Include(o => o.Language)
                .Include(o => o.CustomerOrganisation);

        #endregion

        #region lists connected to requests

        public static IQueryable<RequestStatusConfirmation> GetStatusConfirmationsForRequest(this IQueryable<RequestStatusConfirmation> confirmations, int id)
            => confirmations.Where(o => o.RequestId == id);

        public static IQueryable<RequestStatusConfirmation> GetRequestStatusConfirmationsForRequestGroup(this IQueryable<RequestStatusConfirmation> confirmations, int requestGroupId)
            => confirmations.Where(c => c.Request.RequestGroupId == requestGroupId);

        public static IQueryable<RequestPriceRow> GetPriceRowsForRequest(this IQueryable<RequestPriceRow> rows, int id)
            => rows.Include(p => p.PriceListRow).Where(o => o.RequestId == id);

        public static IQueryable<Attachment> GetAttachmentsForRequest(this IQueryable<Attachment> attachments, int id, int? requestGroupId)
            => attachments.Where(a => a.RequestGroups.Any(g => g.RequestGroupId == (requestGroupId ?? -1)) ||
                    a.Requests.Any(r => r.RequestId == id));

        public static IQueryable<TemporaryAttachmentGroup> GetTemporaryAttachmentGroupsByGroupKey(this IQueryable<TemporaryAttachmentGroup> temporaryAttachmentGroups, Guid groupKey)
            => temporaryAttachmentGroups.Include(t => t.Attachment).Where(t => t.TemporaryAttachmentGroupKey == groupKey);

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

        #region lists connected to ordergroup and requestgroup

        public static IQueryable<Request> GetRequestsForRequestGroup(this IQueryable<Request> requests, int id)
            => requests
            .Include(r => r.Order)
            .Include(r => r.Interpreter)
            .Include(r => r.Ranking).ThenInclude(r => r.FrameworkAgreement)
            .Where(r => r.RequestGroupId == id);

        public static IQueryable<Request> GetRequestsWithIncludesForRequestGroup(this IQueryable<Request> requests, int id)
            => requests.GetRequestsWithBaseIncludesAndRegionAndLanguage()
                .Where(r => r.RequestGroupId == id && r.ReplacedByRequest == null);

        public static IQueryable<RequestPriceRow> GetRequestPriceRowsForRequestGroup(this IQueryable<RequestPriceRow> pricerows, int requestGroupId)
            => pricerows.Include(p => p.PriceListRow).Where(p => p.Request.RequestGroupId == requestGroupId);

        public static IQueryable<OrderRequirementRequestAnswer> GetRequirementAnswersForRequestsInGroup(this IQueryable<OrderRequirementRequestAnswer> answers, int requestGroupId)
            //=> answers.Include(a => a.OrderRequirement).Where(a => a.Request.RequestGroupId == requestGroupId);
            => answers.Where(a => a.Request.RequestGroupId == requestGroupId);

        public static IQueryable<Order> GetOrdersForOrderGroup(this IQueryable<Order> orders, int id, bool includeIsExtraInterpreterForOrder = false)
            => includeIsExtraInterpreterForOrder ? orders.Where(r => r.OrderGroupId == id) :
            orders.Include(o => o.IsExtraInterpreterForOrder).Where(r => r.OrderGroupId == id);

        public static IQueryable<Order> GetOrdersWithIncludesForOrderGroup(this IQueryable<Order> orders, int id)
            => orders.Include(o => o.Language).Include(o => o.Region).Include(o => o.CustomerOrganisation).Where(r => r.OrderGroupId == id);

        public static IQueryable<Order> GetOrdersWithUnitForOrderGroup(this IQueryable<Order> orders, int id)
           => orders.Include(o => o.CustomerUnit).Where(r => r.OrderGroupId == id);

        public static IQueryable<OrderGroupStatusConfirmation> GetStatusConfirmationsForOrderGroup(this IQueryable<OrderGroupStatusConfirmation> confirmations, int id)
            => confirmations.Where(o => o.OrderGroupId == id);

        public static IQueryable<OrderStatusConfirmation> GetOrderStatusConfirmationsForOrderGroup(this IQueryable<OrderStatusConfirmation> confirmations, int orderGroupId)
            => confirmations.Where(c => c.Order.OrderGroupId == orderGroupId);

        public static IQueryable<RequestGroupStatusConfirmation> GetStatusConfirmationsForRequestGroup(this IQueryable<RequestGroupStatusConfirmation> confirmations, int id)
            => confirmations.Where(c => c.RequestGroupId == id);

        public static IQueryable<RequestGroup> GetRequestGroupsForOrderGroup(this IQueryable<RequestGroup> requestGroups, int id)
            => requestGroups.GetRequestGroupsWithBaseIncludes()
                .Include(r => r.OrderGroup).ThenInclude(og => og.Region)
                .Include(r => r.OrderGroup).ThenInclude(og => og.Language)
                .Where(r => r.OrderGroupId == id);

        public static IQueryable<RequestGroup> GetPreviousRequestGroupsForOrderGroup(this IQueryable<RequestGroup> requestGroups, int id)
            => requestGroups
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Where(r => r.OrderGroupId == id &&
                (r.Status == RequestStatus.DeclinedByBroker || r.Status == RequestStatus.DeniedByTimeLimit ||
                 r.Status == RequestStatus.DeniedByCreator || r.Status == RequestStatus.LostDueToQuarantine));

        public static IQueryable<OrderGroupCompetenceRequirement> GetOrderedCompetenceRequirementsForOrderGroup(this IQueryable<OrderGroupCompetenceRequirement> requirements, int id)
            => requirements.Where(o => o.OrderGroupId == id);

        public static IQueryable<OrderGroupRequirement> GetRequirementsForOrderGroup(this IQueryable<OrderGroupRequirement> requirements, int id)
            => requirements.Where(o => o.OrderGroupId == id);

        public static IQueryable<OrderGroupInterpreterLocation> GetInterpreterLocationsForOrderGroup(this IQueryable<OrderGroupInterpreterLocation> interpreterLocations, int id)
            => interpreterLocations.Where(o => o.OrderGroupId == id);

        public static IQueryable<Attachment> GetAttachmentsForOrderGroup(this IQueryable<Attachment> attachments, int id)
            => attachments.Where(a => a.OrderGroups.Any(g => g.OrderGroupId == id));

        public static IQueryable<Attachment> GetAttachmentsForRequestGroup(this IQueryable<Attachment> attachments, int id)
            => attachments.Where(a => a.RequestGroups.Any(g => g.RequestGroupId == id));

        public static IQueryable<OrderPriceRow> GetPriceRowsForOrdersInOrderGroup(this IQueryable<OrderPriceRow> rows, int id)
            => rows.Include(p => p.PriceListRow).Include(p => p.PriceCalculationCharge).Where(p => p.Order.OrderGroupId == id);

        public static IQueryable<OrderRequirement> GetRequirementsForOrdersInOrderGroup(this IQueryable<OrderRequirement> rows, int orderGroupId)
            => rows.Where(p => p.Order.OrderGroupId == orderGroupId);

        public static IQueryable<OrderCompetenceRequirement> GetOrderCompetencesForOrdersInOrderGroup(this IQueryable<OrderCompetenceRequirement> orderCompetenceRequirements, int orderGroupId)
           => orderCompetenceRequirements.Where(oc => oc.Order.OrderGroupId == orderGroupId);

        public static IQueryable<OrderInterpreterLocation> GetInterpreterLocationsForOrdersInGroup(this IQueryable<OrderInterpreterLocation> locations, int orderGroupId)
            => locations.Where(c => c.Order.OrderGroupId == orderGroupId);

        public static IQueryable<RequestGroupView> GetRequestViewsForRequestGroup(this IQueryable<RequestGroupView> views, int id)
            => views.Where(v => v.RequestGroupId == id);

        public static IQueryable<RequestGroupView> GetActiveViewsForRequestGroup(this IQueryable<RequestGroupView> views, int id)
            => views.Include(a => a.ViewedByUser).Where(a => a.RequestGroupId == id);

        #endregion

        #region lists connected to requsition

        public static IQueryable<RequisitionPriceRow> GetPriceRowsForRequisition(this IQueryable<RequisitionPriceRow> rows, int id)
            => rows.Include(p => p.PriceListRow).Where(p => p.RequisitionId == id);

        public static IQueryable<MealBreak> GetMealBreksForRequisition(this IQueryable<MealBreak> rows, int id)
            => rows.Where(p => p.RequisitionId == id);

        public static IQueryable<RequisitionStatusConfirmation> GetRequisitionStatusConfirmationsForRequisition(this IQueryable<RequisitionStatusConfirmation> confirmations, int id)
            => confirmations.Include(c => c.ConfirmedByUser).Where(c => c.RequisitionId == id);

        #endregion

        #region lists connected to user, customer units

        public static IQueryable<UserNotificationSetting> GetNotificationSettingsForUser(this IQueryable<UserNotificationSetting> notificationSettinga, int userId)
            => notificationSettinga.Where(ns => ns.UserId == userId);

        public static IQueryable<UserDefaultSetting> GetDefaultSettingsForUser(this IQueryable<UserDefaultSetting> defaultSettings, int userId)
            => defaultSettings.Where(ds => ds.UserId == userId);

        public static IQueryable<UserDefaultSettingOrderRequirement> GetDefaultSettingOrderRequirementsForUser(this IQueryable<UserDefaultSettingOrderRequirement> defaultSettingOrderRequirements, int userId)
           => defaultSettingOrderRequirements.Where(ds => ds.UserId == userId);

        public static IQueryable<CustomerUnitUser> GetCustomerUnitsForUser(this IQueryable<CustomerUnitUser> customerUnits, int userId)
           => customerUnits
            .Include(u => u.CustomerUnit)
            .Where(cu => cu.UserId == userId);

        public static IQueryable<CustomerUnit> GetCustomerUnitsForCustomerOrganisation(this IQueryable<CustomerUnit> customerUnits, int? customerOrganisationId)
          => customerUnits.Where(cu => cu.CustomerOrganisationId == customerOrganisationId)
                .OrderByDescending(cu => cu.IsActive).ThenBy(cu => cu.Name);

        public static IQueryable<CustomerUnitUser> GetCustomerUnitsWithCustomerUnitForUser(this IQueryable<CustomerUnitUser> customerUnitUsers, int userId)
          => customerUnitUsers.Include(cuu => cuu.CustomerUnit)
                .Where(cuu => cuu.UserId == userId);

        public static IQueryable<CustomerUnit> GetCustomerUnitsForUser(this IQueryable<CustomerUnit> customerUnits, int userId)
          => customerUnits.Where(cu => cu.CustomerUnitUsers.Any(cuu => cuu.UserId == userId));

        public static IQueryable<CustomerUnitUser> GetUsersForCustomerUnit(this IQueryable<CustomerUnitUser> customerUnitUsers, int customerUnitId)
          => customerUnitUsers.Where(cu => cu.CustomerUnitId == customerUnitId).Include(cu => cu.User);

        public static async Task<IEnumerable<IdentityUserClaim<int>>> GetClaimsForUser(this IQueryable<IdentityUserClaim<int>> claims, int userId)
            => await claims.Where(u => u.UserId == userId).ToListAsync();

        public static IQueryable<IdentityUserRole<int>> GetRolesForUser(this IQueryable<IdentityUserRole<int>> roles, int userId)
          => roles.Where(u => u.UserId == userId);

        #endregion

        #region single entities by id      

        public static async Task<InterpreterBroker> GetInterpreterBrokerById(this IQueryable<InterpreterBroker> interpreterBrokers, int id)
            => await interpreterBrokers.Include(ib => ib.Interpreter).SingleAsync(i => i.InterpreterBrokerId == id);

        public static async Task<AspNetUser> GetUserById(this IQueryable<AspNetUser> users, int id)
            => await users.SingleOrDefaultAsync(u => u.Id == id);

        public static async Task<CustomerUnit> GetCustomerUnitById(this IQueryable<CustomerUnit> customerUnits, int id)
            => await customerUnits.Include(c => c.CreatedByUser).Include(c => c.InactivatedByUser)
                .Where(c => c.CustomerUnitId == id).SingleOrDefaultAsync();

        public static async Task<Attachment> GetNonConnectedAttachmentById(this IQueryable<Attachment> attachments, int id)
            => await attachments.Include(a => a.TemporaryAttachmentGroup).Where(a => a.AttachmentId == id &&
                !a.Orders.Any() && !a.Requests.Any() && !a.Requisitions.Any()
                && !a.OrderGroups.Any() && !a.RequestGroups.Any()).SingleOrDefaultAsync();

        public static async Task<RequestGroup> GetRequestGroupById(this IQueryable<RequestGroup> groups, int id)
            => await groups
                .Include(r => r.Ranking).ThenInclude(ra => ra.Broker)
                .Include(r => r.OrderGroup).ThenInclude(o => o.CustomerOrganisation)
                .SingleAsync(r => r.RequestGroupId == id);

        public static async Task<RequestGroup> GetRequestGroupToProcessById(this IQueryable<RequestGroup> groups, int id)
           => await groups
               .Include(r => r.Ranking).ThenInclude(ra => ra.Broker)
               .Include(r => r.Ranking).ThenInclude(ra => ra.FrameworkAgreement)
               .Include(r => r.OrderGroup).ThenInclude(o => o.CustomerUnit)
               .Include(r => r.OrderGroup).ThenInclude(o => o.CreatedByUser)
               .SingleAsync(r => r.RequestGroupId == id);

        public static async Task<RequestGroup> GetRequestGroupToView(this IQueryable<RequestGroup> groups, int id)
           => await groups.GetRequestGroupsWithUsers()
                .Include(g => g.Ranking).ThenInclude(r => r.FrameworkAgreement)
                .Include(g => g.OrderGroup).ThenInclude(o => o.CreatedByUser)
                .Include(g => g.OrderGroup).ThenInclude(o => o.Language)
                .Include(g => g.OrderGroup).ThenInclude(o => o.Region)
                .Include(g => g.OrderGroup).ThenInclude(o => o.CustomerOrganisation)
                .SingleAsync(r => r.RequestGroupId == id);

        public static async Task<OutboundEmail> GetEmailById(this IQueryable<OutboundEmail> emails, int id)
            => await emails.Include(e => e.ReplacedByEmail).SingleOrDefaultAsync(e => e.OutboundEmailId == id);

        public static async Task<OrderGroup> GetOrderGroupById(this IQueryable<OrderGroup> groups, int id)
            => await groups.SingleOrDefaultAsync(og => og.OrderGroupId == id);

        public static async Task<OrderGroup> GetOrderGroupWithContactsById(this IQueryable<OrderGroup> groups, int id)
           => await groups.Include(g => g.CreatedByUser).SingleOrDefaultAsync(og => og.OrderGroupId == id);

        public static async Task<OrderGroup> GetFullOrderGroupById(this IQueryable<OrderGroup> groups, int id)
           => await groups.Include(o => o.Language)
                .Include(o => o.Region)
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.CreatedByUser)
                .Include(o => o.CustomerUnit)
                .SingleOrDefaultAsync(og => og.OrderGroupId == id);

        public static async Task<OutboundWebHookCall> GetWebHookById(this IQueryable<OutboundWebHookCall> webhooks, int id)
            => await webhooks
                 .Include(wh => wh.RecipientUser).ThenInclude(u => u.Broker)
                 .Include(wh => wh.ReplacingWebHook)
                 .SingleOrDefaultAsync(wh => wh.OutboundWebHookCallId == id);

        public static async Task<OutboundPeppolMessage> GetPeppolMessageById(this IQueryable<OutboundPeppolMessage> peppolMessages, int id)
            => await peppolMessages
                 .Include(m => m.PeppolMessagePayload).ThenInclude(p => p.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                 .Include(wh => wh.ReplacingMessage)
                 .SingleOrDefaultAsync(wh => wh.OutboundPeppolMessageId == id);

        public static async Task<CustomerOrganisation> GetCustomerById(this IQueryable<CustomerOrganisation> customers, int id)
           => await customers
               .Include(c => c.ParentCustomerOrganisation)
               .SingleAsync(c => c.CustomerOrganisationId == id);

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
            => await orders.GetOrdersWithInclude().SingleOrDefaultAsync(o => o.OrderId == id);

        public static async Task<Order> GetOrderByOrderNumber(this IQueryable<Order> orders, string orderNumber)
            => await orders.SingleOrDefaultAsync(o => o.OrderNumber == orderNumber);

        public static async Task<Request> GetRequestForOrderAgreementCreation(this IQueryable<Request> requests, int orderId)
            => await requests
                    .Include(r => r.Order)
                    .Include(r => r.Requisitions)
                    .Include(r => r.PeppolPayloads)
                    .GetLastRequestForOrder(orderId);

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
                .Include(r => r.Ranking).ThenInclude(ra => ra.FrameworkAgreement)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.AnsweringUser)
                .Include(r => r.AcceptingUser)
                .SingleAsync(r => r.RequestId == id);

        public static async Task<Request> GetSimpleRequestById(this IQueryable<Request> requests, int id)
            => await requests
                .Include(r => r.Ranking)
                    .ThenInclude(r => r.Broker)
                .Include(r => r.Ranking)
                    .ThenInclude(ra => ra.FrameworkAgreement)
                .Include(r => r.Order)
                .SingleAsync(r => r.RequestId == id);

        public static async Task<Request> GetRequestForRequisitionCreateById(this IQueryable<Request> requests, int id)
           => await requests.GetRequestsWithBaseIncludes()
                   .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder)
                   .Include(r => r.Ranking).ThenInclude(ra => ra.FrameworkAgreement)
                   .SingleAsync(r => r.RequestId == id);

        public static async Task<Request> GetRequestForOtherViewsById(this IQueryable<Request> requests, int id)
              => await requests
                   .Include(r => r.Interpreter)
                   .Include(r => r.Order).ThenInclude(o => o.CreatedByUser)
                   .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                   .Include(r => r.Order).ThenInclude(o => o.Language)
                   .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                   .Include(r => r.Ranking).ThenInclude(r => r.Region)
                   .Include(r => r.Ranking).ThenInclude(r => r.FrameworkAgreement)
                   .SingleAsync(r => r.RequestId == id);

        public static async Task<Request> GetRequestForWebHook(this IQueryable<Request> requests, int id)
            => await requests.Include(r => r.Ranking)
               .Include(r => r.Order).ThenInclude(o => o.Language)
               .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
               .Include(r => r.Order).ThenInclude(o => o.Region)
               .Include(r => r.Order).ThenInclude(r => r.CustomerUnit)
               .SingleAsync(r => r.RequestId == id);

        public static async Task<Request> GetRequestForAcceptById(this IQueryable<Request> requests, int id)
            => await requests.GetRequestsWithBaseIncludes()
                    .Include(r => r.RequestGroup)
                    .Include(r => r.Order).ThenInclude(o => o.Language)
                    .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder)
                    .Include(r => r.Order).ThenInclude(o => o.ExtraInterpreterOrder)
                    .Include(r => r.Order).ThenInclude(o => o.IsExtraInterpreterForOrder)
                    .SingleAsync(r => r.RequestId == id);

        public static async Task<Request> GetRequestWithContactsById(this IQueryable<Request> requests, int id)
             => await requests.GetRequestsWithBaseIncludes().SingleAsync(r => r.RequestId == id);

        public static async Task<Request> GetRequestForChangeInterpreterWithBrokerAndOrderNumber(this IQueryable<Request> requests, string orderNumber, int brokerId)
           => await requests.GetRequestsWithBaseIncludesAndRegionAndLanguage()
                   .Include(r => r.RequestGroup)
                   .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder)
                   .Include(r => r.Order).ThenInclude(o => o.ExtraInterpreterOrder)
                   .Include(r => r.Order).ThenInclude(o => o.IsExtraInterpreterForOrder)
                   .SingleOrDefaultAsync(r => r.Order.OrderNumber == orderNumber &&
                   r.Ranking.BrokerId == brokerId && r.ReplacedByRequest == null);

        public static async Task<Order> GetOrderWithBrokerAndOrderNumber(this IQueryable<Order> orders, string orderNumber, int brokerId)
           => await orders
                .SingleOrDefaultAsync(o => o.OrderNumber == orderNumber && o.Requests.Any(r => r.Ranking.BrokerId == brokerId));

        public static async Task<Broker> GetBrokerByIdentifier(this IQueryable<Broker> brokers, string identifier)
           => await brokers
                .SingleOrDefaultAsync(b => b.OrganizationPrefix == identifier);

        public static async Task<Request> GetActiveRequestForApiWithBrokerAndOrderNumber(this IQueryable<Request> requests, string orderNumber, int brokerId)
             => await requests.GetRequestsWithBaseIncludesAndRegionAndLanguage()
                .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder)
                .SingleOrDefaultAsync(r => r.Order.OrderNumber == orderNumber &&
                    r.Ranking.BrokerId == brokerId && r.ReplacedByRequest == null);

        public static async Task<RequestGroup> GetRequestGroupForApiWithBrokerAndOrderNumber(this IQueryable<RequestGroup> requestGroups, string orderNumber, int brokerId)
           => await requestGroups.Include(r => r.OrderGroup)
              .Include(r => r.Ranking).SingleOrDefaultAsync(r => r.OrderGroup.OrderGroupNumber == orderNumber && r.Ranking.BrokerId == brokerId && r.ReplacedByRequestGroup == null);

        public static async Task<RequestGroup> GetFullRequestGroupForApiWithBrokerAndOrderNumber(this IQueryable<RequestGroup> requestGroups, string orderNumber, int brokerId)
            => await requestGroups.GetRequestGroupsWithBaseIncludes()
            .Include(r => r.OrderGroup.Language)
            .Include(r => r.OrderGroup.Region)
            .SingleOrDefaultAsync(r => r.OrderGroup.OrderGroupNumber == orderNumber && r.Ranking.BrokerId == brokerId && r.ReplacedByRequestGroup == null);

        public static async Task<Request> GetConfirmedRequestForApiWithBrokerAndOrderNumber(this IQueryable<Request> requests, string orderNumber, int brokerId, IEnumerable<RequestStatus> statuses)
            => await requests.GetRequestsWithBaseIncludesForApi()
                .SingleOrDefaultAsync(r => r.Order.OrderNumber == orderNumber &&
                    r.Ranking.BrokerId == brokerId && statuses.Contains(r.Status));

        public static async Task<Request> GetSimpleActiveRequestForApiWithBrokerAndOrderNumber(this IQueryable<Request> requests, string orderNumber, int brokerId)
           => await requests.GetRequestsWithBaseIncludesForApi()
               .SingleOrDefaultAsync(r => r.Order.OrderNumber == orderNumber &&
                   r.Ranking.BrokerId == brokerId && r.ReplacedByRequest == null);

        public static async Task<Request> GetActiveRequestByOrderId(this IQueryable<Request> requests, int orderId, bool includeNotAnsweredByCreator = true)
                    => await requests.GetActiveRequestsWithBaseIncludes()
                        .SingleOrDefaultAsync(r => r.OrderId == orderId &&
                            (includeNotAnsweredByCreator || r.Status != RequestStatus.ResponseNotAnsweredByCreator));

        public static async Task<FrameworkAgreement> GetFrameworkByOrderId(this IQueryable<Request> requests, int orderId)
            => await requests
                .Where(r => r.OrderId == orderId)
                .Select(r => r.Ranking.FrameworkAgreement)
                .FirstOrDefaultAsync();

        public static async Task<Request> GetActiveRequestIncludeProcessingUserByOrderId(this IQueryable<Request> requests, int orderId, bool includeNotAnsweredByCreator = true)
            => await requests.GetActiveRequestsWithBaseIncludes()
                .Include(r => r.ProcessingUser)
                .SingleOrDefaultAsync(r => r.OrderId == orderId &&
                    (includeNotAnsweredByCreator || r.Status != RequestStatus.ResponseNotAnsweredByCreator));
        public static async Task<RequestGroup> GetLastRequestGroupForOrderGroup(this IQueryable<RequestGroup> requestGroups, int orderGroupId)
        {
            return await requestGroups.GetRequestGroupsWithUsers().OrderBy(r => r.RequestGroupId).LastAsync(r => r.OrderGroupId == orderGroupId);
        }

        public static async Task<Complaint> GetFullComplaintById(this IQueryable<Complaint> complaints, int id)
                => await complaints.GetComplaintsWithBaseIncludes()
                .Include(r => r.Request).ThenInclude(r => r.Interpreter)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerUnit)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(ra => ra.FrameworkAgreement)
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
                .Include(c => c.Request).ThenInclude(r => r.Ranking).ThenInclude(ra => ra.FrameworkAgreement)
                .SingleOrDefaultAsync(c => c.Request.Order.OrderNumber == orderNumber && c.Request.Ranking.BrokerId == brokerId);

        public static async Task<AspNetUser> GetAPIUserForBroker(this IQueryable<AspNetUser> users, int brokerId)
            => await users
                .Include(u => u.Broker)
                .SingleOrDefaultAsync(u => u.IsApiUser && u.BrokerId == brokerId);

        public static async Task<AspNetUser> GetAPIUserForCustomer(this IQueryable<AspNetUser> users, int customerId)
            => await users.SingleOrDefaultAsync(u => u.IsApiUser && u.CustomerOrganisationId == customerId);

        public static async Task<CustomerUnitUser> GetCustomerUnitUserForUserAndCustomerUnit(this IQueryable<CustomerUnitUser> customerUnitUsers, int userId, int customerUnitId)
            => await customerUnitUsers.Include(cuu => cuu.CustomerUnit)
                    .Where(cu => cu.UserId == userId && cu.CustomerUnitId == customerUnitId).SingleAsync();

        public static async Task<AspNetUser> GetUserWithCustomerOrganisationById(this IQueryable<AspNetUser> users, int id)
            => await users.Include(u => u.CustomerOrganisation).SingleOrDefaultAsync(u => u.Id == id);

        public static async Task<AspNetUser> GetUserByIdWithTemporaryEmail(this IQueryable<AspNetUser> users, int id)
            => await users.Include(u => u.TemporaryChangedEmailEntry).SingleOrDefaultAsync(u => u.Id == id);

        public static async Task<OutboundWebHookCall> GetOutboundWebHookCall(this IQueryable<OutboundWebHookCall> outboundWebHookCalls, int id)
        => await outboundWebHookCalls.Include(c => c.RecipientUser).SingleOrDefaultAsync(c => c.OutboundWebHookCallId == id);

        public static async Task<OutboundPeppolMessage> GetOutboundPeppolMessage(this IQueryable<OutboundPeppolMessage> outboundPeppolMessages, int id)
        => await outboundPeppolMessages
            .Include(c => c.PeppolMessagePayload).ThenInclude(o => o.Request).ThenInclude(r => r.Order)
            .SingleOrDefaultAsync(c => c.OutboundPeppolMessageId == id);

        #endregion

        public static async Task<OrderAttachment> GetOrderAttachmentByAttachmentId(this IQueryable<OrderAttachment> attachments, int id)
            => await attachments.Include(a => a.Attachment).Include(a => a.Order)
            .Where(a => a.Attachment.AttachmentId == id).FirstOrDefaultAsync();

        public static async Task<RequestAttachment> GetRequestAttachmentByAttachmentId(this IQueryable<RequestAttachment> attachments, int id)
         => await attachments.Include(a => a.Attachment).Include(a => a.Request).ThenInclude(r => r.Ranking)
            .Include(a => a.Request).ThenInclude(r => r.Order)
            .Where(a => a.Attachment.AttachmentId == id).FirstOrDefaultAsync();

        public static async Task<RequisitionAttachment> GetRequisitionAttachmentByAttachmentId(this IQueryable<RequisitionAttachment> attachments, int id)
        => await attachments.Include(a => a.Attachment)
            .Include(a => a.Requisition).ThenInclude(r => r.Request).ThenInclude(r => r.Ranking)
            .Include(a => a.Requisition).ThenInclude(r => r.Request).ThenInclude(r => r.Order)
            .Where(a => a.Attachment.AttachmentId == id).FirstOrDefaultAsync();

        public static async Task<OrderGroupAttachment> GetOrderGroupAttachmentByAttachmentId(this IQueryable<OrderGroupAttachment> attachments, int id)
           => await attachments.Include(a => a.Attachment).Include(a => a.OrderGroup)
            .Where(a => a.Attachment.AttachmentId == id).FirstOrDefaultAsync();

        public static async Task<RequestGroupAttachment> GetRequestGroupAttachmentByAttachmentId(this IQueryable<RequestGroupAttachment> attachments, int id)
         => await attachments.Include(a => a.Attachment).Include(a => a.RequestGroup).ThenInclude(r => r.Ranking)
            .Include(a => a.RequestGroup).ThenInclude(r => r.OrderGroup)
            .Where(a => a.Attachment.AttachmentId == id).FirstOrDefaultAsync();

        public static async Task<CustomerOrganisation> GetParentOrganisationsByDomain(this IQueryable<CustomerOrganisation> customerOrganisations, string domain)
            => await customerOrganisations.SingleOrDefaultAsync(c => c.EmailDomain == domain && c.ParentCustomerOrganisationId == null);

        public static async Task<PeppolPayload> GetById(this IQueryable<PeppolPayload> payloads, int id)
           => await payloads
                .Include(p => p.Request).ThenInclude(r => r.Order)                
                .Where(p => p.PeppolPayloadId == id).SingleOrDefaultAsync();

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

        public static PriceInformationModel GetPriceInformationModel(this IEnumerable<PriceRowBase> priceRows, string competenceLevel)
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
                        CalculationBase = p.Count() == 1 ? p.Single()?.PriceCalculationCharge?.ChargePercentage : null,
                        CalculatedFrom = EnumHelper.Parent<PriceRowType, PriceRowType?>(p.Key)?.GetCustomName(),
                        PriceListRows = p.Where(l => l.PriceRowType == PriceRowType.InterpreterCompensation || l.PriceRowType == PriceRowType.BrokerFee).Select(l => new PriceRowListModel
                        {
                            PriceListRowType = l.PriceRowType == PriceRowType.BrokerFee ? PriceListRowType.BasePrice.GetCustomName() : l.PriceListRow.PriceListRowType.GetCustomName(),
                            Description = l.PriceRowType == PriceRowType.BrokerFee ? PriceListRowType.BasePrice.GetDescription() : l.PriceListRow.PriceListRowType.GetDescription(),
                            Price = l.Price,
                            Quantity = l.Quantity
                        })

                    })
            };
        }

        public static IQueryable<PriceCalculationCharge> GetPriceCalculationChargesByIds(this IQueryable<PriceCalculationCharge> priceCalculationCharges, List<int> priceCalculationChargesIds)
          => priceCalculationCharges.Where(x => priceCalculationChargesIds.Contains(x.PriceCalculationChargeId));

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
                .Include(r => r.Ranking).ThenInclude(r => r.FrameworkAgreement)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.CustomerUnit)
                .Include(r => r.Order.CreatedByUser)
                .Include(r => r.Order.ContactPersonUser);

        private static IQueryable<Request> GetRequestsWithBaseIncludesAndRegionAndLanguage(this IQueryable<Request> requests)
            => requests.GetRequestsWithBaseIncludes()
            .Include(r => r.Order.Language)
            .Include(r => r.Order.Region);

        private static IQueryable<Request> GetActiveRequestsWithBaseIncludes(this IQueryable<Request> requests)
           => requests
               .Include(r => r.Order)
               .Include(r => r.AnsweringUser)
               .Include(r => r.AcceptingUser)
               .Include(r => r.Interpreter)
               .Include(r => r.Ranking).ThenInclude(r => r.Broker)
               .Include(r => r.Ranking).ThenInclude(r => r.FrameworkAgreement)
            .Where(r => r.Status != RequestStatus.InterpreterReplaced &&
                        r.Status != RequestStatus.DeniedByTimeLimit &&
                        r.Status != RequestStatus.DeniedByCreator &&
                        r.Status != RequestStatus.DeclinedByBroker &&
                        r.Status != RequestStatus.LostDueToQuarantine &&
                        r.Status != RequestStatus.ReplacedAtAnswerAfterAccept &&
                        r.Status != RequestStatus.ReplacedAfterAcceptOfFlexible &&
                        r.Status != RequestStatus.ReplacedAfterPriceUpdate
            );

        private static IQueryable<RequestGroup> GetRequestGroupsWithBaseIncludes(this IQueryable<RequestGroup> requestGroups)
            => requestGroups
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Ranking).ThenInclude(ra => ra.FrameworkAgreement)
                .Include(r => r.OrderGroup).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.OrderGroup).ThenInclude(o => o.CustomerUnit)
                .Include(r => r.OrderGroup.CreatedByUser);

        private static IQueryable<RequestGroup> GetRequestGroupsWithUsers(this IQueryable<RequestGroup> requestGroups)
            => requestGroups
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Ranking).ThenInclude(r => r.FrameworkAgreement)
                .Include(r => r.AnsweringUser)
                .Include(r => r.AcceptingUser)
                .Include(r => r.ProcessingUser)
                .Include(r => r.CancelledByUser);

        private static IQueryable<Request> GetRequestsWithBaseIncludesForApi(this IQueryable<Request> requests)
                => requests
                .Include(r => r.Ranking).ThenInclude(r => r.FrameworkAgreement)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.ContactPersonUser)
                .Include(r => r.Order).ThenInclude(o => o.CreatedByUser)
                .Include(r => r.Order).ThenInclude(o => o.CustomerUnit);

        private static IQueryable<Requisition> GetRequisitionsWithBaseIncludes(this IQueryable<Requisition> requisitions)
                 => requisitions
                 .Include(r => r.CreatedByUser)
                 .Include(r => r.ProcessedUser)
                 .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                 .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                 .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.FrameworkAgreement);

        private static IQueryable<Complaint> GetComplaintsWithBaseIncludes(this IQueryable<Complaint> complaints)
            => complaints.Include(c => c.CreatedByUser)
                .Include(c => c.AnsweringUser)
                .Include(c => c.AnswerDisputingUser)
                .Include(c => c.TerminatingUser);

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

        public static IQueryable<Broker> GetAllBrokers(this IQueryable<Broker> brokers)
            => brokers.OrderBy(c => c.Name);

        public static IQueryable<CustomerOrganisation> GetAllCustomers(this IQueryable<CustomerOrganisation> customerOrganisations)
            => customerOrganisations.Include(c => c.ParentCustomerOrganisation).OrderBy(c => c.Name);

        public static IQueryable<CustomerOrganisation> GetSubOrganisationsByParent(this IQueryable<CustomerOrganisation> customerOrganisations, int parentOrganisationId)
            => customerOrganisations.Where(c => c.ParentCustomerOrganisationId == parentOrganisationId);

        public static IQueryable<FaqDisplayUserRole> GetAllFaqWithFaqDisplayUserRoles(this IQueryable<FaqDisplayUserRole> faqDisplayUserRoles)
            => faqDisplayUserRoles.Include(f => f.Faq);

        public static IQueryable<FaqDisplayUserRole> GetFaqWithFaqDisplayUserRolesByFaqId(this IQueryable<FaqDisplayUserRole> faqDisplayUserRoles, int id)
          => faqDisplayUserRoles.Include(f => f.Faq).Where(f => f.FaqId == id);

        public static IQueryable<FaqDisplayUserRole> GetFaqDisplayUserRolesByDisplayUserRoles(this IQueryable<FaqDisplayUserRole> faqDisplayUserRoles, IEnumerable<DisplayUserRole> displayUserRoles)
          => faqDisplayUserRoles.Where(f => displayUserRoles.Contains(f.DisplayUserRole));

        public static IQueryable<Faq> GetPublishedFaqWithFaqIds(this IQueryable<Faq> faqs, IQueryable<int> faqIds)
            => faqs.Where(f => faqIds.Contains(f.FaqId) && f.IsDisplayed);

        public static IQueryable<SystemMessage> GetAllSystemMessages(this IQueryable<SystemMessage> systemMessages)
            => systemMessages
            .Include(s => s.CreatedByUser)
            .Include(s => s.LastUpdatedByUser);

        public static async Task<SystemMessage> GetSystemMessageById(this IQueryable<SystemMessage> systemMessages, int id)
            => await systemMessages.Where(sm => sm.SystemMessageId == id).SingleOrDefaultAsync();

        #region reports

        public static IQueryable<Request> GetDeliveredRequestsWithOrders(this IQueryable<Request> requests, DateTime start, DateTime end, DateTime now, int? organisationId, IEnumerable<int> customerUnits, int? brokerid = null)
          => requests
            .Where(r =>
                (r.Status == RequestStatus.Approved || r.Status == RequestStatus.Delivered)
                && r.Order.EndAt <= now && r.Order.StartAt.Date >= start.Date && r.Order.StartAt.Date <= end.Date
                && (r.Order.Status == OrderStatus.Delivered || r.Order.Status == OrderStatus.DeliveryAccepted || r.Order.Status == OrderStatus.ResponseAccepted)
                && (organisationId.HasValue ? r.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                && (customerUnits == null || (r.Order.CustomerUnitId.HasValue && customerUnits.Contains(r.Order.CustomerUnitId.Value)))
                && (brokerid == null || r.Ranking.BrokerId == brokerid));

        public static IQueryable<OrderInterpreterLocation> GetInterpreterLocationsForDeliveredOrders(this IQueryable<OrderInterpreterLocation> interpreterLocations, DateTime start, DateTime end, DateTime now, int? organisationId, IEnumerable<int> customerUnits)
           => interpreterLocations.Where(x =>
               x.Order.EndAt <= now && x.Order.StartAt.Date >= start.Date && x.Order.StartAt.Date <= end.Date
               && (x.Order.Status == OrderStatus.Delivered || x.Order.Status == OrderStatus.DeliveryAccepted || x.Order.Status == OrderStatus.ResponseAccepted)
               && (organisationId.HasValue ? x.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
               && (customerUnits == null || (x.Order.CustomerUnitId.HasValue && customerUnits.Contains(x.Order.CustomerUnitId.Value))));

        public static IQueryable<OrderInterpreterLocation> GetInterpreterLocationsByOrderIds(this IQueryable<OrderInterpreterLocation> interpreterLocations, List<int> orderIds)
           => interpreterLocations.Where(x => orderIds.Contains(x.OrderId));

        public static IQueryable<OrderRequirement> GetOrderRequirementsForDeliveredOrders(this IQueryable<OrderRequirement> orderRequirements, DateTime start, DateTime end, DateTime now, int? organisationId, IEnumerable<int> customerUnits)
           => orderRequirements.Where(x =>
               x.Order.EndAt <= now && x.Order.StartAt.Date >= start.Date && x.Order.StartAt.Date <= end.Date
               && (x.Order.Status == OrderStatus.Delivered || x.Order.Status == OrderStatus.DeliveryAccepted || x.Order.Status == OrderStatus.ResponseAccepted)
               && (organisationId.HasValue ? x.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
               && (customerUnits == null || (x.Order.CustomerUnitId.HasValue && customerUnits.Contains(x.Order.CustomerUnitId.Value))));

        public static IQueryable<OrderRequirement> GetOrderRequirementsByOrderIds(this IQueryable<OrderRequirement> orderRequirements, List<int> orderIds)
           => orderRequirements.Where(x => orderIds.Contains(x.OrderId));

        public static IQueryable<OrderCompetenceRequirement> GetOrderCompetencesForDeliveredOrders(this IQueryable<OrderCompetenceRequirement> orderCompetenceRequirements, DateTime start, DateTime end, DateTime now, int? organisationId, IEnumerable<int> customerUnits)
           => orderCompetenceRequirements.Where(x =>
               x.Order.EndAt <= now && x.Order.StartAt.Date >= start.Date && x.Order.StartAt.Date <= end.Date
               && (x.Order.Status == OrderStatus.Delivered || x.Order.Status == OrderStatus.DeliveryAccepted || x.Order.Status == OrderStatus.ResponseAccepted)
               && (organisationId.HasValue ? x.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
               && (customerUnits == null || (x.Order.CustomerUnitId.HasValue && customerUnits.Contains(x.Order.CustomerUnitId.Value))));

        public static IQueryable<OrderCompetenceRequirement> GetOrderCompetencesByOrderIds(this IQueryable<OrderCompetenceRequirement> orderCompetenceRequirements, List<int> orderIds)
           => orderCompetenceRequirements.Where(x => orderIds.Contains(x.OrderId));

        public static IQueryable<OrderRequirementRequestAnswer> GetRequirementAnswersForDeliveredOrders(this IQueryable<OrderRequirementRequestAnswer> orderRequirementAnswers, DateTime start, DateTime end, DateTime now, int? organisationId, IEnumerable<int> customerUnits, int? brokerid = null)
           => orderRequirementAnswers.Where(x =>
                (x.Request.Status == RequestStatus.Approved || x.Request.Status == RequestStatus.Delivered)
                && x.Request.Order.EndAt <= now && x.Request.Order.StartAt.Date >= start.Date && x.Request.Order.StartAt.Date <= end.Date
                && (x.Request.Order.Status == OrderStatus.Delivered || x.Request.Order.Status == OrderStatus.DeliveryAccepted || x.Request.Order.Status == OrderStatus.ResponseAccepted)
                && (organisationId.HasValue ? x.Request.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                && (customerUnits == null || (x.Request.Order.CustomerUnitId.HasValue && customerUnits.Contains(x.Request.Order.CustomerUnitId.Value)))
                && (brokerid == null || x.Request.Ranking.BrokerId == brokerid));

        public static IQueryable<RequestPriceRow> GetRequestPriceRowsForDeliveredOrders(this IQueryable<RequestPriceRow> pricerows, DateTime start, DateTime end, DateTime now, int? organisationId, IEnumerable<int> customerUnits, int? brokerid = null)
           => pricerows.Where(x =>
                (x.Request.Status == RequestStatus.Approved || x.Request.Status == RequestStatus.Delivered)
                && x.Request.Order.EndAt <= now && x.Request.Order.StartAt.Date >= start.Date && x.Request.Order.StartAt.Date <= end.Date
                && (x.Request.Order.Status == OrderStatus.Delivered || x.Request.Order.Status == OrderStatus.DeliveryAccepted || x.Request.Order.Status == OrderStatus.ResponseAccepted)
                && (organisationId.HasValue ? x.Request.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                && (customerUnits == null || (x.Request.Order.CustomerUnitId.HasValue && customerUnits.Contains(x.Request.Order.CustomerUnitId.Value)))
                && (brokerid == null || x.Request.Ranking.BrokerId == brokerid));

        public static IQueryable<Requisition> GetRequisitionsForDeliveredOrders(this IQueryable<Requisition> requisitions, DateTime start, DateTime end, DateTime now, int? organisationId, IEnumerable<int> customerUnits, int? brokerid = null)
           => requisitions.Where(x =>
                (x.Request.Status == RequestStatus.Approved || x.Request.Status == RequestStatus.Delivered)
                && x.Request.Order.EndAt <= now && x.Request.Order.StartAt.Date >= start.Date && x.Request.Order.StartAt.Date <= end.Date
                && (x.Request.Order.Status == OrderStatus.Delivered || x.Request.Order.Status == OrderStatus.DeliveryAccepted || x.Request.Order.Status == OrderStatus.ResponseAccepted)
                && (organisationId.HasValue ? x.Request.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                && (customerUnits == null || (x.Request.Order.CustomerUnitId.HasValue && customerUnits.Contains(x.Request.Order.CustomerUnitId.Value)))
                && (brokerid == null || x.Request.Ranking.BrokerId == brokerid));

        public static IQueryable<Complaint> GetComplaintsForDeliveredOrders(this IQueryable<Complaint> complaints, DateTime start, DateTime end, DateTime now, int? organisationId, IEnumerable<int> customerUnits, int? brokerid = null)
           => complaints.Where(x =>
                (x.Request.Status == RequestStatus.Approved || x.Request.Status == RequestStatus.Delivered)
                && x.Request.Order.EndAt <= now && x.Request.Order.StartAt.Date >= start.Date && x.Request.Order.StartAt.Date <= end.Date
                && (x.Request.Order.Status == OrderStatus.Delivered || x.Request.Order.Status == OrderStatus.DeliveryAccepted || x.Request.Order.Status == OrderStatus.ResponseAccepted)
                && (organisationId.HasValue ? x.Request.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                && (customerUnits == null || (x.Request.Order.CustomerUnitId.HasValue && customerUnits.Contains(x.Request.Order.CustomerUnitId.Value)))
                && (brokerid == null || x.Request.Ranking.BrokerId == brokerid));

        public static IQueryable<Request> GetRequestsOrdersForReport(this IQueryable<Request> requests, DateTime start, DateTime end, int? organisationId, IEnumerable<int> customerUnits)
            => requests
                .Where(r => r.Order.CreatedAt.Date >= start.Date && r.Order.CreatedAt.Date <= end.Date
                 && (organisationId.HasValue ? r.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                 && (customerUnits == null || (r.Order.CustomerUnitId.HasValue && customerUnits.Contains(r.Order.CustomerUnitId.Value))));

        public static IQueryable<OrderInterpreterLocation> GetInterpreterLocationsForOrdersForReport(this IQueryable<OrderInterpreterLocation> interpreterLocations, DateTime start, DateTime end, int? organisationId, IEnumerable<int> customerUnits)
            => interpreterLocations.Where(x =>
               x.Order.CreatedAt.Date >= start.Date && x.Order.CreatedAt.Date <= end.Date
               && (organisationId.HasValue ? x.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
               && (customerUnits == null || (x.Order.CustomerUnitId.HasValue && customerUnits.Contains(x.Order.CustomerUnitId.Value))));

        public static IQueryable<OrderRequirement> GetOrderRequirementsForOrdersForReport(this IQueryable<OrderRequirement> orderRequirements, DateTime start, DateTime end, int? organisationId, IEnumerable<int> customerUnits)
           => orderRequirements.Where(x =>
               x.Order.CreatedAt.Date >= start.Date && x.Order.CreatedAt.Date <= end.Date
               && (organisationId.HasValue ? x.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
               && (customerUnits == null || (x.Order.CustomerUnitId.HasValue && customerUnits.Contains(x.Order.CustomerUnitId.Value))));

        public static IQueryable<OrderCompetenceRequirement> GetOrderCompetencesForOrdersForReport(this IQueryable<OrderCompetenceRequirement> orderCompetenceRequirements, DateTime start, DateTime end, int? organisationId, IEnumerable<int> customerUnits)
           => orderCompetenceRequirements.Where(x =>
               x.Order.CreatedAt.Date >= start.Date && x.Order.CreatedAt.Date <= end.Date
               && (organisationId.HasValue ? x.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
               && (customerUnits == null || (x.Order.CustomerUnitId.HasValue && customerUnits.Contains(x.Order.CustomerUnitId.Value))));

        public static IQueryable<OrderRequirementRequestAnswer> GetRequirementAnswersForOrdersForReport(this IQueryable<OrderRequirementRequestAnswer> orderRequirementAnswers, DateTime start, DateTime end, int? organisationId, IEnumerable<int> customerUnits)
           => orderRequirementAnswers.Where(x =>
                x.Request.Order.CreatedAt.Date >= start.Date && x.Request.Order.CreatedAt.Date <= end.Date
                && (organisationId.HasValue ? x.Request.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                && (customerUnits == null || (x.Request.Order.CustomerUnitId.HasValue && customerUnits.Contains(x.Request.Order.CustomerUnitId.Value))));

        public static IQueryable<Requisition> GetRequisitionsForOrdersForReport(this IQueryable<Requisition> requisitions, DateTime start, DateTime end, int? organisationId, IEnumerable<int> customerUnits)
           => requisitions.Where(x =>
                x.Request.Order.CreatedAt.Date >= start.Date && x.Request.Order.CreatedAt.Date <= end.Date
                && (organisationId.HasValue ? x.Request.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                && (customerUnits == null || (x.Request.Order.CustomerUnitId.HasValue && customerUnits.Contains(x.Request.Order.CustomerUnitId.Value))));

        public static IQueryable<Complaint> GetComplaintsForOrdersForReport(this IQueryable<Complaint> complaints, DateTime start, DateTime end, int? organisationId, IEnumerable<int> customerUnits)
           => complaints.Where(x =>
                x.Request.Order.CreatedAt.Date >= start.Date && x.Request.Order.CreatedAt.Date <= end.Date
                && (organisationId.HasValue ? x.Request.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                && (customerUnits == null || (x.Request.Order.CustomerUnitId.HasValue && customerUnits.Contains(x.Request.Order.CustomerUnitId.Value))));

        public static IQueryable<Request> GetRequestOrdersForBrokerReport(this IQueryable<Request> requests, DateTime start, DateTime end, int brokerId)
            => requests
                .Where(r => r.Ranking.BrokerId == brokerId && r.CreatedAt.Date >= start.Date && r.CreatedAt.Date <= end.Date
                && !(r.Status == RequestStatus.NoDeadlineFromCustomer || 
                     r.Status == RequestStatus.AwaitingDeadlineFromCustomer ||
                     r.Status == RequestStatus.InterpreterReplaced || 
                     r.Status == RequestStatus.ReplacedAtAnswerAfterAccept || 
                     r.Status == RequestStatus.ReplacedAfterAcceptOfFlexible || 
                     r.Status == RequestStatus.LostDueToQuarantine ||
                     r.Status == RequestStatus.ReplacedAfterPriceUpdate));

        public static IQueryable<OrderRequirementRequestAnswer> GetRequirementAnswersForBrokerReport(this IQueryable<OrderRequirementRequestAnswer> orderRequirementAnswers, DateTime start, DateTime end, int brokerId)
           => orderRequirementAnswers.Where(x =>
                x.Request.Ranking.BrokerId == brokerId && x.Request.CreatedAt.Date >= start.Date && x.Request.CreatedAt.Date <= end.Date
                && !(x.Request.Status == RequestStatus.NoDeadlineFromCustomer || 
                     x.Request.Status == RequestStatus.AwaitingDeadlineFromCustomer ||
                     x.Request.Status == RequestStatus.InterpreterReplaced || 
                     x.Request.Status == RequestStatus.ReplacedAtAnswerAfterAccept || 
                     x.Request.Status == RequestStatus.ReplacedAfterAcceptOfFlexible || 
                     x.Request.Status == RequestStatus.LostDueToQuarantine ||
                     x.Request.Status == RequestStatus.ReplacedAfterPriceUpdate));

        public static IQueryable<Requisition> GetRequisitionsForBrokerReport(this IQueryable<Requisition> requisitions, DateTime start, DateTime end, int brokerId)
           => requisitions.Where(x =>
                x.Request.Ranking.BrokerId == brokerId && x.Request.CreatedAt.Date >= start.Date && x.Request.CreatedAt.Date <= end.Date
                && !(x.Request.Status == RequestStatus.NoDeadlineFromCustomer ||
                     x.Request.Status == RequestStatus.AwaitingDeadlineFromCustomer ||
                     x.Request.Status == RequestStatus.InterpreterReplaced ||
                     x.Request.Status == RequestStatus.ReplacedAtAnswerAfterAccept ||
                     x.Request.Status == RequestStatus.ReplacedAfterAcceptOfFlexible ||
                     x.Request.Status == RequestStatus.LostDueToQuarantine ||
                     x.Request.Status == RequestStatus.ReplacedAfterPriceUpdate));

        public static IQueryable<Complaint> GetComplaintsForBrokerReport(this IQueryable<Complaint> complaints, DateTime start, DateTime end, int brokerId)
           => complaints.Where(x =>
                x.Request.Ranking.BrokerId == brokerId && x.Request.CreatedAt.Date >= start.Date && x.Request.CreatedAt.Date <= end.Date
                && !(x.Request.Status == RequestStatus.NoDeadlineFromCustomer ||
                     x.Request.Status == RequestStatus.AwaitingDeadlineFromCustomer ||
                     x.Request.Status == RequestStatus.InterpreterReplaced ||
                     x.Request.Status == RequestStatus.ReplacedAtAnswerAfterAccept ||
                     x.Request.Status == RequestStatus.ReplacedAfterAcceptOfFlexible ||
                     x.Request.Status == RequestStatus.LostDueToQuarantine ||
                     x.Request.Status == RequestStatus.ReplacedAfterPriceUpdate));

        public static IQueryable<Complaint> GetComplaintsForReports(this IQueryable<Complaint> complaints, DateTime start, DateTime end, int? organisationId, IEnumerable<int> customerUnits, int? brokerId)
           => complaints.Where(c =>
                c.CreatedAt.Date >= start.Date && c.CreatedAt.Date <= end.Date
                && (organisationId.HasValue ? c.Request.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                && (customerUnits == null || (c.Request.Order.CustomerUnitId.HasValue && customerUnits.Contains(c.Request.Order.CustomerUnitId.Value)))
                && (brokerId == null || c.Request.Ranking.BrokerId == brokerId))
                .Include(c => c.Request).ThenInclude(re => re.Order)
                .Include(c => c.Request).ThenInclude(re => re.Ranking).ThenInclude(ra => ra.FrameworkAgreement);

        public static IQueryable<Requisition> GetRequisitionsForReports(this IQueryable<Requisition> requisitions, DateTime start, DateTime end, int? organisationId, IEnumerable<int> customerUnits, int? brokerId)
           => requisitions.Where(r =>
                r.CreatedAt.Date >= start.Date && r.CreatedAt.Date <= end.Date && r.ReplacedByRequisitionId == null
                && (organisationId.HasValue ? r.Request.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                && (customerUnits == null || (r.Request.Order.CustomerUnitId.HasValue && customerUnits.Contains(r.Request.Order.CustomerUnitId.Value)))
                && (brokerId == null || r.Request.Ranking.BrokerId == brokerId))
                .Include(r => r.Request).ThenInclude(re => re.Order)
                .Include(r => r.Request).ThenInclude(re => re.Ranking).ThenInclude(ra => ra.FrameworkAgreement);

        public static IQueryable<RequisitionPriceRow> GetRequisitionPriceRowsForRequisitionReport(this IQueryable<RequisitionPriceRow> requisitionPriceRows, DateTime start, DateTime end, int? organisationId, IEnumerable<int> customerUnits, int? brokerId)
           => requisitionPriceRows.Where(r =>
                r.Requisition.CreatedAt.Date >= start.Date && r.Requisition.CreatedAt.Date <= end.Date && r.Requisition.ReplacedByRequisitionId == null
                && (organisationId.HasValue ? r.Requisition.Request.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                && (customerUnits == null || (r.Requisition.Request.Order.CustomerUnitId.HasValue && customerUnits.Contains(r.Requisition.Request.Order.CustomerUnitId.Value)))
                && (brokerId == null || r.Requisition.Request.Ranking.BrokerId == brokerId));

        public static IQueryable<MealBreak> GetMealBreaksForReport(this IQueryable<MealBreak> mealbreaks, DateTime start, DateTime end, int? organisationId, IEnumerable<int> customerUnits, int? brokerId)
           => mealbreaks.Where(r =>
                r.Requisition.CreatedAt.Date >= start.Date && r.Requisition.CreatedAt.Date <= end.Date && r.Requisition.ReplacedByRequisitionId == null
                && (organisationId.HasValue ? r.Requisition.Request.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                && (customerUnits == null || (r.Requisition.Request.Order.CustomerUnitId.HasValue && customerUnits.Contains(r.Requisition.Request.Order.CustomerUnitId.Value)))
                && (brokerId == null || r.Requisition.Request.Ranking.BrokerId == brokerId));

        public static IQueryable<RequestPriceRow> GetRequestPriceRowsForRequisitionReport(this IQueryable<RequestPriceRow> requestPriceRows, List<int> requestIds)
           => requestPriceRows.Where(r => requestIds.Contains(r.RequestId));

        #endregion

        public static async Task<Order> GetOrderWithLanguageByOrderId(this IQueryable<Order> orders, int id)
           => await orders.Include(o => o.Language).SingleOrDefaultAsync(o => o.OrderId == id);

        public static IQueryable<FailedWebHookCall> GetFailedWebhookCallsByWebHookId(this IQueryable<FailedWebHookCall> failedWebhookCalls, int id)
             => failedWebhookCalls.Where(f => f.OutboundWebHookCallId == id);

        public static IQueryable<FailedPeppolMessage> GetFailedPeppolMessagesByPeppolMessageId(this IQueryable<FailedPeppolMessage> failedPeppolMessages, int id)
             => failedPeppolMessages.Where(f => f.OutboundPeppolMessageId == id);

        public static IQueryable<Ranking> GetActiveRankings(this IQueryable<Ranking> rankings, DateTime now, int activeFramweorkAgreementId)
        => rankings
            .Include(r => r.Broker)
            .Include(r => r.Region)
            .Where(ra => ra.FirstValidDate <= now && ra.LastValidDate > now &&
                   ra.FrameworkAgreementId == activeFramweorkAgreementId);

        public static IQueryable<Region> GetRegionsWithActiveRankings(this IQueryable<Ranking> rankings, DateTime now, int activeFramweorkAgreementId)
            => rankings
                .GetActiveRankings(now, activeFramweorkAgreementId)
                .Select(r => r.Region)
                .Distinct();

        public static IQueryable<Ranking> GetLatestRankingsForFrameworkAgreement(this IQueryable<Ranking> rankings, int frameworkAgreementId, DateTime nowOrLastValid)
            => rankings
                .Include(r => r.Broker)
                .Include(r => r.Region).ThenInclude(re => re.RegionGroup)
                .Where(ra => ra.FrameworkAgreementId == frameworkAgreementId &&
                ra.FirstValidDate <= nowOrLastValid && ra.LastValidDate >= nowOrLastValid);

        public static async Task<Request> GetRequestToUpdateExpiryByOrderId(this IQueryable<Request> requests, int orderId)
            => await requests.GetRequestsWithBaseIncludesAndRegionAndLanguage()
                .SingleOrDefaultAsync(r => r.OrderId == orderId && r.Status == RequestStatus.AwaitingDeadlineFromCustomer);

        public static async Task<Request> GetRequestForNewRequestById(this IQueryable<Request> requests, int id)
            => await requests.GetRequestsWithBaseIncludesAndRegionAndLanguage()
            .SingleOrDefaultAsync(r => r.RequestId == id);

        public static async Task<RequestGroup> GetActiveRequestGroupByOrderGroupId(this IQueryable<RequestGroup> groups, int id)
            => await groups
            .Include(r => r.Ranking)
            .Include(r => r.OrderGroup).ThenInclude(og => og.CustomerOrganisation)
            .SingleOrDefaultAsync(r => r.OrderGroupId == id &&
                (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received || r.Status == RequestStatus.AcceptedAwaitingInterpreter));

        public static IQueryable<Ranking> GetActiveRankingsForRegion(this IQueryable<Ranking> rankings, int regionId, DateTime date)
            => rankings
            .Include(r => r.FrameworkAgreement)
            .Where(r => r.RegionId == regionId && r.FirstValidDate <= date && r.LastValidDate >= date);

        public static IQueryable<Quarantine> GetQuarantinesForRankings(this IQueryable<Quarantine> quarantines, IEnumerable<int> rankingIds)
            => quarantines.Where(q => rankingIds.Contains(q.RankingId));

        public static async Task<Request> GetRequestForInterpretertById(this IQueryable<Request> requests, int id)
            => await requests.GetRequestsWithBaseIncludes()
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder)
                .Include(r => r.Order).ThenInclude(o => o.ReplacedByOrder)
                .SingleAsync(r => r.RequestId == id);

        public static IQueryable<Request> GetRequestsForInterpreter(this IQueryable<Request> requests, int interpreterId)
            => requests.Include(r => r.Order).ThenInclude(o => o.Region)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Where(r => r.Interpreter.InterpreterId == interpreterId &&
                    (r.Status == RequestStatus.Approved || r.Status == RequestStatus.CancelledByBroker
                    || r.Status == RequestStatus.CancelledByCreator || r.Status == RequestStatus.CancelledByCreatorWhenApprovedOrAccepted));

        public static async Task<RequestGroup> GetRequestGroupForCreateById(this IQueryable<RequestGroup> groups, int id)
            => await groups.GetRequestGroupsWithBaseIncludes()
                .Include(g => g.OrderGroup).ThenInclude(o => o.Region)
                .Include(g => g.OrderGroup).ThenInclude(o => o.Language)
                .SingleAsync(r => r.RequestGroupId == id);

        public static async Task<Request> GetRequestWithInterpreterById(this IQueryable<Request> requests, int id)
            => await requests.Include(r => r.Interpreter).SingleAsync(r => r.RequestId == id);

        public static IEnumerable<PriceInformationBrokerFee> CurrentOrLastActiveBrokerFeesForAgreement(this IEnumerable<PriceInformationBrokerFee> brokerfees, CurrentOrLatestFrameworkAgreement frameworkAgreement, DateTime today)
        {
            return frameworkAgreement.IsActive ?
                brokerfees.Where(bf => bf.StartDate.Date <= today && bf.EndDate >= today) :
                brokerfees.Where(bf => bf.StartDate.Date <= today && bf.StartDate.Date <= frameworkAgreement.LastValidDate && bf.EndDate.Date >= frameworkAgreement.LastValidDate);
        }

        public static IEnumerable<BrokerFeeByRegionAndServiceType> CurrentOrLastActiveDistanceBrokerFeesForAgreement(this IEnumerable<BrokerFeeByRegionAndServiceType> brokerfees, CurrentOrLatestFrameworkAgreement frameworkAgreement, DateTime today)
                => brokerfees.Where(bf => bf.InterpreterLocation == InterpreterLocation.OffSiteVideo || bf.InterpreterLocation == InterpreterLocation.OffSitePhone).AsQueryable().GetBrokerFeesForFrameworkAgreement(frameworkAgreement,today);
     
        public static IEnumerable<BrokerFeeByRegionAndServiceType> CurrentOrLastActiveOnSiteBrokerFeesForAgreement(this IEnumerable<BrokerFeeByRegionAndServiceType> brokerfees, CurrentOrLatestFrameworkAgreement frameworkAgreement, DateTime today)
                => brokerfees.Where(bf => bf.InterpreterLocation == InterpreterLocation.OnSite || bf.InterpreterLocation == InterpreterLocation.OffSiteDesignatedLocation).AsQueryable().GetBrokerFeesForFrameworkAgreement(frameworkAgreement, today);

        private static IEnumerable<BrokerFeeByRegionAndServiceType> GetBrokerFeesForFrameworkAgreement(this IQueryable<BrokerFeeByRegionAndServiceType> brokerFeeQuery, CurrentOrLatestFrameworkAgreement frameworkAgreement, DateTime today)
        {
            return frameworkAgreement.IsActive ?
                brokerFeeQuery.Where(bf => bf.StartDate.Date <= today && bf.EndDate.Date >= today) :
                brokerFeeQuery.Where(bf => bf.StartDate.Date <= today && bf.StartDate.Date <= frameworkAgreement.LastValidDate && bf.EndDate >= frameworkAgreement.LastValidDate);
        }

        public static async Task<IEnumerable<Request>> GetActiveRequestWithPriceRowsToUpdate(this IQueryable<Request> requests)
            =>await requests
                 .Include(r => r.PriceRows)
                 .Include(r => r.Order)
                    .ThenInclude(o => o.CustomerOrganisation)
                 .Include(r => r.Ranking)
                    .ThenInclude(ra => ra.FrameworkAgreement)
                .Where(r => (r.PriceRows.Any(pr => pr.StartAt.Date > pr.PriceListRow.EndDate)) &&
                    r.Status != RequestStatus.InterpreterReplaced &&
                    r.Status != RequestStatus.DeniedByTimeLimit &&
                    r.Status != RequestStatus.DeniedByCreator &&
                    r.Status != RequestStatus.DeclinedByBroker &&
                    r.Status != RequestStatus.LostDueToQuarantine &&
                    r.Status != RequestStatus.ReplacedAtAnswerAfterAccept &&
                    r.Status != RequestStatus.ReplacedAfterAcceptOfFlexible &&
                    r.Status != RequestStatus.ReplacedAfterPriceUpdate &&
                    r.Status != RequestStatus.BrokerDeclinedReplacementWithTimeSlotOutsideOriginalRequestTimeSlot
                )
                 .ToListAsync();

        public static IQueryable<CustomerSpecificProperty> GetCustomerSpecificPropertiesWithCustomerOrganisation(this IQueryable<CustomerSpecificProperty> customerSpecificProperties, int customerOrganisationId, PropertyType propertyToReplace)
            => customerSpecificProperties
                .Where(csp => csp.CustomerOrganisationId == customerOrganisationId && csp.PropertyType == propertyToReplace)
                .Include(csp => csp.CustomerOrganisation)
                    .ThenInclude(co => co.CustomerChangeLogEntries);

    }
}
