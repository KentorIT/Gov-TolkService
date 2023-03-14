using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;

namespace Tolk.Web.Controllers
{

    [Authorize(Policy = Policies.Customer)]
    public class ValidateController : Controller
    {        
        private readonly UserService _userService;
        private readonly ValidationService _validationService;        
        private readonly CacheService _cacheService;
        private const string CustomerSpecificInvoice= $"{nameof(CustomerSpecificInvoiceReference)}.Value";
        public ValidateController( UserService userService,CacheService cacheService, ValidationService validationService)
        {            
            _userService = userService;
            _validationService = validationService;            
            _cacheService = cacheService;
        }

        [HttpGet]
        public IActionResult CustomerSpecificInvoiceReference([Bind(Prefix = CustomerSpecificInvoice)] string value)
        {            
            var invoiceProperty = _cacheService.CustomerSpecificProperties.Where(csp => csp.CustomerOrganisationId == User.GetCustomerOrganisationId() && csp.PropertyToReplace == PropertyType.InvoiceReference).Single();                            
            if (!_validationService.ValidateCustomerSpecificProperty(invoiceProperty, value))
            {                
                return Json(invoiceProperty.RegexErrorMessage);
            }
            return Json(true);
        }
    }
}