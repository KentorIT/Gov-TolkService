using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Helpers
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ClientRequiredAttribute : ValidationAttribute, IClientModelValidator
    {
        public void AddValidation(ClientModelValidationContext context)
        {
            NullCheckHelper.ArgumentCheckNull(context, nameof(ClientRequiredAttribute));
            // Overriding required message manually, as it will default to built-in english messages otherwise
            context.Attributes["data-val-required"] = ErrorMessage ?? Resources.DataAnnotationValidationMessages.Required.FormatSwedish(context.ModelMetadata.DisplayName);
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // This attribute should always succeed in the server side validation.
            return ValidationResult.Success;
        }
    }
}
