using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Tolk.BusinessLogic.Entities
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class AspNetUserHistoryEntry
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AspNetUserHistoryEntryId { get; set; }

        public int UserAuditLogEntryId { get; set; }

        public int UserId { get; set; }

        [MaxLength(255)]
        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        [MaxLength(255)]
        public string NameFirst { get; set; }

        [MaxLength(255)]
        public string NameFamily { get; set; }

        [StringLength(32)]
        public string PhoneNumberCellphone { get; set; }

        public bool IsActive { get; set; }

        public bool IsApiUser { get; set; }

        [ForeignKey(nameof(UserAuditLogEntryId))]
        public int UserAuditLogEntry { get; set; }

        [ForeignKey(nameof(UserId))]
        public AspNetUser User { get; set; }
    }
}
