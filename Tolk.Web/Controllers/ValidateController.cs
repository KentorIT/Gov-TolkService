using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.RegularExpressions;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Models.CustomerSpecificProperties;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;

namespace Tolk.Web.Controllers
{

    [Authorize(Policy = Policies.Customer)]
    public class ValidateController : Controller
    {                           
        private readonly CacheService _cacheService;
        private readonly ILogger _logger;
        private const string CustomerSpecificInvoice= $"{nameof(CustomerSpecificInvoiceReference)}.Value";
        public ValidateController(CacheService cacheService, ILogger<ValidateController> logger)
        {                                           
            _cacheService = cacheService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult CustomerSpecificInvoiceReference([Bind(Prefix = CustomerSpecificInvoice)] string value)
        {
            var customerOrganisationId = User.GetCustomerOrganisationId();
            var invoiceProperty = _cacheService.CustomerSpecificProperties.Where(csp => csp.CustomerOrganisationId == customerOrganisationId && csp.PropertyToReplace == PropertyType.InvoiceReference).Single();        
            var validationResult = ValidateCustomerSpecificProperty(invoiceProperty, value);

            if (!validationResult.Success)
            {                
                return Json(validationResult.ErrorMessage);
            }
            return Json(validationResult.Success);
        }

        private (bool Success, string ErrorMessage ) ValidateCustomerSpecificProperty(CustomerSpecificPropertyModel property, string value)
        {
            if (property == null)
            {
                _logger.LogInformation("Myndighet med id {customerOrganisationId} har ingen kundspecifik inställning för {property}", property.CustomerOrganisationId, property.PropertyToReplace.GetDescription());
                return (false, $"Kundspecifik inställning för {property.PropertyToReplace.GetDescription()} saknas");
            }
            var regexChecker = new Regex(property.RegexPattern);
            if(!regexChecker.Match(value).Success)
            {
                return new (false, property.RegexErrorMessage);
            }
            return (true, string.Empty);
        }
    }
}