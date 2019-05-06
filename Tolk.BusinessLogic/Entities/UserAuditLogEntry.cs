using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class UserAuditLogEntry
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserAuditLogEntryId { get; set; }

        public UserChangeType UserChangeType { get;set;}

        public int UserId { get; set; }

        public int? UpdatedByUserId { get; set; }

        public DateTimeOffset LoggedAt { get; set; }

        public AspNetUser User { get; set; }

        [ForeignKey(nameof(UpdatedByUserId))]
        public AspNetUser UpdatedByUser { get; set; }

        public AspNetUserHistoryEntry UserHistory { get; set; }

        public List<AspNetUserRoleHistoryEntry> RolesHistory { get; set; }

        public List<AspNetUserClaimHistoryEntry> ClaimsHistory { get; set; }

        public List<UserNotificationSettingHistoryEntry> NotificationsHistory { get; set; }

        public List<CustomerUnitUserHistoryEntry> CustomerunitUsersHistory { get; set; }
    }
}
