using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

        public void Push(int objectId, ObjectType objectType, string eventDetails, string actor, string organization = null)
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
    }
}
