using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;

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

        #endregion

        #region navigation

        public List<OrderRequirementRequestAnswer> RequirementAnswers { get; set; }

        #endregion

    }
}
