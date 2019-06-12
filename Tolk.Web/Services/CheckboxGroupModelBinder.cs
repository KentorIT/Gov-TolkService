using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Services
{
    public class CheckboxGroupModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var selectedValues = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}");

            if (selectedValues == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            var model = new CheckboxGroup
            {
                SelectedItems = selectedValues.Select(sv => new SelectListItem { Value = sv })
            };

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }
    }
}
