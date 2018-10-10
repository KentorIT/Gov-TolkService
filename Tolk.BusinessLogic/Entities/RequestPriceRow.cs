using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestPriceRow : PriceRowBase
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestPriceRowId { get; set; }

        public int RequestId { get; set; }

        [ForeignKey(nameof(RequestId))]
        public Request Request { get; set; }
    }
}
