using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Services
{
    public class RadioButtonGroupModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var genericType = bindingContext.ModelType.GenericTypeArguments[0];
            var value = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}");

            if (value == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            var model = new RadioButtonGroup<object> { SelectedItem = Enum.Parse(genericType, value.FirstValue) };

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }
    }
}
