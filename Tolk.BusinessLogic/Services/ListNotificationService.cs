using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Models.Notification;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class ListNotificationService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<NotificationService> _logger;
        private readonly ISwedishClock _clock;

        public ListNotificationService(
            TolkDbContext dbContext,
            ILogger<NotificationService> logger,
            ISwedishClock clock
        )
        {
            _dbContext = dbContext;
            _logger = logger;
            _clock = clock;
        }

        public async Task Archive(int brokerId, DateTime archiveToDate, int userId, int? impersonatorId, NotificationType notificationToArchive)
        {
            var now = _clock.SwedenNow;
            var list = GetStartListNotificationsFor(notificationToArchive, brokerId);
            RequestStatusConfirmation[] c = null;
            //TODO: verify the different filter dates!
            switch (notificationToArchive)
            {
                case NotificationType.RequestAnswerDenied:
                    c = list.Where(n => n.AnswerProcessedAt < archiveToDate)
                    .Select(n => GetRequestStatusConfirmation(userId, impersonatorId, n.RequestId.Value, n.RequestStatus.Value, now)).ToArray();
                    break;
                case NotificationType.RequestLostDueToNoAnswerFromCustomer:
                    c = list.Where(n => n.StartAt < archiveToDate)
                    .Select(n => GetRequestStatusConfirmation(userId, impersonatorId, n.RequestId.Value, n.RequestStatus.Value, now)).ToArray();
                    break;
                case NotificationType.RequestCancelledByCustomerWhenApproved:
                    c = list.Where(n => n.CancelledAt < archiveToDate)
                    .Select(n => GetRequestStatusConfirmation(userId, impersonatorId, n.RequestId.Value, n.RequestStatus.Value, now)).ToArray();
                    break;
                case NotificationType.RequestAssignmentTimePassed:
                    var requests = list.Where(n => (n.RespondedStartAt ?? n.StartAt) < archiveToDate).Select(n => new { RequestId = n.RequestId.Value, RequestStatus = n.RequestStatus.Value });
                    c = requests.Select(n => 
                        GetRequestStatusConfirmation(userId, impersonatorId, n.RequestId, n.RequestStatus, now)).ToArray();

                    //TODO: VALIDATE THE CREATED SQL, IT MUST NOT CREATE ITERATIVE CALLS TO DB!!!
                    _dbContext.Requests.Where(r => requests.Select(r => r.RequestId).Contains(r.RequestId)).ToList()
                        .ForEach(r => r.Status = RequestStatus.Delivered);

                    var orderIds = await _dbContext.Requests.Where(r => requests.Select(r => r.RequestId).Contains(r.RequestId)).Select(r => r.OrderId).ToListAsync();
                    _dbContext.Orders.Where(o => orderIds.Contains(o.OrderId)).ToList()
                        .ForEach(r => r.Status = OrderStatus.Delivered);
                    break;
                case NotificationType.RequestGroupAnswerDenied:
                    {
                        var status = RequestStatus.DeniedByCreator;
                        var groups = list.Where(n => n.AnswerProcessedAt < archiveToDate).Select(n => n.RequestGroupId).ToList();
                        c = GetFromGroups(userId, impersonatorId, now, status, groups);
                        await _dbContext.AddRangeAsync(groups
                            .Select(id => GetRequestGroupStatusConfirmation(userId, impersonatorId, id, status, now)).ToArray());
                    }
                    break;
                case NotificationType.RequestGroupLostDueToNotFullyAnswered:
                    {
                        var status = RequestStatus.ResponseNotAnsweredByCreator;
                        var groups = list.Where(n => n.StartAt < archiveToDate).Select(n => n.RequestGroupId).ToList();
                        c = GetFromGroups(userId, impersonatorId, now, status, groups);
                        await _dbContext.AddRangeAsync(groups
                            .Select(id => GetRequestGroupStatusConfirmation(userId, impersonatorId, id, status, now)).ToArray());
                    }
                    break;
                default:
                    throw new NotSupportedException($"{notificationToArchive.GetDescription()} is not a archivable {nameof(NotificationType)}.");
            }
            await _dbContext.AddRangeAsync(c);
        }

        private RequestStatusConfirmation[] GetFromGroups(int userId, int? impersonatorId, DateTimeOffset now, RequestStatus status, List<int?> groups)
        {
            return _dbContext.Requests.Where(r => groups.Contains(r.RequestGroupId) &&
                !r.RequestStatusConfirmations.Any(rs => rs.RequestStatus == status) &&
                r.Status == status)
                .Select(n => GetRequestStatusConfirmation(userId, impersonatorId, n.RequestId, status, now)).ToArray();
        }

        //TODO:MAKE CONSTRUCTOR!!
        private static RequestGroupStatusConfirmation GetRequestGroupStatusConfirmation(int userId, int? impersonatorId, int? id, RequestStatus status, DateTimeOffset now)
        {
            return new RequestGroupStatusConfirmation
            {
                RequestGroupId = id.Value,
                ConfirmedBy = userId,
                ImpersonatingConfirmedBy = impersonatorId,
                RequestStatus = status,
                ConfirmedAt = now
            };
        }

        //TODO:MAKE CONSTRUCTOR!!
        private static RequestStatusConfirmation GetRequestStatusConfirmation(int userId, int? impersonatorId, int requestId, RequestStatus status, DateTimeOffset now)
        {
            return new RequestStatusConfirmation
            {
                RequestId = requestId,
                ConfirmedBy = userId,
                ImpersonatingConfirmedBy = impersonatorId,
                RequestStatus = status,
                ConfirmedAt = now
            };
        }

        public IEnumerable<NotificationDisplayModel> GetAllArchivableNotificationsForBroker(int brokerId)
        {
            foreach (var type in GetNotificationTypes(NotificationConsumerType.BrokerStartPage))
            {
                yield return new NotificationDisplayModel
                {
                    NotificationType = type,
                    Count = GetStartListNotificationsFor(type, brokerId).Count()
                };
            }
        }

        public IQueryable<BrokerStartListRow> GetStartListNotificationsFor(NotificationType type, int brokerId)
        {
            return type switch
            {
                NotificationType.RequestAnswerDenied => _dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                    .Where(r => r.RowType == StartListRowType.Request &&
                        r.RequestGroupId == null &&
                        r.RequestStatus == RequestStatus.DeniedByCreator),
                NotificationType.RequestLostDueToNoAnswerFromCustomer => _dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                    .Where(r => r.RowType == StartListRowType.Request &&
                        r.RequestGroupId == null &&
                        r.RequestStatus == RequestStatus.ResponseNotAnsweredByCreator),
                NotificationType.RequestAssignmentTimePassed => _dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                    .Where(r => r.RowType == StartListRowType.Request &&
                        r.RequestStatus == RequestStatus.Approved &&
                        (r.RespondedStartAt ?? r.StartAt) < _clock.SwedenNow),
                NotificationType.RequestCancelledByCustomerWhenApproved => _dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                    .Where(r => r.RowType == StartListRowType.Request && r.RequestStatus == RequestStatus.CancelledByCreatorWhenApprovedOrAccepted && (r.AnsweredAt.HasValue || !r.RequestGroupId.HasValue)),
                NotificationType.RequestGroupLostDueToNotFullyAnswered => _dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                    .Where(rg => rg.RowType == StartListRowType.RequestGroup && rg.RequestGroupStatus == RequestStatus.ResponseNotAnsweredByCreator),
                NotificationType.RequestGroupAnswerDenied => _dbContext.BrokerStartListRows.BrokerStartListRows(brokerId)
                    .Where(rg => rg.RowType == StartListRowType.RequestGroup && rg.RequestGroupStatus == RequestStatus.DeniedByCreator),
                _ => throw new NotSupportedException($"{type.GetDescription()} is not a archivable {nameof(NotificationType)}.")
            };
        }

        private IEnumerable<NotificationType> GetNotificationTypes(NotificationConsumerType? consumer = null)
        {
            var list = Enum.GetValues(typeof(NotificationType)).OfType<NotificationType>();
            if (consumer.HasValue)
            {
                list = list.Where(t => EnumHelper.GetAvailableNotificationConsumerTypes(t).Contains(consumer.Value));
            }
            return list;
        }
    }
}
