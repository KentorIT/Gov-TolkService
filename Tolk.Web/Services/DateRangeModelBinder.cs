using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Services
{
    public class DateRangeModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var fromValue = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}.Start");
            var toValue = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}.End");

            var dateFrom = ParseDateTimeValue(fromValue);
            var dateTo = ParseDateTimeValue(toValue);

            var model = new DateRange { Start = dateFrom, End = dateTo };

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }

        private DateTimeOffset? ParseDateTimeValue(ValueProviderResult dateValue)
        {
            try
            {
                if (dateValue == ValueProviderResult.None 
                    || string.IsNullOrWhiteSpace(dateValue.FirstValue))
                {
                    return null;
                }
                else
                {
                    return DateTimeOffset.Parse(dateValue.FirstValue);
                }
            }
            catch (FormatException)
            {
                // Invalid format, return null
                return null;
            }
        }
    }
}