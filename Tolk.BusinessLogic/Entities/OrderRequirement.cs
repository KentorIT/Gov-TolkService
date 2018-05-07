using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderRequirement
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderRequirementId { get; set; }

        //TODO: Make Enum and fk
        public int RequirementType { get; set; }

        [MaxLength(100)]
        public string Description { get; set; }

        #region foreign keys

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }

        #endregion
    }
}
