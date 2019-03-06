using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class CommentRequisitionModel
    {
        public int RequisitionId { get; set; }

        [StringLength(255)]
        public string CustomerComment { get; set; }
    }
}
