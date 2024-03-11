using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tolk.Api.Payloads.WebHookPayloads;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
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

        public void Archive(DateTime archiveToDate, int v1, int? v2, IEnumerable<NotificationType> notificationsToArchive)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<NotificationDisplayModel> GetAllArchivableNotificationsForBroker(int brokerId)
        {
            foreach (var type in GetNotificationTypes(NotificationConsumerType.BrokerStartPage))
            {
                //TODO: Make this a for each on Notificationtypes with a new NotificationConsumerType 
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
                        (r.RespondedStartAt ?? r.StartAt) < _clock.SwedenNow)

            };
        }
        public IEnumerable<NotificationType> GetNotificationTypes(NotificationConsumerType? consumer = null)
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
