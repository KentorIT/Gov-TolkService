using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class EventLogEntryModel
    {
        public int EventLogEntryId { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string EventDetails { get; set; }

        public string Actor { get; set; }

        public string Organization { get; set; }
    }
}
