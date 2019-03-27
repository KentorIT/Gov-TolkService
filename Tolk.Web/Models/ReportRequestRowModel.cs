using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Models
{
    public class ReportRequestRowModel : ReportRowModel
    {

        public string CustomerName { get; set; }

        public string AnsweredBy { get; set; }
    }
}
