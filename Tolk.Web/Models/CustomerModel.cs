using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class CustomerModel : IModel
    {
        public int? CustomerId { get; set; }

        public string Message { get; set; }

        [Display(Name = "Namn")]
        [Required]
        public string Name { get; set; }

        [Display(Name = "Föräldermyndighet")]
        public string ParentName { get; set; }

        [Display(Name = "Föräldermyndighet")]
        public int? ParentId { get; set; }

        [Display(Name = "Prislista")]
        [RequiredIf(nameof(IsCreating), true, OtherPropertyType = typeof(bool), AlwaysDisplayRequiredStar = true)]
        public PriceListType? PriceListType { get; set; }

        [Display(Name = "Avtal för bilersättning")]
        [RequiredIf(nameof(IsCreating), true, OtherPropertyType = typeof(bool), AlwaysDisplayRequiredStar = true)]
        public TravelCostAgreementType? TravelCostAgreementType { get; set; }

        [Display(Name = "Namnprefix", Description = "Detta används vid skapande av användarnamn när det skapas en ny användare kopplat till organisationen")]
        [RequiredIf(nameof(IsCreating), true, OtherPropertyType = typeof(bool), AlwaysDisplayRequiredStar = true)]
        public string OrganisationPrefix { get; set; }

        [Display(Name = "Organisationsnummer")]
        [Required]
        public string OrganisationNumber { get; set; }

        [Display(Name = "Peppol-ID")]
        public string PeppolId { get; set; }

        [Display(Name = "EmailDomän", Description = "Detta används när en användare som kopplar upp sig själv, för att kunna räkna ut med vilken organisation hen skall kopplas till.")]
        [Required]
        public string EmailDomain { get; set; }

        public bool IsCreating { get; set; }

        public bool AllowEdit { get; set; }

        public UserPageMode UserPageMode { get; set; }

        public CustomerUserFilterModel UserFilterModel { get; set; }

        public AdminUnitFilterModel UnitFilterModel { get; set; }

        [SubItem]
        public List<CustomerSettingModel> CustomerSettings { get; set; }

        [Display(Name = "Använd sammanhållen bokning")]
        public bool UseOrderGroups { get; set; }

        [Display(Name = "Tolken fakturerar själv tolkarvode")]
        public bool UseSelfInvoicingInterpreter { get; set; }

        internal static CustomerModel GetModelFromCustomer(CustomerOrganisation customer, string message = null, bool allowEdit = true)
        {
            return new CustomerModel
            {
                IsCreating = false,
                CustomerId = customer.CustomerOrganisationId,
                Name = customer.Name,
                ParentName = customer.ParentCustomerOrganisation?.Name,
                ParentId = customer.ParentCustomerOrganisationId,
                PriceListType = customer.PriceListType,
                EmailDomain = customer.EmailDomain,
                OrganisationPrefix = customer.OrganisationPrefix,
                OrganisationNumber = customer.OrganisationNumber,
                PeppolId = customer.PeppolId,
                TravelCostAgreementType = customer.TravelCostAgreementType,
                Message = message,
                UserPageMode = new UserPageMode
                {
                    BackController = "Customer",
                    BackAction = "View",
                    BackId = customer.CustomerOrganisationId.ToSwedishString()
                },
                CustomerSettings = customer.CustomerSettings.Select(c => new CustomerSettingModel { CustomerSettingType = c.CustomerSettingType, Value = c.Value }).OrderBy(csm => csm.CustomerSettingType.GetDescription()).ToList(),
                AllowEdit = allowEdit
            };
        }

        internal void UpdateCustomer(CustomerOrganisation customer, bool isNewCustomer = false)
        {
            customer.Name = Name;
            customer.ParentCustomerOrganisationId = ParentId;
            customer.EmailDomain = EmailDomain;
            customer.OrganisationNumber = OrganisationNumber;
            customer.PeppolId = PeppolId;
            if (isNewCustomer)
            {
                customer.PriceListType = PriceListType.Value;
                customer.OrganisationPrefix = OrganisationPrefix;
                customer.TravelCostAgreementType = TravelCostAgreementType.Value;
                customer.CustomerSettings.AddRange(CustomerSettings.Select(c => new CustomerSetting { CustomerSettingType = c.CustomerSettingType, Value = c.Value }).ToList());
            }
        }

    }
}
