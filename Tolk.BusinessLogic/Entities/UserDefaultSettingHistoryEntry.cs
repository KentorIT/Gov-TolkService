using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class UserDefaultSettingHistoryEntry
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserDefaultSettingHistoryEntryId { get; set; }

        public int UserAuditLogEntryId { get; set; }

        public DefaultSettingsType DefaultSettingType { get; set; }

        public string Value { get; set; }

        [ForeignKey(nameof(UserAuditLogEntryId))]
        public UserAuditLogEntry UserAuditLogEntry { get; set; }
    }
}

