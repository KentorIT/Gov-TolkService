using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Helpers
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ClientRequiredAttribute : ValidationAttribute, IClientModelValidator
    {
        public void AddValidation(ClientModelValidationContext context)
        {
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
