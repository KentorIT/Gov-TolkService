using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class OrderRequirementModel
    {
        public int? OrderRequirementId { get; set; }

        [Required]
        [Display(Name = "Typ av behov")]
        public RequirementType? RequirementType { get; set; }

        [Display(Name = "Är ett krav")]
        public bool RequirementIsRequired { get; set; }

        [Display(Name = "Beskrivning")]
        [Required]
        [StringLength(1000)]
        public string RequirementDescription { get; set; }

        #region methods

        public OrderRequirement UpdateOrderRequirement(OrderRequirement orderRequirement)
        {
            orderRequirement.Description = RequirementDescription;
            orderRequirement.RequirementType = RequirementType.Value;
            orderRequirement.IsRequired = RequirementIsRequired;
            return orderRequirement;
        }

        public static OrderRequirementModel GetModelFromOrderRequirement(OrderRequirement orderRequirement)
        {
            return new OrderRequirementModel
            {
                OrderRequirementId = orderRequirement.OrderRequirementId,
                RequirementDescription = orderRequirement.Description,
                RequirementIsRequired = orderRequirement.IsRequired,
                RequirementType = orderRequirement.RequirementType
            };

        }

        #endregion
    }
}
