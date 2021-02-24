using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class CancelOrderGroupModel : IModel
    {
        public int OrderGroupId { get; set; }

        [StringLength(1000)]
        public string CancelMessage { get; set; }
    }
}
