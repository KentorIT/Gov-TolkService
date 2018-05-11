using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Tolk.BusinessLogic.Entities
{
    public class BrokerRegion
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int BrokerRegionId { get; set; }

        #region foreign keys

        public int RegionId { get; set; }

        [ForeignKey(nameof(RegionId))]
        public Region Region { get; set; }

        public int BrokerId { get; set; }

        [ForeignKey(nameof(BrokerId))]
        public Broker Broker { get; set; }

        public Ranking Ranking { get; set; }

        #endregion
    }
}
