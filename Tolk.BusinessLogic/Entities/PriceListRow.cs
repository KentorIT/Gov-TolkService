using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

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

        public PriceRowType PriceRowType { get; set; }
    }
}
