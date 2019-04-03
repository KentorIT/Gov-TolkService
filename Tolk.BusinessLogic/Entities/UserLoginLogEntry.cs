using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Enums;

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
