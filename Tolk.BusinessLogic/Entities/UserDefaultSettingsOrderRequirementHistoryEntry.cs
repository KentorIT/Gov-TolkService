using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class UserDefaultSettingsOrderRequirementHistoryEntry
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserDefaultSettingsOrderRequirementHistoryEntryId { get; set; }

        public int UserAuditLogEntryId { get; set; }

        public RequirementType RequirementType { get; set; }

        public string Description { get; set; }

        public bool IsRequired { get; set; }

        [ForeignKey(nameof(UserAuditLogEntryId))]
        public UserAuditLogEntry UserAuditLogEntry { get; set; }
    }
}
