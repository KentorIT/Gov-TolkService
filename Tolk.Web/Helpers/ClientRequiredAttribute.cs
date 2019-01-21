﻿using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Tolk.Web.Helpers
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ClientRequiredAttribute : ValidationAttribute, IClientModelValidator
    {
        public void AddValidation(ClientModelValidationContext context)
        {
            // Overriding required message manually, as it will default to built-in english messages otherwise
            context.Attributes["data-val-required"] = ErrorMessage ?? string.Format(Resources.DataAnnotationValidationMessages.Required, context.ModelMetadata.DisplayName);
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // This attribute should always succeed in the server side validation.
            return ValidationResult.Success;
        }

        public class ValidationMetadataProvider : IValidationMetadataProvider
        {
            public void CreateValidationMetadata(ValidationMetadataProviderContext context)
            {
                if(context.Attributes.OfType<ClientRequiredAttribute>().Any())
                {
                    context.ValidationMetadata.IsRequired = true;
                }
            }
        }
    }
}
