using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Tolk.BusinessLogic.Validation;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderGroup
    {
        private OrderGroup() { }

        public OrderGroup(AspNetUser createdByUser, AspNetUser createdByImpersonator, DateTimeOffset createdAt, IEnumerable<Order> orders)
        {
            //Verify that all orders have the same customer, region and language
            Validate.Ensure(orders.GroupBy(o => o.CustomerOrganisationId).Count() == 1, "A group cannot have orders connected to several customers.");
            Validate.Ensure(orders.GroupBy(o => o.LanguageId).Count() == 1, "A group cannot have orders connected to several languages.");
            Validate.Ensure(orders.GroupBy(o => o.RegionId).Count() == 1, "A group cannot have orders connected to several regions.");
            CreatedAt = createdAt;
            CreatedByUser = createdByUser;
            CreatedByImpersonator = createdByImpersonator;
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderGroupId { get; set; }

        [MaxLength(255)]
        [Required]
        public string OrderGroupNumber { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public int CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public AspNetUser CreatedByUser { get; set; }

        public int? ImpersonatingCreator { get; set; }

        [ForeignKey(nameof(ImpersonatingCreator))]
        public AspNetUser CreatedByImpersonator { get; set; }

        public List<Order> Orders { get; set; }
    }
}
