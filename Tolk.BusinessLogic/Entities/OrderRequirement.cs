using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderRequirement : OrderRequirementBase
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderRequirementId { get; set; }

        #region foreign keys

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }

        public int? OrderGroupRequirementId { get; set; }

        [ForeignKey(nameof(OrderGroupRequirementId))]
        public OrderGroupRequirement OrderGroupRequirement { get; set; }

        #endregion

        #region navigation

        public List<OrderRequirementRequestAnswer> RequirementAnswers { get; set; }

        #endregion

    }
}
