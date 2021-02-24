using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class OrderRequirementModel : IModel
    {
        public int? OrderRequirementId { get; set; }

        public int? UserDefaultSettingOrderRequirementId { get; set; }

        [Required]
        [NoDisplayName]
        [Display(Name = "Typ")]
        public RequirementType? RequirementType { get; set; }

        public bool RequirementIsRequired { get; set; }

        [Display(Name = "Beskrivning")]
        [Required]
        [StringLength(1000)]
        public string RequirementDescription { get; set; }

        [StringLength(1000)]
        public string Answer { get; set; }

        public bool? CanSatisfyRequirement { get; set; }

        [ClientRequired]
        [NoDisplayName]
        public Gender? Gender { get; set; }

        internal static async Task<List<OrderRequirementModel>> GetFromList(IQueryable<OrderRequirement> requirements)
            => await requirements.Select(r => new OrderRequirementModel
            {
                OrderRequirementId = r.OrderRequirementId,
                RequirementDescription = r.Description,
                RequirementIsRequired = r.IsRequired,
                RequirementType = r.RequirementType,
            }).ToListAsync();
    }
}
