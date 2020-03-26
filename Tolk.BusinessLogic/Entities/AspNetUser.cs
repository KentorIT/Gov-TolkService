using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Tolk.BusinessLogic.Entities
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class AspNetUser : IdentityUser<int>
    {
        private AspNetUser() { }

        public AspNetUser(int id, string email, string userName, string firstName, string lastName, CustomerOrganisation customer)
                        : this(email, userName, firstName, lastName, customer)

        {
            Id = id;
        }

        public AspNetUser(int id, string email, string userName, string firstName, string lastName, Broker broker)
            : this(email, userName, firstName, lastName, broker)
        {
            Id = id;
        }
        public AspNetUser(string email, string userName, string firstName, string lastName)
        {
            Email = email;
            UserName = userName;
            NameFirst = firstName;
            NameFamily = lastName;
            IsActive = true;
        }

        public AspNetUser(string email, string userName, string firstName, string lastName, CustomerOrganisation customer)
            : this(email, userName, firstName, lastName)
        {
            CustomerOrganisation = customer;
            CustomerOrganisationId = customer?.CustomerOrganisationId;
        }

        public AspNetUser(string email, string userName, string firstName, string lastName, Broker broker)
            : this(email, userName, firstName, lastName)
        {
            Broker = broker;
        }

        [MaxLength(255)]
        public string NameFirst { get; set; }

        [MaxLength(255)]
        public string NameFamily { get; set; }

        public string FullName => $"{NameFirst} {NameFamily}";

        public string CompleteContactInformation => $"{FullName}\n{Email}\nTel: {PhoneNumber ?? "-"}\nMobil: {PhoneNumberCellphone ?? "-"}";

        public string PhoneNumbers => string.IsNullOrWhiteSpace(PhoneNumber) && string.IsNullOrWhiteSpace(PhoneNumberCellphone) ? null : $"Tel: {PhoneNumber ?? "-"}\nMobil: {PhoneNumberCellphone ?? "-"}";

        [StringLength(32)]
        public string PhoneNumberCellphone { get; set; }
        public List<IdentityUserRole<int>> Roles { get; set; }

        public List<IdentityUserClaim<int>> Claims { get; set; }

        [ForeignKey(nameof(BrokerId))]
        public Broker Broker { get; set; }

        public int? BrokerId { get; set; }

        [ForeignKey(nameof(CustomerOrganisationId))]
        public CustomerOrganisation CustomerOrganisation { get; set; }

        public int? CustomerOrganisationId { get; set; }

        public int? InterpreterId { get; set; }

        [ForeignKey(nameof(InterpreterId))]
        public Interpreter Interpreter { get; set; }

        public DateTimeOffset? LastLoginAt { get; set; }

        public bool IsActive { get; set; }

        public bool IsApiUser { get; set; }

        #region Navigation properties

        public List<UserNotificationSetting> NotificationSettings { get; set; }

        public List<UserAuditLogEntry> AuditLogEntries { get; set; }

        public TemporaryChangedEmailEntry TemporaryChangedEmailEntry { get; set; }

        public List<UserLoginLogEntry> LoginLogEntries { get; set; }

        public List<CustomerUnitUser> CustomerUnits { get; set; }

        public List<UserDefaultSetting> DefaultSettings { get; set; }

        public List<UserDefaultSettingOrderRequirement> DefaultSettingOrderRequirements { get; set; }

        [NotMapped]
        public IEnumerable<CustomerUnitUser> CustomerUnitsLocalAdmin => CustomerUnits.Where(cu => cu.IsLocalAdmin);

        #endregion
    }
}
