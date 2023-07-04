using System;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class BrokerFeeByServiceTypePriceListRow
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BrokerFeeByServiceTypePriceListRowId { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal Price { get; set; }

        public CompetenceLevel CompetenceLevel { get; set; }

        public InterpreterLocation InterpreterLocation { get; set; }

        [Column(TypeName = "date")]
        public DateTime FirstValidDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime LastValidDate { get; set; }        

        public int RegionId;

        [ForeignKey(nameof(RegionId))]
        public Region Region { get; set; }
    }
}
