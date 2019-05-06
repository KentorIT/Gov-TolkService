using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class CustomerUnitUserHistoryEntry
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerUnitUserHistoryEntryId { get; set; }

        public int UserAuditLogEntryId { get; set; }

        [ForeignKey(nameof(UserAuditLogEntryId))]
        public UserAuditLogEntry UserAuditLogEntry { get; set; }

        public int CustomerUnitId { get; set; }

        public bool IsLocalAdmin { get; set; }
    }
}

