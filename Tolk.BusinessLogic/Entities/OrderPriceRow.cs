using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderPriceRow : PriceRowBase
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderPriceRowId { get; set; }

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }
    }
}
