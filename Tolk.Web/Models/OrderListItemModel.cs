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
    public class OrderListItemModel
    {
        public string Action { get; set; }

        public int OrderId { get; set; }

        public OrderStatus Status { get; set; }

        public string CreatorName { get; set; } 

        public string BrokerName { get; set; }

        public string OrderNumber { get; set; }

        public string RegionName { get; set; }

        public string Language { get; set; }

        public DateTimeOffset Start { get; set; }

        public DateTimeOffset End { get; set; }
    }
}
