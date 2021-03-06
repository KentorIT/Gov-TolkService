﻿using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Services
{
    public class CheckboxGroupModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            NullCheckHelper.ArgumentCheckNull(bindingContext, nameof(CheckboxGroupModelBinder));
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
