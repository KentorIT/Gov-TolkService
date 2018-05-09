using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Tolk.Web.Services
{
    public class DateTimeOffsetModelBinder : IModelBinder
    {
        private static readonly TimeZoneInfo timeZoneInfo =
            TimeZoneInfo.GetSystemTimeZones().Single(tzi => tzi.Id == "W. Europe Standard Time");

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var dateValue = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}.Date");
            var timeValue = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}.TimeOfDay");

            if(dateValue == ValueProviderResult.None
                || string.IsNullOrWhiteSpace(dateValue.FirstValue)
                || timeValue == ValueProviderResult.None
                || string.IsNullOrWhiteSpace(timeValue.FirstValue))
            {
                return Task.CompletedTask;
            }

            var rawDateTime = DateTime.Parse($"{dateValue.FirstValue} {timeValue}");

            var timeZoneOffset = timeZoneInfo.GetUtcOffset(rawDateTime);
            var model = new DateTimeOffset(rawDateTime, timeZoneOffset);

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }
    }
}