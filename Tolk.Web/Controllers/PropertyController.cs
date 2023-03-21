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

namespace Tolk.Web.Controllers
{
    [Authorize(Roles = Roles.AppOrSysAdmin)]
    [Route("[controller]/[action]")]
    public class PropertyController : Controller
    {
        private readonly TolkDbContext _tolkDbContext;
        private readonly CacheService _cacheService;
        private readonly IAuthorizationService _authorizationService;

        public PropertyController(TolkDbContext tolkDbContext, CacheService cacheService, IAuthorizationService authorizationService)
        {
            _tolkDbContext = tolkDbContext;
            _cacheService = cacheService;
            _authorizationService = authorizationService;
        }

        public ActionResult Index()
        {
            return RedirectToAction(nameof(View));
        }

        [HttpGet("{customerOrganisationId}/{propertyType}")]
        public ActionResult View(int customerOrganisationId, PropertyType propertyType)
        {
            var property = _cacheService.CustomerSpecificProperties.Where(csp => csp.CustomerOrganisationId == customerOrganisationId && csp.PropertyToReplace == propertyType).Single();

            return View(property);
        }
        public ActionResult Edit(int customerOrganisationId, PropertyType propertyType)
        {
            var property = _cacheService.CustomerSpecificProperties.Where(csp => csp.CustomerOrganisationId == customerOrganisationId && csp.PropertyToReplace == propertyType).Single();
            return View(property);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(CustomerSpecificPropertyModel propertyModel)
        {
            if(ModelState.IsValid)
            {
                var propEntity = await _tolkDbContext.CustomerSpecificProperties.Where(csp => csp.CustomerOrganisationId == propertyModel.CustomerOrganisationId && csp.PropertyType == propertyModel.PropertyToReplace).SingleAsync();
                propEntity.RemoteValidation = propertyModel.RemoteValidation;
                propEntity.InputPlaceholder = propertyModel.Placeholder;
                propEntity.Required = propertyModel.Required;
                propEntity.MaxLength = propertyModel.MaxLength;
                propEntity.RegexPattern = propertyModel.RegexPattern;
                propEntity.RegexErrorMessage = propertyModel.RegexErrorMessage;
                propEntity.DisplayName = propertyModel.DisplayName;
                propEntity.DisplayDescription = propertyModel.DisplayDescription;
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
            var propEntity = new CustomerSpecificProperty();
            propEntity.CustomerOrganisationId = propertyModel.CustomerOrganisationId;
            propEntity.PropertyType = propertyModel.PropertyToReplace;
            propEntity.RemoteValidation = propertyModel.RemoteValidation;
            propEntity.InputPlaceholder = propertyModel.Placeholder;
            propEntity.Required = propertyModel.Required;
            propEntity.MaxLength = propertyModel.MaxLength;
            propEntity.RegexPattern = propertyModel.RegexPattern;
            propEntity.RegexErrorMessage = propertyModel.RegexErrorMessage;
            propEntity.DisplayName = propertyModel.DisplayName;
            propEntity.DisplayDescription = propertyModel.DisplayDescription;

            await _tolkDbContext.AddAsync(propEntity);

            await _tolkDbContext.SaveChangesAsync();
            await _cacheService.Flush(CacheKeys.CustomerSpecificProperties);

            return RedirectToAction(nameof(View), new { customerOrganisationId = propertyModel.CustomerOrganisationId, propertyType = propertyModel.PropertyToReplace });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(CustomerSpecificPropertyModel propertyModel)
        {
            var existingProperty = await _tolkDbContext.CustomerSpecificProperties.Where(csp => csp.CustomerOrganisationId == propertyModel.CustomerOrganisationId && csp.PropertyType == propertyModel.PropertyToReplace).FirstOrDefaultAsync();
            if(existingProperty != null)
            {
                _tolkDbContext.Remove(existingProperty);
                await _tolkDbContext.SaveChangesAsync();
                await _cacheService.Flush(CacheKeys.CustomerSpecificProperties);
                return RedirectToAction(nameof(CustomerController.View),"Customer", new {id=propertyModel.CustomerOrganisationId, message = $"Kundspecifik {propertyModel.PropertyToReplace.GetDescription()} borttaget" });
            }
            else
            {
                return RedirectToAction(nameof(CustomerController.View),"Customer", new { customerOrganisationId = propertyModel.CustomerOrganisationId, message = "Fält redan borttaget" });
            }
        }
    }
}