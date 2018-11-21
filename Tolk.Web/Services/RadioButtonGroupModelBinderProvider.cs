using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Services
{
    public class RadioButtonGroupModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType.GetInterfaces().Contains(typeof(IRadioButtonGroup)))
            {
                return new BinderTypeModelBinder(typeof(RadioButtonGroupModelBinder));
            }

            return null;
        }
    }
}
