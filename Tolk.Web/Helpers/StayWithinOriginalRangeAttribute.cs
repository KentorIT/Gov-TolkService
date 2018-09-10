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
    public class StayWithinOriginalRangeAttribute : ValidationAttribute
    {
        public string OtherRangeProperty { get; set; }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            TimeRange otherRange = GetProperty(OtherRangeProperty, validationContext) as TimeRange;
            TimeRange range = value as TimeRange;
            if (range.StartDateTime >= otherRange.StartDateTime && range.EndDateTime <= otherRange.EndDateTime)
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
