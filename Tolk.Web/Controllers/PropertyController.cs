using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Models.CustomerSpecificProperties;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;

namespace Tolk.Web.Controllers
{
    [Authorize(Roles = Roles.AppOrSysAdmin)]
    [Route("[controller]/[action]")]
    public class PropertyController : Controller
    {
        private readonly TolkDbContext _tolkDbContext;
        private readonly CacheService _cacheService;
        private readonly IAuthorizationService _authorizationService;
        private readonly ISwedishClock _clock;

        public PropertyController(TolkDbContext tolkDbContext, CacheService cacheService, IAuthorizationService authorizationService, ISwedishClock clock)
        {
            _tolkDbContext = tolkDbContext;
            _cacheService = cacheService;
            _authorizationService = authorizationService;
            _clock = clock;
        }

        public ActionResult Index()
        {
            return RedirectToAction(nameof(View));
        }

        [HttpGet("{customerOrganisationId}/{propertyType}")]
        public ActionResult View(int customerOrganisationId, PropertyType propertyType)
        {
            var property = _cacheService.AllCustomerSpecificProperties.Where(csp => csp.CustomerOrganisationId == customerOrganisationId && csp.PropertyToReplace == propertyType).Single();

            return View(property);
        }
        public ActionResult Edit(int customerOrganisationId, PropertyType propertyType)
        {
            var property = _cacheService.AllCustomerSpecificProperties.Where(csp => csp.CustomerOrganisationId == customerOrganisationId && csp.PropertyToReplace == propertyType).Single();
            return View(property);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(CustomerSpecificPropertyModel propertyModel)
        {
            if(ModelState.IsValid)
            {
                var propertyEntity = propertyModel.ToEntity();

                var existingProperty = await _tolkDbContext.CustomerSpecificProperties.GetCustomerSpecificPropertiesWithCustomerOrganisation(propertyModel.CustomerOrganisationId, propertyModel.PropertyToReplace).SingleAsync();                  
                existingProperty.CustomerOrganisation.UpdateCustomerSpecificPropertySettings(_clock.SwedenNow, User.GetUserId(), propertyEntity);  
                await _tolkDbContext.SaveChangesAsync();
                await _cacheService.Flush(CacheKeys.CustomerSpecificProperties);
                return RedirectToAction(nameof(View), new { customerOrganisationId = propertyModel.CustomerOrganisationId, propertyType = propertyModel.PropertyToReplace });
            }
            return Forbid();
        }

        public ActionResult Create(int customerOrganisationId)
        {
            return View(new CustomerSpecificPropertyModel { CustomerOrganisationId = customerOrganisationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CustomerSpecificPropertyModel propertyModel)
        {

            var existingProperty = await _tolkDbContext.CustomerSpecificProperties.Where(csp => csp.CustomerOrganisationId == propertyModel.CustomerOrganisationId && csp.PropertyType == propertyModel.PropertyToReplace).FirstOrDefaultAsync();
            if(existingProperty != null || !ModelState.IsValid)
            {
                return Forbid();
            }
            var propertyEntity = propertyModel.ToEntity();         

            await _tolkDbContext.AddAsync(propertyEntity);

            await _tolkDbContext.SaveChangesAsync();
            await _cacheService.Flush(CacheKeys.CustomerSpecificProperties);

            return RedirectToAction(nameof(View), new { customerOrganisationId = propertyModel.CustomerOrganisationId, propertyType = propertyModel.PropertyToReplace });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Disable(CustomerSpecificPropertyModel propertyModel)
            => await ChangeEnabled(propertyModel, enabledStatus: false);
        

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Enable(CustomerSpecificPropertyModel propertyModel)
            => await ChangeEnabled(propertyModel, enabledStatus: true);         
        
        private async Task<ActionResult> ChangeEnabled(CustomerSpecificPropertyModel propertyModel, bool enabledStatus)
        {
            var existingProperty = await _tolkDbContext.CustomerSpecificProperties.GetCustomerSpecificPropertiesWithCustomerOrganisation(propertyModel.CustomerOrganisationId, propertyModel.PropertyToReplace).FirstOrDefaultAsync();

            if (existingProperty != null)
            {
                var newProperty = CustomerSpecificProperty.CopyCustomerSpecificProperty(existingProperty);
                newProperty.Enabled = enabledStatus;
                existingProperty.CustomerOrganisation.UpdateCustomerSpecificPropertySettings(_clock.SwedenNow, User.GetUserId(), newProperty);
                await _tolkDbContext.SaveChangesAsync();
                await _cacheService.Flush(CacheKeys.CustomerSpecificProperties);
                return RedirectToAction(nameof(CustomerController.View), "Customer", new { id = propertyModel.CustomerOrganisationId, message = $"Kundspecifik {propertyModel.PropertyToReplace.GetDescription()} uppdaterat" });
            }
            else
            {
                return RedirectToAction(nameof(CustomerController.View), "Customer", new { customerOrganisationId = propertyModel.CustomerOrganisationId, message = "Kundspecifikt fält existerar inte" });
            }
        }
    }
}