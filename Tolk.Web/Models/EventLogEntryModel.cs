using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class EventLogEntryModel
    {
        // Weight to order events that happen simultaneously
        // Larger weight is ordered after smaller weight
        public int Weight { get; set; } = 100;

        public DateTimeOffset Timestamp { get; set; }

        public string EventDetails { get; set; }

        public string Actor { get; set; }

        public string Organization { get; set; }
    }
}
