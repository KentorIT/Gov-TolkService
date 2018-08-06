using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Services
{
    public class TimeSpanModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType == typeof(TimeSpan)
                || context.Metadata.ModelType == typeof(TimeSpan?))
            {
                return new BinderTypeModelBinder(typeof(TimeSpanModelBinder));
            }

            return null;
        }
    }
}
