using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Models;

namespace Tolk.Web.Services
{
    public class CheckboxGroupModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var genericType = bindingContext.ModelType.GenericTypeArguments[0];
            var value = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}");

            if (value == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            var selectedItems = new HashSet<object>();
            foreach (var val in value)
            {
                selectedItems.Add(Enum.Parse(genericType, val));
            }

            var model = new CheckboxGroup<object>
            {
                SelectedItems = selectedItems
            };

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }
    }
}
