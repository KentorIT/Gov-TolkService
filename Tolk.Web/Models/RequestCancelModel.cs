using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class RequestCancelModel
    {

        public int RequestId { get; set; }

        [DataType(DataType.MultilineText)]
        [Required]
        [StringLength(1000)]
        public string CancelMessage { get; set; }
    }
}
