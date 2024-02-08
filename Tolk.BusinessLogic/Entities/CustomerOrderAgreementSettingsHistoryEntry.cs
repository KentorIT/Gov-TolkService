using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class CustomerOrderAgreementSettingsHistoryEntry
    {        
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerOrderAgreementSettingsHistoryEntryId { get; set; }     
        public int CustomerChangeLogEntryId { get; set; }
        public int BrokerId { get; set; }
        public DateTimeOffset? EnabledAt { get; set; }

        #region foreign keys
        [ForeignKey(nameof(CustomerChangeLogEntryId))]
        public CustomerChangeLogEntry CustomerChangeLogEntry { get; set; }
        #endregion
    }
}
