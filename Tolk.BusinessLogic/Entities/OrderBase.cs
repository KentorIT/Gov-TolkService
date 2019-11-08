using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.BusinessLogic.Validation;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderBase
    {
        public DateTimeOffset CreatedAt { get; set; }

        public int CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public AspNetUser CreatedByUser { get; set; }

        public virtual OrderStatus Status{get;set;}

        public int CustomerOrganisationId { get; set; }

        [ForeignKey(nameof(CustomerOrganisationId))]
        public CustomerOrganisation CustomerOrganisation { get; set; }

        public int? CustomerUnitId { get; set; }

        [ForeignKey(nameof(CustomerUnitId))]
        public CustomerUnit CustomerUnit { get; set; }

        public int RegionId { get; set; }

        [ForeignKey(nameof(RegionId))]
        public Region Region { get; set; }

        public int? LanguageId { get; set; }

        [ForeignKey(nameof(LanguageId))]
        public Language Language { get; set; }

        [MaxLength(255)]
        public string OtherLanguage { get; set; }

        public bool LanguageHasAuthorizedInterpreter { get; set; }

        public AssignmentType AssignmentType { get; set; }

        public bool SpecificCompetenceLevelRequired { get; set; }

        public AllowExceedingTravelCost? AllowExceedingTravelCost { get; set; }

        public int? ImpersonatingCreator { get; set; }

        [ForeignKey(nameof(ImpersonatingCreator))]
        public AspNetUser CreatedByImpersonator { get; set; }

        public bool IsAuthorizedAsCreator(IEnumerable<int> customerUnits, int? customerOrganisationId, int userId, bool hasCorrectAdminRole = false)
        {
            return HasCorrectAdminRoleForCustomer(customerOrganisationId, hasCorrectAdminRole)
                || CreatedByUserWithoutUnit(customerOrganisationId, userId)
                || CreatedByUsersUnit(customerUnits);
        }

        public bool IsAuthorizedAsCreatorOrContact(IEnumerable<int> customerUnits, int? customerOrganisationId, int userId, bool hasCorrectAdminRole = false)
        {
            return IsAuthorizedAsCreator(customerUnits, customerOrganisationId, userId, hasCorrectAdminRole)
                || UserIsContact(userId);
        }

        private bool HasCorrectAdminRoleForCustomer(int? customerOrganisationId, bool hasCorrectAdminRole = false)
        {
            return hasCorrectAdminRole && CustomerOrganisationId == customerOrganisationId;
        }
        private bool CreatedByUserWithoutUnit(int? customerOrganisationId, int userId)
        {
            return CustomerOrganisationId == customerOrganisationId && CustomerUnitId == null && CreatedBy == userId;
        }

        internal virtual bool UserIsContact(int userId)
        {
            return false;
        }

        private bool CreatedByUsersUnit(IEnumerable<int> customerUnits)
        {
            return CustomerUnitId != null && (customerUnits?.Contains(CustomerUnitId.Value) ?? false);
        }

    }
}
