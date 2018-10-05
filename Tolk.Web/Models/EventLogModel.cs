using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Models
{
    public class EventLogModel
    {
        public IEnumerable<EventLogEntryModel> Entries { get; set; }
    }
}
