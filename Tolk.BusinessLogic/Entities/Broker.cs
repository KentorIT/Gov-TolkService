using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Tolk.BusinessLogic.Entities
{
    public class Broker
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int BrokerId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        #region navigation properites

        public List<BrokerRegion> BrokerRegions { get; set; }

        public List<UserBroker> BrokerUsers { get; set; }

        #endregion
    }
}
