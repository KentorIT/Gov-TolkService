using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class CustomerUnitUser
    {

        public int CustomerUnitId { get; set; }

        [ForeignKey(nameof(CustomerUnitId))]
        public CustomerUnit CustomerUnit { get; set; }

        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public AspNetUser User { get; set; }

        public bool IsLocalAdmin { get; set; }

    }
}

