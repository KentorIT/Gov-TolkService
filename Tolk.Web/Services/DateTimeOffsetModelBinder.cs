﻿using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;

namespace Tolk.Web.Services
{
    public class DateTimeOffsetModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            NullCheckHelper.ArgumentCheckNull(bindingContext, nameof(DateTimeOffsetModelBinder));
            var dateValue = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}.Date");
            var timeValue = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}.TimeOfDay");
            var timeHourValue = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}.Hour");
            var timeMinuteValue = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName}.Minute");

            // Date and time always required
            if (!ValueDefinedAndUsed(dateValue) || (!ValueDefinedAndUsed(timeValue) && (!ValueDefinedAndUsed(timeHourValue))))
            {
                return Task.CompletedTask;
            }
            string timeValueSanitized;
            if (ValueDefinedAndUsed(timeValue))
            {

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
            }
            else
            {
                timeValueSanitized = $"{timeHourValue.FirstValue}:{timeMinuteValue.FirstValue}";
            }
            DateTime dateTime = $"{dateValue.FirstValue} {timeValueSanitized}".ToSwedishDateTime();
            var model = dateTime.ToDateTimeOffsetSweden();

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }

        private static bool ValueDefinedAndUsed(ValueProviderResult vpr)
        {
            return !(vpr == ValueProviderResult.None || string.IsNullOrWhiteSpace(vpr.FirstValue));
        }
    }
}