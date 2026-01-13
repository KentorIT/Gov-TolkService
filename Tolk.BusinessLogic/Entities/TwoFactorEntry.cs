using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class TwoFactorEntry
    {
        public int UserId { get; set; }

        [MaxLength(100)]
        public string DeviceId { get; set; }

        [MaxLength(2000)]
        public string StateInformation { get; set; }

        #region foreign keys

        [ForeignKey(nameof(UserId))]
        public AspNetUser User { get; set; }

        #endregion
    }
}
