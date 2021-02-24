using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class ConnectUserUnitModel : IModel
    {

        [Required]
        public int? ConnectUserId { get; set; }

        public bool IsLocalAdministrator { get; set; }

        public int CustomerUnitId { get; set; }
    }
}
