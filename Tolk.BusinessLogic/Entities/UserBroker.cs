using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Tolk.BusinessLogic.Entities
{
    public class UserBroker
    {
        [Key]
        public string UserId { get; set; }

        public AspNetUser User { get; set; }

        public int BrokerId { get; set; }

        public Broker Broker { get; set; }
    }
}
