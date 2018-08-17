using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tolk.BusinessLogic.Helpers;

namespace Tolk.Web.Services
{
    public class DateTimeOffsetModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            DateTime dateTime;
            var dateValue = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}.Date");
            var timeValue = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}.TimeOfDay");

            // Date always required
            if (dateValue == ValueProviderResult.None
                || string.IsNullOrWhiteSpace(dateValue.FirstValue))
            {
                return Task.CompletedTask;
            }
            // Time required if type isn't nullable 
            // Time should probably always be "nullable" (sets automatically to 00:00 if null). This check might be better suited for an external form validation method, where applicable.
            if (!bindingContext.ModelMetadata.IsNullableValueType 
                && (timeValue == ValueProviderResult.None
                    || string.IsNullOrWhiteSpace(timeValue.FirstValue)))
            {
                return Task.CompletedTask;
            }

            if (timeValue == ValueProviderResult.None || string.IsNullOrWhiteSpace(timeValue.FirstValue))
            {
                dateTime = DateTime.Parse(dateValue.FirstValue);
            }
            else
            {
                var timeValueSanitized = timeValue.FirstValue.Contains(":")
                    ? timeValue.FirstValue
                    : timeValue.FirstValue.Insert(timeValue.FirstValue.Length - 2, ":"); // Add colon to time if not exists
                dateTime = DateTime.Parse($"{dateValue.FirstValue} {timeValueSanitized}");
            }

            var model = dateTime.ToDateTimeOffsetSweden();

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }
    }
}