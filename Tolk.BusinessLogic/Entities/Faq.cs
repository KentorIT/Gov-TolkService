using System;
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

        public List<FaqDisplayUserRole> FaqDisplayUserRole { get; set; }

    }
}
