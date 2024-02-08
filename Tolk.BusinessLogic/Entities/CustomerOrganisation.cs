using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class CustomerOrganisation
    {
        public int CustomerOrganisationId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public List<AspNetUser> Users { get; set; }

        public PriceListType PriceListType { get; set; }

        [MaxLength(50)]
        public string EmailDomain { get; set; }

        public int? ParentCustomerOrganisationId { get; set; }

        [ForeignKey(nameof(ParentCustomerOrganisationId))]
        [InverseProperty(nameof(SubCustomerOrganisations))]
        public CustomerOrganisation ParentCustomerOrganisation { get; set; }

        [InverseProperty(nameof(ParentCustomerOrganisation))]
        public List<CustomerOrganisation> SubCustomerOrganisations { get; set; }

        [MaxLength(8)]
        public string OrganisationPrefix { get; set; }

        [Required]
        [MaxLength(32)]
        public string OrganisationNumber { get; set; }

        [MaxLength(50)]
        [Required]
        public string PeppolId { get; set; }

        [Required]
        public TravelCostAgreementType TravelCostAgreementType { get; set; }

        public List<CustomerSetting> CustomerSettings { get; set; } = new List<CustomerSetting>();

        public DateTime? UseOrderAgreementsFromDate { get; set; }

        public List<CustomerSpecificProperty> CustomerSpecificProperties { get; set; }

        public List<CustomerChangeLogEntry> CustomerChangeLogEntries { get; set; } = new List<CustomerChangeLogEntry>();
        public List<CustomerOrderAgreementSettings> CustomerOrderAgreementSettings { get; set; }

        public void UpdateCustomerSettingsAndHistory(DateTimeOffset changedAt, int userId, IEnumerable<CustomerSetting> updatedCustomerSettings)
        {
            var customerSettings = CustomerSettings.Select(c => new CustomerSettingHistoryEntry { CustomerSettingType = c.CustomerSettingType, Value = c.Value });
            CustomerChangeLogEntries.Add(new CustomerChangeLogEntry
            {
                LoggedAt = changedAt,
                UpdatedByUserId = userId,
                CustomerChangeLogType = CustomerChangeLogType.Settings,
                CustomerSettingHistories = customerSettings.ToList(),
                CustomerOrganisationHistoryEntry = new CustomerOrganisationHistoryEntry(this)
            });
            foreach (CustomerSetting cs in updatedCustomerSettings.ToList())
            {
                CustomerSettings.SingleOrDefault(c => c.CustomerSettingType == cs.CustomerSettingType).Value = cs.Value;
            }
        }

        public void UpdateCustomerSpecificPropertySettings(DateTimeOffset changedAt, int userId, CustomerSpecificProperty updatedCustomerSpecificProperty)
        {
            var customerSpecificProperties = CustomerSpecificProperties.Select(c => new CustomerSpecificPropertyHistoryEntry
            {
                DisplayName = c.DisplayName,
                DisplayDescription = c.DisplayDescription,
                InputPlaceholder = c.InputPlaceholder,
                Required = c.Required,
                RemoteValidation = c.RemoteValidation,
                RegexPattern = c.RegexPattern,
                RegexErrorMessage = c.RegexErrorMessage,
                MaxLength = c.MaxLength,
                CustomerOrganisationId = c.CustomerOrganisationId,
                PropertyType = c.PropertyType,
                Enabled = c.Enabled,                
            });
            CustomerChangeLogEntries.Add(new CustomerChangeLogEntry
            {
                LoggedAt = changedAt,
                UpdatedByUserId = userId,
                CustomerChangeLogType = CustomerChangeLogType.CustomerSpecificProperty,
                CustomerSpecificPropertyHistories = customerSpecificProperties.ToList(),
                CustomerOrganisationHistoryEntry = new CustomerOrganisationHistoryEntry(this)
            });

            var propertyToUpdate = CustomerSpecificProperties.SingleOrDefault(c => c.PropertyType == updatedCustomerSpecificProperty.PropertyType);
            propertyToUpdate.DisplayName = updatedCustomerSpecificProperty.DisplayName;
            propertyToUpdate.DisplayDescription = updatedCustomerSpecificProperty.DisplayDescription;
            propertyToUpdate.InputPlaceholder = updatedCustomerSpecificProperty.InputPlaceholder;
            propertyToUpdate.Required = updatedCustomerSpecificProperty.Required;
            propertyToUpdate.RemoteValidation = updatedCustomerSpecificProperty.RemoteValidation;
            propertyToUpdate.RegexPattern = updatedCustomerSpecificProperty.RegexPattern;
            propertyToUpdate.RegexErrorMessage = updatedCustomerSpecificProperty.RegexErrorMessage;
            propertyToUpdate.MaxLength = updatedCustomerSpecificProperty.MaxLength;
            propertyToUpdate.CustomerOrganisationId = updatedCustomerSpecificProperty.CustomerOrganisationId;
            propertyToUpdate.PropertyType = updatedCustomerSpecificProperty.PropertyType;
            propertyToUpdate.Enabled = updatedCustomerSpecificProperty.Enabled;
        }

        public void UpdateCustomerOrderAgreementBrokerSettings(DateTimeOffset changedAt, int userId)
        {
            var customerOrderAgreementSettings = CustomerOrderAgreementSettings.Select(c => new CustomerOrderAgreementSettingsHistoryEntry
            {
                EnabledAt = c.EnabledAt,
                BrokerId = c.BrokerId,
            });
            CustomerChangeLogEntries.Add(new CustomerChangeLogEntry
            {
                LoggedAt = changedAt,
                UpdatedByUserId = userId,
                CustomerChangeLogType = CustomerChangeLogType.CustomerOrderAgreementBrokerSettings,
                CustomerOrderAgreementSettingsHistoryEntry = customerOrderAgreementSettings.ToList(),
                CustomerOrganisationHistoryEntry = new CustomerOrganisationHistoryEntry(this)
            });
        }
    }
}
