using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System;
using Tolk.Web.Helpers;

namespace Tolk.Web.Services
{
    public class DateTimeOffsetModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            NullCheckHelper.ArgumentCheckNull(context, nameof(DateTimeOffsetModelBinderProvider));
            if (context.Metadata.ModelType == typeof(DateTimeOffset) ||
                context.Metadata.ModelType == typeof(DateTimeOffset?))
            {
                return new BinderTypeModelBinder(typeof(DateTimeOffsetModelBinder));
            }
            return null;
        }
    }
}
