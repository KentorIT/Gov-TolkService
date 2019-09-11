using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class QuarantineHistoryEntry
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int QuarantineHistoryEntryId { get; set; }

        public int QuarantineId { get; set; }

        [ForeignKey(nameof(QuarantineId))]
        public Quarantine Quarantine { get; set; }

        public DateTimeOffset LoggedAt { get; set; }

        public int? UpdatedById { get; set; }

        [ForeignKey(nameof(UpdatedById))]
        public AspNetUser UpdatedByUser { get; set; }

        public DateTimeOffset ActiveFrom { get; set; }

        public DateTimeOffset ActiveTo { get; set; }

        [MaxLength(1024)]
        public string Motivation { get; set; }
    }
}
