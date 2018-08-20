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
            DateTimeOffset? dateFrom, dateTo;
            var fromValue = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}.Start");
            var toValue = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}.End");

            if (fromValue == ValueProviderResult.None || string.IsNullOrWhiteSpace(fromValue.FirstValue))
            {
                dateFrom = null;
            }
            else
            {
                dateFrom = DateTimeOffset.Parse(fromValue.FirstValue);
            }

            if (toValue == ValueProviderResult.None || string.IsNullOrWhiteSpace(toValue.FirstValue))
            {
                dateTo = null;
            }
            else
            {
                dateTo = DateTimeOffset.Parse(toValue.FirstValue);
            }

            var model = new DateRange { Start = dateFrom, End = dateTo };

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }
    }
}