using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.RegularExpressions;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;

namespace Tolk.Web.Controllers
{

    [Authorize(Policy = Policies.Customer)]
    public class ValidateController : Controller
    {
        private readonly ILogger _logger;
        private readonly UserService _userService;
        private readonly CacheService _cacheService;        
        private const string CustomerSpecificInvoice= $"{nameof(CustomerSpecificInvoiceReference)}.Value";
        public ValidateController(ILogger<OrderController> logger, UserService userService, TolkDbContext dbContext, CacheService cacheService)
        {
            _logger = logger;
            _userService = userService;            
            _cacheService = cacheService;
        }

        [HttpGet]
        public IActionResult CustomerSpecificInvoiceReference([Bind(Prefix = CustomerSpecificInvoice)] string value)
        {            
            var invoiceProperty = _cacheService.CustomerSpecificProperties.Where(csp => csp.CustomerOrganisationId == User.GetCustomerOrganisationId() && csp.PropertyToReplace == PropertyType.InvoiceReference).Single();                
            var regexChecker = new Regex(invoiceProperty.RegexPattern);
            if (!regexChecker.Match(value).Success)
            {
                return Json(invoiceProperty.RegexErrorMessage);
            }
            return Json(true);
        }
    }
}