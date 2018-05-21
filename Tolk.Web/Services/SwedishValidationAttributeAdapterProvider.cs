using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Web.Resources;

namespace Tolk.Web.Services
{
    public class SwedishValidationAttributeAdapterProvider : IValidationAttributeAdapterProvider
    {
        public IAttributeAdapter GetAttributeAdapter(ValidationAttribute attribute, IStringLocalizer stringLocalizer)
        {
            switch(attribute)
            {
                case RequiredAttribute requiredAttribute:
                    return new LocalizedRequiredAttributeAdapter(requiredAttribute, stringLocalizer);
                default:
                    return null;
            }
        }

        private class LocalizedRequiredAttributeAdapter : RequiredAttributeAdapter
        {
            public LocalizedRequiredAttributeAdapter(
                RequiredAttribute attribute,
                IStringLocalizer stringLocalizer) 
                : base(attribute, stringLocalizer)
            {
                if(attribute.ErrorMessage == null
                    && attribute.ErrorMessageResourceName == null
                    && attribute.ErrorMessageResourceType == null)
                {
                    attribute.ErrorMessageResourceType = typeof(DataAnnotationValidationMessages);
                    attribute.ErrorMessageResourceName = nameof(DataAnnotationValidationMessages.ValueRequired);
                }
            }
        }
    }
}
