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
        public PriceListType PriceListType { get; set; }

        [Display(Name = "Namnprefix", Description = "Detta används vid skapande av användarnamn när det skapas en ny användare kopplat till organisationen")]
        [Required]
        public string OrganisationPrefix { get; set; }

        [Display(Name = "Organisationsnummer")]
        [Required]
        public string OrganisationNumber { get; set; }

        [Display(Name = "EmailDomän", Description = "Detta används när en användare som kopplar upp sig själv, för att kunna räkna ut med vilken organisation hen skall kopplas till.")]
        [Required]
        public string EmailDomain { get; set; }

        public UserPageMode UserPageMode { get; set; }

        public CustomerUserFilterModel UserFilterModel { get; set; }

        public static CustomerModel GetModelFromCustomer(CustomerOrganisation customer, string message = null)
        {
            return new CustomerModel
            {
                CustomerId = customer.CustomerOrganisationId,
                Name = customer.Name,
                ParentName = customer.ParentCustomerOrganisation?.Name,
                ParentId = customer.ParentCustomerOrganisationId,
                PriceListType = customer.PriceListType,
                EmailDomain = customer.EmailDomain,
                OrganisationPrefix = customer.OrganisationPrefix,
                OrganisationNumber = customer.OrganisationNumber,
                Message = message,
                UserPageMode = new UserPageMode
                {
                    BackController = "Customer",
                    BackAction = "View",
                    BackId = customer.CustomerOrganisationId.ToSwedishString()
                }
                
            };
        }

        internal void UpdateCustomer(CustomerOrganisation customer)
        {
            customer.Name = Name;
            customer.ParentCustomerOrganisationId = ParentId;
            customer.EmailDomain = EmailDomain;
            customer.OrganisationPrefix = OrganisationPrefix;
            customer.OrganisationNumber = OrganisationNumber;
        }
    }
}
