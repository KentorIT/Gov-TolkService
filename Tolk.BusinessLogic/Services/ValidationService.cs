using System.Text.RegularExpressions;
using Tolk.BusinessLogic.Models.CustomerSpecificProperties;

namespace Tolk.BusinessLogic.Services
{
    public class ValidationService
    {                  
        public bool ValidateCustomerSpecificProperty(CustomerSpecificPropertyModel property, string value)
        {
            if(property == null)
            {
                return false;
            }
            var regexChecker = new Regex(property.RegexPattern);
            return regexChecker.Match(value).Success;
        }
    }
}
