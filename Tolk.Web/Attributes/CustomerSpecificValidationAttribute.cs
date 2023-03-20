using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Tolk.BusinessLogic.Models.CustomerSpecificProperties;

namespace Tolk.Web.Attributes
{
    public class CustomerSpecificValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {                        
            var property = (CustomerSpecificPropertyModel) value;
            if (property == null || (!property.Required && string.IsNullOrEmpty(property.Value)))
            {
                return null;
            }
            if(property.Value.Length > property.MaxLength)
            {
                return new ValidationResult($"Value can not be longer than {property.MaxLength}");
            }
            if(property.RegexPattern == null)
            {
                return new ValidationResult("There is not Regex to validate against");
            }            
            var regexChecker = new Regex(property.RegexPattern);
            return regexChecker.Match(property.Value).Success ? ValidationResult.Success : new ValidationResult(property.RegexErrorMessage);
        }
    }
}
