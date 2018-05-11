using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Tolk.BusinessLogic.Entities
{
    public class Ranking
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RankingId { get; set; }

        public int Rank { get; set; }

        public decimal BrokerFee { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        #region foreign keys

        public int BrokerRegionId { get; set; }

        [ForeignKey(nameof(BrokerRegionId))]
        public BrokerRegion BrokerRegion { get; set; }

        #endregion
    }
}
