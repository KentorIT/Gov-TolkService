using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using System.Linq;
using Tolk.Web.Helpers;

namespace Tolk.Web.Services
{

    public class ValidationMetadataProvider : IValidationMetadataProvider
    {
        public void CreateValidationMetadata(ValidationMetadataProviderContext context)
        {
            NullCheckHelper.ArgumentCheckNull(context, nameof(ValidationMetadataProvider));
            if (context.Attributes.OfType<ClientRequiredAttribute>().Any())
            {
                context.ValidationMetadata.IsRequired = true;
            }
        }
    }
}
