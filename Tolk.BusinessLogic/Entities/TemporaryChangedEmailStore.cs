using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class TemporaryChangedEmailEntry
    {
        [Key]
        public int UserId { get; set; }

        //SET THIS TO USER ID ON CREATE IN MIGRATION!!!!!!
        public int UpdatedByUserId { get; set; }

        public int? ImpersonatingUpdatedByUserId { get; set; }

        [Required]
        public string EmailAddress { get; set; }

        public DateTimeOffset ExpirationDate { get; set; }

        #region foreign keys

        [ForeignKey(nameof(UserId))]
        public AspNetUser User { get; set; }

        [ForeignKey(nameof(UpdatedByUserId))]
        public AspNetUser UpdatedByUser { get; set; }

        [ForeignKey(nameof(ImpersonatingUpdatedByUserId))]
        public AspNetUser ImpersonatingUpdatedByUser { get; set; }

        #endregion
    }
}
