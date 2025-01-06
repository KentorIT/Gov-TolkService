using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Utilities;
namespace Tolk.Web.Helpers
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RequireSameEmailDomainAttribute : ValidationAttribute, IClientModelValidator
    {
        public string ValidEmailDomainProperty { get; set; }
        public string ValidateIfTrue { get; set; }

        public new string ErrorMessageString { get; set; } = null;

        public RequireSameEmailDomainAttribute() { }
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var validate = validationContext.ObjectInstance.GetPropertyValue<bool>(ValidateIfTrue);
            var domain = validationContext.ObjectInstance.GetPropertyValue<string>(ValidEmailDomainProperty);
            string newDomain = value?.ToString().GetEmailDomain();
            //Do not validate if value is not a valid email adress or domain is not set
            validate &= !string.IsNullOrEmpty(newDomain) && !string.IsNullOrEmpty(domain);
            return !validate || newDomain.Equals(domain, StringComparison.InvariantCultureIgnoreCase) ?
                ValidationResult.Success :
                new ValidationResult(ErrorMessageString);
        }
        public void AddValidation(ClientModelValidationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-requiresameemaildomain", $"{context.ModelMetadata.DisplayName} får inte byta epost domän");
            MergeAttribute(context.Attributes, "data-val-requiresameemaildomain-validemaildomainproperty", ValidEmailDomainProperty);
            MergeAttribute(context.Attributes, "data-val-requiresameemaildomain-validateiftrue", ValidateIfTrue.ToString());
        }

        private bool MergeAttribute(IDictionary<string, string> attributes, string key, string value)
        {
            if (attributes.ContainsKey(key))
            {
                return false;
            }

            attributes.Add(key, value);
            return true;
        }
    }
}
