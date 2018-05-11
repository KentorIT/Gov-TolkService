using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;

namespace Tolk.Web.Models
{
    public class RequestListItemModel
    {
        public int RequestId { get; set; }

        public RequestStatus Status { get; set; }

        public string OrderNumber { get; set; }

        public string RegionName { get; set; }

        public string CustomerName { get; set; }

        public string Language { get; set; }

        public DateTimeOffset Start { get; set; }

        public DateTimeOffset End { get; set; }
    }
}
