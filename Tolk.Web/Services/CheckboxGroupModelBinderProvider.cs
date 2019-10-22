using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Services
{
    public class CheckboxGroupModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            NullCheckHelper.ArgumentCheckNull(context, nameof(CheckboxGroupModelBinderProvider));
            if (context.Metadata.ModelType == typeof(CheckboxGroup))
            {
                return new BinderTypeModelBinder(typeof(CheckboxGroupModelBinder));
            }
            return null;
        }
    }
}
