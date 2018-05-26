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

            var model = rawDateTime.ToDateTimeOffsetSweden();

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }
    }
}