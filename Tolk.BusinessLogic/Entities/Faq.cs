using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class Faq
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FaqId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public int CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public AspNetUser CreatedByUser { get; set; }

        [MaxLength(255)]
        public string Question { get; set; }

        [MaxLength(2000)]
        public string Answer { get; set; }

        public bool IsDisplayed { get; set; }

        public int? LastUpdatedBy { get; set; }

        [ForeignKey(nameof(LastUpdatedBy))]
        public AspNetUser LastUpdatedByUser { get; set; }

        public DateTimeOffset? LastUpdatedAt { get; set; }

        public List<FaqDisplayUserRole> FaqDisplayUserRoles { get; set; }

        public void Create(DateTimeOffset swedenNow, int userId, bool isDisplayed, string question, string answer, IEnumerable<DisplayUserRole> displayedForUserRole)
        {
            CreatedAt = swedenNow;
            CreatedBy = userId;
            IsDisplayed = isDisplayed;
            Question = question;
            Answer = answer;
            FaqDisplayUserRoles = displayedForUserRole.Select(r => new FaqDisplayUserRole
            {
                DisplayUserRole = r
            }).ToList();

        }

    }
}
