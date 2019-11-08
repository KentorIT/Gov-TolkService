using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Tolk.BusinessLogic.Enums;

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
