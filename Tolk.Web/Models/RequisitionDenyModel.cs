using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class RequisitionDenyModel
    {
        public int RequisitionId { get; set; }

        [StringLength(255)]
        public string DenyMessage { get; set; }
    }
}
