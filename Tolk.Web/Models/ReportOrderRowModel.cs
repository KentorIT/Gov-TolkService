using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Models
{
    public class ReportOrderRowModel : ReportRowModel
    {
        public string ReferenceNumber { get; set; }

        public string UnitName { get; set; }

        public string BrokerName { get; set; }

        public string CreatedBy { get; set; }

    }
}
