using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class RequisitionDenyModel
    {
        public int RequisitionId { get; set; }

        public string DenyMessage { get; set; }
    }
}
