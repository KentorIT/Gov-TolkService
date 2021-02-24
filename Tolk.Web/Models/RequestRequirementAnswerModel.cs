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
    public class RequestRequirementAnswerModel : IModel
    {
        public int OrderRequirementId { get; set; }

        [NoDisplayName]
        public string Description { get; set; }

        [Display(Name = "Typ")]
        public RequirementType RequirementType { get; set; }

        [Display(Name = "Är ett krav")]
        public bool IsRequired { get; set; }

        [Display(Name = "Svar")]
        [SubItem]
        [StringLength(1000)]
        public string Answer { get; set; }

        [NoDisplayName]
        public bool CanMeetRequirement { get; set; }

        internal static async Task<List<RequestRequirementAnswerModel>> GetFromList(IQueryable<OrderRequirementRequestAnswer> answers)
        {
            return await answers.Select(r => new RequestRequirementAnswerModel
            {
                OrderRequirementId = r.OrderRequirementId,
                Description = r.OrderRequirement.Description,
                IsRequired = r.OrderRequirement.IsRequired,
                RequirementType = r.OrderRequirement.RequirementType,
                CanMeetRequirement = r.CanSatisfyRequirement,
                Answer = r.Answer
            }).ToListAsync();
        }
    }
}
