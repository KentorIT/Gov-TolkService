using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Helpers
{
    public class ValidTimeSpanRangeAttribute : ValidationAttribute, IClientModelValidator
    {
        public string StartAtProperty { get; set; }

        public string EndAtProperty { get; set; }

        public ValidTimeSpanRangeAttribute() { }
        public ValidTimeSpanRangeAttribute(string StartAtProperty, string EndAtProperty)
        {
            this.StartAtProperty = StartAtProperty;
            this.EndAtProperty = EndAtProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (validationContext == null)
            {
                return new ValidationResult("The range values could not be validated due to null values");
            }
            if (value == null)
            {
                return ValidationResult.Success;
            }
            TimeSpan timeSpan = (TimeSpan)(value as TimeSpan?);
            var startAt = (TimeSpan)GetProperty(StartAtProperty, validationContext);
            var endAt = (TimeSpan)GetProperty(EndAtProperty, validationContext);
            if (timeSpan > endAt)
            {
                return new ValidationResult("The value cannot be after EndAt");
            }
            if (timeSpan < startAt)
            {
                return new ValidationResult("The value cannot be before StartAt");
            }
            return ValidationResult.Success;
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-validtimespanrange", $"{context.ModelMetadata.DisplayName} måste vara mellan valid start och slut.");
            MergeAttribute(context.Attributes, "data-val-validtimespanrange-startatproperty", StartAtProperty);
            MergeAttribute(context.Attributes, "data-val-validtimespanrange-endatproperty", EndAtProperty);
        }

        //TODO: Make Extension method, and replace where applicable
        private bool MergeAttribute(IDictionary<string, string> attributes, string key, string value)
        {
            if (attributes.ContainsKey(key))
            {
                return false;
            }

            attributes.Add(key, value);
            return true;
        }

        //TODO: Make Extension method, and replace where applicable
        private static object GetProperty(string name, ValidationContext validationContext)
        {
            return validationContext.ObjectType.GetProperty(name).GetValue(
                validationContext.ObjectInstance, null);
        }

    }
}
