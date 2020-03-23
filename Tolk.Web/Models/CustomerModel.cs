using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{

    public class CustomerModel
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
        [SubItem]
        [RequiredIf(nameof(IsCreating), true, OtherPropertyType = typeof(bool), AlwaysDisplayRequiredStar = true)]
        public TravelCostAgreementType? TravelCostAgreementType { get; set; }

        [Display(Name = "Namnprefix", Description = "Detta används vid skapande av användarnamn när det skapas en ny användare kopplat till organisationen")]
        [RequiredIf(nameof(IsCreating), true, OtherPropertyType = typeof(bool), AlwaysDisplayRequiredStar = true)]
        public string OrganisationPrefix { get; set; }

        [Display(Name = "Organisationsnummer")]
        [Required]
        public string OrganisationNumber { get; set; }

        [Display(Name = "EmailDomän", Description = "Detta används när en användare som kopplar upp sig själv, för att kunna räkna ut med vilken organisation hen skall kopplas till.")]
        [Required]
        public string EmailDomain { get; set; }

        public bool IsCreating { get; set; }

        public UserPageMode UserPageMode { get; set; }

        public CustomerUserFilterModel UserFilterModel { get; set; }

        [Display(Name = "Använd sammanhållen bokning")]
        public bool UseOrderGroups { get; set; }

        [Display(Name = "Tolken fakturerar själv tolkarvode")]
        public bool UseSelfInvoicingInterpreter { get; set; }

        internal static CustomerModel GetModelFromCustomer(CustomerOrganisation customer, string message = null)
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
                TravelCostAgreementType = customer.TravelCostAgreementType,
                Message = message,
                UserPageMode = new UserPageMode
                {
                    BackController = "Customer",
                    BackAction = "View",
                    BackId = customer.CustomerOrganisationId.ToSwedishString()
                },
                UseOrderGroups = customer.UseOrderGroups,
                UseSelfInvoicingInterpreter = customer.UseSelfInvoicingInterpreter
            };
        }

        internal void UpdateCustomer(CustomerOrganisation customer)
        {
            customer.Name = Name;
            customer.ParentCustomerOrganisationId = ParentId;
            customer.EmailDomain = EmailDomain;
            customer.OrganisationNumber = OrganisationNumber;
            customer.UseOrderGroups = UseOrderGroups;
            customer.UseSelfInvoicingInterpreter = UseSelfInvoicingInterpreter;
        }
    }
}
