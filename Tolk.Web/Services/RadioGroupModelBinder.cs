using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Services
{
    public class RadioGroupModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var value = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}");

            if (value == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            var model = Enum.Parse(typeof(Sex), value.FirstValue);

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }
    }
}
