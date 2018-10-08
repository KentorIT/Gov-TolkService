using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Entities;

namespace Tolk.Web.Models
{
    public class EventLogModel
    {
        public List<EventLogEntryModel> Entries { get; set; }

        public static EventLogModel GetModel(IEnumerable<EventLogEntry> entries)
        {
            return new EventLogModel
            {
                Entries = entries.Select(e => new EventLogEntryModel
                {
                    EventLogEntryId = e.EventLogEntryId,
                    Timestamp = e.Timestamp,
                    EventDetails = e.EventDetails,
                    Actor = e.Actor,
                    Organization = e.Organization,
                })
                .OrderBy(e => e.Timestamp)
                .ToList(),
            };
        }
    }
}
