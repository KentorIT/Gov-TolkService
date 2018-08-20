using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Services
{
    public class DateRangeModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType == typeof(DateRange))
            {
                return new BinderTypeModelBinder(typeof(DateRangeModelBinder));
            }

            return null;
        }
    }
}
