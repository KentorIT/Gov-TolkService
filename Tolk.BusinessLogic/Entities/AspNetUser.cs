﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Tolk.BusinessLogic.Entities
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class AspNetUser : IdentityUser<int>
    {
        [MaxLength(255)]
        public string NameFirst { get; set; }

        [MaxLength(255)]
        public string NameFamily { get; set; }

        public string FullName { get => $"{NameFirst} {NameFamily}"; }

        public string CompleteContactInformation { get => $"{FullName}\n{Email}\nTel: {PhoneNumber ?? "-"}\nMobil: {PhoneNumberCellphone ?? "-"}"; }

        [StringLength(32)]
        public string PhoneNumberCellphone { get; set; }

        private AspNetUser() { }

        public AspNetUser(string email)
        {
            Email = email;
            UserName = email;
            IsActive = true;
        }

        public AspNetUser(string email, CustomerOrganisation customer)
            : this(email)
        {
            CustomerOrganisation = customer;
        }

        public AspNetUser(string email, Broker broker)
            : this(email)
        {
            Broker = broker;
        }

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

        #endregion
    }
}
