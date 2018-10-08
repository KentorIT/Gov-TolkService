using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Services
{
    public class EventLogService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;

        public EventLogService(
            TolkDbContext dbContext,
            ISwedishClock clock
            )
        {
            _dbContext = dbContext;
            _clock = clock;
        }

        public void Push(int objectId, ObjectType objectType, string eventDetails, string actor = "Systemet", string organization = null)
        {
            _dbContext.EventLog.Add(new EventLogEntry
            {
                ObjectId = objectId,
                ObjectType = objectType,
                Timestamp = _clock.SwedenNow,
                EventDetails = eventDetails,
                Actor = actor,
                Organization = organization,
            });
            _dbContext.SaveChanges();
        }

        public IEnumerable<EventLogEntry> GetLogs(Order order)
        {
            return _dbContext.EventLog
                .Where(e =>
                    (e.ObjectType == ObjectType.Order && e.ObjectId == order.OrderId)
                    || (e.ObjectType == ObjectType.Request && order.Requests.Any(r => r.RequestId == e.ObjectId))
                    || (e.ObjectType == ObjectType.Requisition && order.Requests.Any(r => r.Requisitions.Any(req => req.RequisitionId == e.ObjectId)))
                    || (e.ObjectType == ObjectType.Complaint && order.Requests.Any(r => r.Complaints.Any(c => c.ComplaintId == e.ObjectId)))
                    );
        }

        public IEnumerable<EventLogEntry> GetLogs(Request request)
        {
            return _dbContext.EventLog
                .Where(e => 
                    (e.ObjectType == ObjectType.Request && e.ObjectId == request.RequestId)
                    || (e.ObjectType == ObjectType.Requisition && request.Requisitions.Any(r => r.RequisitionId == e.ObjectId))
                    || (e.ObjectType == ObjectType.Complaint && request.Complaints.Any(c => c.ComplaintId == e.ObjectId)));
        }

        public IEnumerable<EventLogEntry> GetLogs(Requisition requisition)
        {
            return _dbContext.EventLog.Where(e => e.ObjectType == ObjectType.Requisition && e.ObjectId == requisition.RequisitionId);
        }

        public IEnumerable<EventLogEntry> GetLogs(Complaint complaint)
        {
            return _dbContext.EventLog.Where(e => e.ObjectType == ObjectType.Complaint && e.ObjectId == complaint.ComplaintId);
        }
    }
}
