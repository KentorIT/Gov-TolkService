using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;

namespace Tolk.Web.Services
{
    public class TimeSpanModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            NullCheckHelper.ArgumentCheckNull(bindingContext, nameof(TimeSpanModelBinder));
            if (bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}") != ValueProviderResult.None)
            {
                return BindSingleInput(bindingContext);
            }
            else
            {
                return BindSeparated(bindingContext);
            }
        }

        private static Task BindSeparated(ModelBindingContext bindingContext)
        {
            var timeHourValue = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}.Hours");
            var timeMinuteValue = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}.Minutes");

            if (!ValueDefinedAndUsed(timeHourValue) || !ValueDefinedAndUsed(timeHourValue))
            {
                return Task.CompletedTask;
            }

            var model = new TimeSpan(timeHourValue.FirstValue.ToSwedishInt(), timeMinuteValue.FirstValue.ToSwedishInt(), 0);

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }

        private static Task BindSingleInput(ModelBindingContext bindingContext)
        {
            var timeValue = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}");

            if (timeValue == ValueProviderResult.None
                || string.IsNullOrWhiteSpace(timeValue.FirstValue))
            {
                return Task.CompletedTask;
            }
            string timeValueSanitized;

            if (timeValue.FirstValue == "0"
                || timeValue.FirstValue == "00")
            {
                timeValueSanitized = "00:00";
            }
            else
            {
                timeValueSanitized = timeValue.FirstValue.ContainsSwedish(":")
                    ? timeValue.FirstValue
                    : timeValue.FirstValue.Insert(timeValue.FirstValue.Length - 2, ":"); // Add colon to time if not exists
            }

            var model = timeValueSanitized.ToSwedishTimeSpan();

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }

        private static bool ValueDefinedAndUsed(ValueProviderResult vpr)
        {
            return !(vpr == ValueProviderResult.None || string.IsNullOrWhiteSpace(vpr.FirstValue));
        }

    }
}
