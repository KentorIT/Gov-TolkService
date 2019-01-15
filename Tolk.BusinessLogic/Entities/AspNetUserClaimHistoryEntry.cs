using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class AspNetUserClaimHistoryEntry
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AspNetUserClaimHistoryEntryId { get; set; }

        public int UserAuditLogEntryId { get; set; }

        public string ClaimType { get; set; }

        public string ClaimValue { get; set; }

        [ForeignKey(nameof(UserAuditLogEntryId))]
        public UserAuditLogEntry UserAuditLogEntry { get; set; }

    }
}

