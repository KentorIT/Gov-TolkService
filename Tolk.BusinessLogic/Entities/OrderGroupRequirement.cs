using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderGroupRequirement : OrderRequirementBase
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderGroupRequirementId { get; set; }

        public int OrderGroupId { get; set; }

        [ForeignKey(nameof(OrderGroupId))]
        public OrderGroup OrderGroup { get; set; }
    }
}
