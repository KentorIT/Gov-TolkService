using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestPriceRow : Utilities.PriceRowBase
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestPriceRowId { get; set; }

        public int RequestId { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        [ForeignKey(nameof(RequestId))]
        public Request Request { get; set; }
    }
}
