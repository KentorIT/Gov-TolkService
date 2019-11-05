using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class RequestGroupDeclineModel
    {
        public int DeniedRequestGroupId { get; set; }

        [DataType(DataType.MultilineText)]
        [Required]
        [StringLength(1000)]
        public string DenyMessage { get; set; }
    }
}
