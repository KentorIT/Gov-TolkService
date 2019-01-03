using System;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

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

        public string ColorClassName { get => CssClassHelper.GetColorClassNameForComplaintStatus(Status); }

    }
}
