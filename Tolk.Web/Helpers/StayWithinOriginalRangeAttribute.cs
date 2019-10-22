using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.Web.Models;

namespace Tolk.Web.Helpers
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class StayWithinOriginalRangeAttribute : ValidationAttribute, IClientModelValidator
    {
        public string OtherRangeProperty { get; set; }

        public bool ValidateEndDate { get; set; }

        public StayWithinOriginalRangeAttribute()
        {
            ValidateEndDate = true;
        }

        public void AddValidation(ClientModelValidationContext context)
        {
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || validationContext == null)
            {
                return new ValidationResult("The range values could not be validated due to null values");
            }
            TimeRange otherRange = GetProperty(OtherRangeProperty, validationContext) as TimeRange;
            TimeRange range = value as TimeRange;
            if (range.StartDateTime >= otherRange.StartDateTime
                && (ValidateEndDate && range.EndDateTime <= otherRange.EndDateTime))
            {
                return ValidationResult.Success;
            }
            return new ValidationResult("The Range was not within the other range.");
        }

        private static object GetProperty(string name, ValidationContext validationContext)
        {
            return validationContext.ObjectType.GetProperty(name).GetValue(
                validationContext.ObjectInstance, null);
        }
    }
}
