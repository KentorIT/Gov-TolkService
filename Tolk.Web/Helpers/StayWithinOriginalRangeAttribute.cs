using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Models;

namespace Tolk.Web.Helpers
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class StayWithinOriginalRangeAttribute : ValidationAttribute, IClientModelValidator
    {
        public string OtherRangeProperty { get; set; }
        public string RulesetProperty { get; set; }

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
            FrameworkAgreementResponseRuleset ruleset = GetProperty(RulesetProperty, validationContext) as FrameworkAgreementResponseRuleset? ?? FrameworkAgreementResponseRuleset.VersionOne;
            TimeRange otherRange = GetProperty(OtherRangeProperty, validationContext) as TimeRange;
            TimeRange range = value as TimeRange;
            if ((ruleset == FrameworkAgreementResponseRuleset.VersionOne && 
                    range.StartDateTime >= otherRange.StartDateTime && 
                    ValidateEndDate && 
                    range.EndDateTime <= otherRange.EndDateTime) ||
                (ruleset == FrameworkAgreementResponseRuleset.VersionTwo && 
                    range.Duration <= otherRange.Duration &&
                    range.StartDateTime.Value.AddMinutes(5) <= otherRange.EndDateTime && 
                    ValidateEndDate && 
                    range.EndDateTime.Value.AddMinutes(-5) >= otherRange.StartDateTime)
                )
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
