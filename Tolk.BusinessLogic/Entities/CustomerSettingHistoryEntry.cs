using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class CustomerSettingHistoryEntry
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerSettingHistoryEntryId { get; set; }

        public int CustomerChangeLogEntryId { get; set; }

        public CustomerSettingType CustomerSettingType { get; set; }

        public bool Value { get; set; }

        [ForeignKey(nameof(CustomerChangeLogEntryId))]
        public CustomerChangeLogEntry CustomerChangeLogEntry { get; set; }
    }
}
