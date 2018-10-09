using System;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class PriceListRow
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int? PriceListRowId { get; set; }

        public PriceListType PriceListType { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int MaxMinutes { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal Price { get; set; }

        public CompetenceLevel CompetenceLevel { get; set; }

        public PriceListRowType PriceListRowType { get; set; }
    }
}
