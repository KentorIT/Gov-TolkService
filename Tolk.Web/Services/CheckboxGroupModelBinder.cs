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
            var value = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}");

            if (value == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            var selectedItems = new HashSet<string>();
            foreach (var val in value)
            {
                selectedItems.Add(val);
            }

            var model = new CheckboxGroup
            {
                SelectedItems = SelectListService.CompetenceLevels
                    .Where(item => selectedItems
                        .Contains(item.Value)
                    ).ToHashSet()
            };

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }
    }
}
