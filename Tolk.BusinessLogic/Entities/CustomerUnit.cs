using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Tolk.BusinessLogic.Entities
{
    public class CustomerUnit
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerUnitId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public int CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public AspNetUser CreatedByUser { get; set; }

        public int CustomerOrganisationId { get; set; }

        [ForeignKey(nameof(CustomerOrganisationId))]
        public CustomerOrganisation CustomerOrganisation { get; set; }

        public int? ImpersonatingCreator { get; set; }

        [ForeignKey(nameof(ImpersonatingCreator))]
        public AspNetUser CreatedByImpersonator { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset? InactivatedAt { get; set; }

        public int? InactivatedBy { get; set; }

        [ForeignKey(nameof(InactivatedBy))]
        public AspNetUser InactivatedByUser { get; set; }

        public int? ImpersonatingInactivatedBy { get; set; }

        [ForeignKey(nameof(ImpersonatingInactivatedBy))]
        public AspNetUser InactivatedByImpersonator { get; set; }

        public List<CustomerUnitUser> CustomerUnitUsers { get; set; }

        public void Create(DateTimeOffset swedenNow, int userId, int? impersonatorId, int customerOrganisationId, string name, string email, int localAdministratorId)
        {
            List<CustomerUnitUser> customerUnitUser = new List<CustomerUnitUser>
            {
                new CustomerUnitUser { IsLocalAdmin = true, UserId = localAdministratorId }
            };
            CreatedAt = swedenNow;
            CreatedBy = userId;
            ImpersonatingCreator = impersonatorId;
            CustomerOrganisationId = customerOrganisationId;
            IsActive = true;
            Name = name;
            Email = email;
            CustomerUnitUsers = customerUnitUser;
        }

        public void Update(DateTimeOffset swedenNow, int userId, int? impersonatorId, string name, string email, bool isActive)
        {
            if (!isActive && IsActive)
            {
                InactivatedAt = swedenNow;
                InactivatedBy = userId;
                ImpersonatingInactivatedBy = impersonatorId;
            }
            IsActive = isActive;
            Name = name;
            Email = email;
        }
    }
}