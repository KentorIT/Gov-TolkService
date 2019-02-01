using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class TemporaryChangedEmailEntry
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string EmailAddress{ get; set; }

        public DateTimeOffset ExpirationDate { get; set; }

        #region foreign keys

        [ForeignKey(nameof(UserId))]
        public AspNetUser User { get; set; }

        #endregion
    }
}
