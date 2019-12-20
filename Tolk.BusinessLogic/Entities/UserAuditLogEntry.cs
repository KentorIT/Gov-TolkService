using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class UserAuditLogEntry
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserAuditLogEntryId { get; set; }

        public UserChangeType UserChangeType { get; set; }

        public int UserId { get; set; }

        public int? UpdatedByUserId { get; set; }

        public int? UpdatedByImpersonatorId { get; set; }

        public DateTimeOffset LoggedAt { get; set; }

        public AspNetUser User { get; set; }

        [ForeignKey(nameof(UpdatedByUserId))]
        public AspNetUser UpdatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedByImpersonatorId))]
        public AspNetUser UpdatedByImpersonatorUser { get; set; }

        public AspNetUserHistoryEntry UserHistory { get; set; }

        public List<AspNetUserRoleHistoryEntry> RolesHistory { get; set; }

        public List<AspNetUserClaimHistoryEntry> ClaimsHistory { get; set; }

        public List<UserNotificationSettingHistoryEntry> NotificationsHistory { get; set; }

        public List<CustomerUnitUserHistoryEntry> CustomerUnitUsersHistory { get; set; }
        public List<UserDefaultSettingHistoryEntry> DefaultsHistory { get; set; }
    }
}
