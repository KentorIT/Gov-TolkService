using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class CustomerSpecificPropertyHistoryEntry
    {        
        public int CustomerSpecificPropertyHistoryEntryId { get; set; }
        [Required]
        public string DisplayName { get; set; }
        [Required]
        public string DisplayDescription { get; set; }
        [Required]
        public string InputPlaceholder { get; set; }
        public bool Required { get; set; }
        public bool RemoteValidation { get; set; }
        public string RegexPattern { get; set; }
        public string RegexErrorMessage { get; set; }
        public int? MaxLength { get; set; }
        public int CustomerOrganisationId { get; set; }
        public PropertyType PropertyType { get; set; }
        public bool Enabled { get; set; }        
        public int UpdatedByUserId { get; set; }
        public int CustomerChangeLogEntryId { get; set; }

        #region foreign keys
        [ForeignKey(nameof(CustomerOrganisationId))]
        public CustomerOrganisation CustomerOrganisation { get; set; }

        [ForeignKey(nameof(CustomerChangeLogEntryId))]
        public CustomerChangeLogEntry CustomerChangeLogEntry { get; set; }
        #endregion

    }
}
