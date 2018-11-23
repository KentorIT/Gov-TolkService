using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Web.Models;

namespace Tolk.Web.Helpers
{
    public class RequiredCheckedAttribute : ValidationAttribute, IClientModelValidator
    {
        public int Min { get; set; } = 0;

        public int Max { get; set; } = int.MaxValue;

        public void AddValidation(ClientModelValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Attributes.ContainsKey("class") && context.Attributes["class"].Contains("force-validation"))
            {
                var minText = Min != 0 ? $"minst {Min} val" : "";
                var maxText = Max != int.MaxValue ? $"max {Max} val" : "";
                var bridge = Min != 0 && Max != int.MaxValue ? ", samt " : "";

                MergeAttribute(context.Attributes, "data-val", "true");
                MergeAttribute(context.Attributes, "data-val-requiredchecked", $"{context.ModelMetadata.DisplayName} får ha {minText}{bridge}{maxText} ifyllda");
                MergeAttribute(context.Attributes, "data-val-requiredchecked-min", Min.ToString());
                MergeAttribute(context.Attributes, "data-val-requiredchecked-max", Max.ToString());
                MergeAttribute(context.Attributes, "data-val-requiredchecked-maxchecked", int.MaxValue.ToString());
            }
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            { // Argument checks
                if (value == null || validationContext == null)
                {
                    throw new ArgumentNullException();
                }
                if (Min < 0 || Max < 0)
                {
                    throw new ArgumentOutOfRangeException("Arguments cannot be negative");
                }
                if (Min > Max)
                {
                    throw new ArgumentException($"{nameof(Min)} cannot be bigger than {nameof(Max)}");
                }
            }

            var checkboxGroup = (CheckboxGroup) value;

            if (checkboxGroup.SelectedItems.Count < Min || checkboxGroup.SelectedItems.Count > Max)
            {
                return new ValidationResult(ErrorMessageString);
            }

            return ValidationResult.Success;
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
