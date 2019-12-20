using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderRequirementRequestAnswer
    {
        [MaxLength(1000)]
        public string Answer { get; set; }

        public bool CanSatisfyRequirement { get; set; }

        #region foreign keys
        public int OrderRequirementId { get; set; }

        [ForeignKey(nameof(OrderRequirementId))]
        public OrderRequirement OrderRequirement { get; set; }

        public int RequestId { get; set; }

        [ForeignKey(nameof(RequestId))]
        public Request Request { get; set; }

        #endregion
    }
}
