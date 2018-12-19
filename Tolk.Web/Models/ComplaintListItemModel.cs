using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class ComplaintListItemModel
    {
        public string Controller { get; set; }

        public int OrderRequestId { get; set; }

        public ComplaintStatus Status { get; set; }

        public string OrderNumber { get; set; }

        public string RegionName { get; set; }
 
        public string CustomerName { get; set; }
   
        public string BrokerName { get; set; }

        public ComplaintType ComplaintType { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
