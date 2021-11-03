using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class CustomerChangeLogEntry
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerChangeLogEntryId { get; set; }

        public CustomerChangeLogType CustomerChangeLogType { get; set; }

        public int UpdatedByUserId { get; set; }

        public int CustomerOrganisationId { get; set; }

        public DateTimeOffset LoggedAt { get; set; }

        [ForeignKey(nameof(UpdatedByUserId))]
        public AspNetUser UpdatedByUser { get; set; }

        [ForeignKey(nameof(CustomerOrganisationId))]
        public CustomerOrganisation CustomerOrganisation { get; set; }

        public List<CustomerSettingHistoryEntry> CustomerSettingHistories { get; set; }

        public CustomerOrganisationHistoryEntry CustomerOrganisationHistoryEntry { get; set; }
    }
}
