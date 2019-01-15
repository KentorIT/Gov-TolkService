using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class AspNetUserRoleHistoryEntry
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AspNetUserRoleHistoryEntryId { get; set; }

        public int UserAuditLogEntryId { get; set; }

        public int RoleId { get; set; }

        [ForeignKey(nameof(UserAuditLogEntryId))]
        public UserAuditLogEntry UserAuditLogEntry { get; set; }

        [ForeignKey(nameof(RoleId))]
        public IdentityRole<int> Role { get; set; }
    }
}

