using System;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class PriceCalculationCharge
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public int PriceCalculationChargeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal Charge { get; set; }

        public ChargeType ChargeTypeId { get; set; }

    }
}
