using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Models
{

    public class ProcessRequestModel
    {
        public int OrderId { get; set; }
        public int RequestId { get; set; }
        public string DenyMessage { get; set; }
    }
}
