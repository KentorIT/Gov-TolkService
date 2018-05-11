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

        [Column(TypeName = "decimal(5, 2)")]
        public decimal BrokerFee { get; set; }

        #region foreign keys

        public int BrokerRegionId { get; set; }

        [ForeignKey(nameof(BrokerRegionId))]
        public BrokerRegion BrokerRegion { get; set; }

        #endregion

        #region Navigation properties

        public List<Request> Requests { get; set; }

        #endregion
    }
}
