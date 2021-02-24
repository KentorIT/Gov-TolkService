using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Localization;
using System;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Services
{
    public class LocalizableValidationMetadataProvider : IValidationMetadataProvider
    {
        private IStringLocalizer _stringLocalizer;
        private Type _injectableType;

        public LocalizableValidationMetadataProvider(IStringLocalizer stringLocalizer, Type injectableType)
        {
            _stringLocalizer = stringLocalizer;
            _injectableType = injectableType;
        }

        public void CreateValidationMetadata(ValidationMetadataProviderContext context)
        {
            // ignore non-properties and types that do not match some model base type
            if (context?.Key.ContainerType == null ||
                !_injectableType.IsAssignableFrom(context.Key.ContainerType))
                return;

            // In the code below I assume that expected use of ErrorMessage will be:
            // 1 - not set when it is ok to fill with the default translation from the resource file
            // 2 - set to a specific key in the resources file to override my defaults
            // 3 - never set to a final text value
            var propertyName = context.Key.Name;
            var modelName = context.Key.ContainerType.Name;

            // sanity check 
            if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(modelName))
                return;

            foreach (var attribute in context.ValidationMetadata.ValidatorMetadata)
            {
                var tAttr = attribute as ValidationAttribute;
                if (tAttr != null)
                {
                    // at first, assume the text to be generic error
                    var errorName = tAttr.GetType().Name;
                    var fallbackName = errorName + "_ValidationError";
                    // Will look for generic widely known resource keys like
                    // MaxLengthAttribute_ValidationError
                    // RangeAttribute_ValidationError
                    // EmailAddressAttribute_ValidationError
                    // RequiredAttribute_ValidationError
                    // etc.

                    // Treat errormessage as resource name, if it's set,
                    // otherwise assume default.
                    var name = tAttr.ErrorMessage ?? fallbackName;

                    // At first, attempt to retrieve model specific text
                    var localized = _stringLocalizer[name];

                    // Final attempt - default name from property alone
                    if (localized.ResourceNotFound) // missing key or prefilled text
                        localized = _stringLocalizer[fallbackName];

                    // If not found yet, then give up, leave initially determined name as it is
                    var text = localized.ResourceNotFound ? name : localized;

                    tAttr.ErrorMessage = text;
                }
            }
        }
    }
}
