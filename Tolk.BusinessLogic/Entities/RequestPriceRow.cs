using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestPriceRow
    {
        public int RequestId { get; set; }

        public int PriceListRowId { get; set; }

        public DateTimeOffset StartAt { get; set; }

        public DateTimeOffset EndAt { get; set; }

        [Column(TypeName = "decimal(10, 2)")]

        public decimal TotalPrice { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        [ForeignKey(nameof(RequestId))]
        public Request Request { get; set; }

        [ForeignKey(nameof(PriceListRowId))]
        public PriceListRow PriceListRow { get; set; }
    }
}
