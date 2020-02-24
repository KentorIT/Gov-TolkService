using System;
using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class EventLogModel
    {
        public string Header { get; set; } = "Händelser";
        public string Id { get; set; } = "EventLog";
        public string DynamicLoadPath { get; set; }
        public IEnumerable<EventLogEntryModel> Entries { get; set; }
    }
}
