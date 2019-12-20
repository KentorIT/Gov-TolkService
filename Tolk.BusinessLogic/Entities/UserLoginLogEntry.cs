using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class UserLoginLogEntry
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserLoginLogEntryId { get; set; }

        public int UserId { get; set; }

        public DateTimeOffset LoggedInAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public AspNetUser User { get; set; }
    }
}
