using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class AspNetUserClaimHistoryEntry
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AspNetUserClaimHistoryEntryId { get; set; }

        public int UserAuditLogEntryId { get; set; }

        public int UserId { get; set; }

        public string ClaimType { get; set; }

        public string ClaimValue { get; set; }

        [ForeignKey(nameof(UserAuditLogEntryId))]
        public int UserAuditLogEntry { get; set; }

        [ForeignKey(nameof(UserId))]
        public AspNetUser User { get; set; }

    }
}

