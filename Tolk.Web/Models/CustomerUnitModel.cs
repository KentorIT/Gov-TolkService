using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tolk.Web.Helpers;
using Tolk.BusinessLogic.Entities;

namespace Tolk.Web.Models
{
    public class CustomerUnitModel
    {
        public int CustomerUnitId { get; set; }

        public int CustomerId { get; set; }

        [Display(Name = "Namn")]
        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Felaktig e-postadress")]
        [RegularExpression(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*@((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$", ErrorMessage = "Felaktig e-postadress")]
        [Display(Name = "E-postadress")]
        [StringLength(255)]
        public string Email { get; set; }

        [Display(Name = "Lokal administratör")]
        [Required]
        public int LocalAdministrator { get; set; }

        [Display(Name = "Skapad")]
        public DateTimeOffset CreatedAt { get; set; }

        [Display(Name = "Skapad av")]
        public string CreatedBy { get; set; }

        [Display(Name = "Aktiv")]
        public bool IsActive { get; set; }

        [Display(Name = "Inkativerad")]
        public DateTimeOffset? InactivatedAt { get; set; }

        [Display(Name = "Inaktiverad av")]
        public string InactivatedBy { get; set; }

        public bool IsCentralAdministrator { get; set; }

        public IEnumerable<DynamicUserListItemModel> UnitUsers { get; set; }

        public string ErrorMessage { get; set; }

        public string Message { get; set; }

        [ClientRequired]
        [Display(Name = "Användare att koppla till enhet")]
        public int? ConnectUserId { get; set; }

        [Display(Name = "Sätt som lokal administratör")]
        public bool IsLocalAdministrator { get; set; }

        public UserPageMode UserPageMode { get; set; }

        internal static CustomerUnitModel GetModelFromCustomerUnit(CustomerUnit unit, bool isCentralAdmin = false)
        {
            return new CustomerUnitModel
            {
                CustomerUnitId = unit.CustomerUnitId,
                Name = unit.Name,
                Email = unit.Email,
                CreatedAt = unit.CreatedAt,
                CreatedBy = unit.CreatedByUser.FullName,
                IsActive = unit.IsActive,
                InactivatedAt = unit.InactivatedAt,
                InactivatedBy = unit.InactivatedByUser?.FullName ?? string.Empty,
                CustomerId = unit.CustomerOrganisationId,
                IsCentralAdministrator = isCentralAdmin
            };

        }

    }
}
