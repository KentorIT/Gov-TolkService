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

        [Column(TypeName = "date")]
        public DateTime FirstValidDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime LastValidDate { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal BrokerFee { get; set; }

        #region foreign keys

        public int BrokerId { get; set; }

        public int RegionId { get; set; }

        [ForeignKey(nameof(BrokerId))]
        public Broker Broker { get; set; }

        [ForeignKey(nameof(RegionId))]
        public Region Region { get; set; }

        #endregion

        #region Navigation properties

        public List<Request> Requests { get; set; }

        public List<Quarantine> Quarantines { get; set; }

        #endregion
    }
}
