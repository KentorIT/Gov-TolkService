using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderCompetenceRequirement : OrderCompetenceRequirementBase
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderCompetenceRequirementId { get; set; }

        #region foreign keys

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }

        #endregion
    }
}
