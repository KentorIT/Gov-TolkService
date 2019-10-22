using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Services
{
    public class RadioButtonGroupModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            NullCheckHelper.ArgumentCheckNull(context, nameof(RadioButtonGroupModelBinderProvider));
            if (context.Metadata.ModelType == typeof(RadioButtonGroup))
            {
                return new BinderTypeModelBinder(typeof(RadioButtonGroupModelBinder));
            }
            return null;
        }
    }
}
