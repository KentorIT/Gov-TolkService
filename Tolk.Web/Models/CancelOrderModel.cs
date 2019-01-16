using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class CancelOrderModel
    {
        public int OrderId { get; set; }

        [StringLength(1000)]
        public string CancelMessage { get; set; }

        public bool AddReplacementOrder { get; set; } = false;
    }
}
