using Microsoft.AspNetCore.Mvc.ModelBinding;
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
    }
}
