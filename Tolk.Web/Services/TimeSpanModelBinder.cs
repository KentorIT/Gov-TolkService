using DocumentFormat.OpenXml.Bibliography;
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

        private static bool ValueDefinedAndUsed(ValueProviderResult vpr)
        {
            return !(vpr == ValueProviderResult.None || string.IsNullOrWhiteSpace(vpr.FirstValue));
        }

    }
}
