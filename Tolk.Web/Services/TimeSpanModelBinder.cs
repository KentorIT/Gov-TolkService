using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Services
{
    public class TimeSpanModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var timeValue = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}");

            if (timeValue == ValueProviderResult.None
                || string.IsNullOrWhiteSpace(timeValue.FirstValue))
            {
                return Task.CompletedTask;
            }

            var timeValueSanitized = timeValue.FirstValue.Contains(":")
                ? timeValue.FirstValue
                : timeValue.FirstValue.Insert(timeValue.FirstValue.Length - 2, ":"); // Add colon to time if not exists

            var model = TimeSpan.Parse(timeValueSanitized);

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }
    }
}
