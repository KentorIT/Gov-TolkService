using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using System;
using Tolk.Web.Resources;

namespace Tolk.Web.Services
{
    public static class LocalizedModelBindingMessageExtensions
    {
        public static IMvcBuilder AddModelBindingMessagesLocalizer(this IMvcBuilder mvc,
            IServiceCollection services, Type modelBaseType)
        {
            var factory = services.BuildServiceProvider().GetService<IStringLocalizerFactory>();
            var VL = factory.Create(typeof(DataAnnotationValidationMessages));

            return mvc.AddMvcOptions(o =>
            {
                // for validation error messages
                o.ModelMetadataDetailsProviders.Add(new LocalizableValidationMetadataProvider(VL, modelBaseType));
            });
        }
    }
}
