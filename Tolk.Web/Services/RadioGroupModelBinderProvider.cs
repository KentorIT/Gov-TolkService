using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Services
{
    public class RadioGroupModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType == typeof(Sex))
            {
                return new BinderTypeModelBinder(typeof(RadioGroupModelBinder));
            }

            return null;
        }
    }
}
