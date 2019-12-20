using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class EventLogModel
    {
        public IEnumerable<EventLogEntryModel> Entries { get; set; }
    }
}
