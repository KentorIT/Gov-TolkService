using System;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class BrokerListModel
    {
        public string BrokerName { get; set; }

        public RequestStatus Status {get;set;} 

        public string DenyMessage { get; set; }
    }
}