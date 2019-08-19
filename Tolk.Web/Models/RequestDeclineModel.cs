using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class RequestDeclineModel
    {
        public int DeniedRequestId { get; set; }

        [DataType(DataType.MultilineText)]
        [Required]
        [StringLength(1000)]
        public string DenyMessage { get; set; }
    }
}
