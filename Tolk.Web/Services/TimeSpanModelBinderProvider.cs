using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System;
using Tolk.Web.Helpers;

namespace Tolk.Web.Services
{
    public class TimeSpanModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            NullCheckHelper.ArgumentCheckNull(context, nameof(TimeSpanModelBinderProvider));
            if (context.Metadata.ModelType == typeof(TimeSpan)
                || context.Metadata.ModelType == typeof(TimeSpan?))
            {
                return new BinderTypeModelBinder(typeof(TimeSpanModelBinder));
            }

            return null;
        }
    }
}
