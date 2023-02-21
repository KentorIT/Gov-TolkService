using System;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class FlexibleTimeRange: IModel
    {
        [Display(Name = "Datum")]
        [Required(ErrorMessage = "Ange datum")]
        public DateTime StartDate { get; set; }

        public TimeSpan FlexibleStartTime { get; set; }

        public TimeSpan FlexibleEndTime { get; set; }

        public DateTimeOffset? FlexibleStartDateTime
        {
            get
            {
                if (StartDate.Year < 2)
                {
                    return null;
                }
                return StartDate.Add(FlexibleStartTime).ToDateTimeOffsetSweden();
            }
            set
            {
                var valueSweden = value.Value.ToDateTimeOffsetSweden();
                StartDate = valueSweden.Date;
                FlexibleStartTime = valueSweden.TimeOfDay;
            }
        }

        private DateTime GetEndDate(TimeSpan endTime)
        {
            return StartDate.AddDays(
                endTime < FlexibleStartTime ? 1 : 0);
        }

        public DateTimeOffset? FlexibleEndDateTime
        {
            get
            {
                if (StartDate.Year < 2)
                {
                    return null;
                }
                var endDate = GetEndDate(FlexibleEndTime);

                return endDate.Add(FlexibleEndTime).ToDateTimeOffsetSweden();
            }
            set
            {
                var valueSweden = value.Value.ToDateTimeOffsetSweden();

                var endDateFromTime = GetEndDate(valueSweden.TimeOfDay);

                if (endDateFromTime != valueSweden.Date)
                {
                    throw new InvalidOperationException("TimeRange can only express positive ranges of up to 24 hours. "
                        + $"Automatically calculated end date {endDateFromTime.ToShortDateString()} "
                        + $"doesn't match supplied end date {value.Value.Date.ToShortDateString()}");
                }

                FlexibleEndTime = valueSweden.TimeOfDay;
            }
        }

        public TimeSpan ExpectedLength { get; set; }

        public string AsSwedishString =>
            $"Förväntad längd: {ExpectedLength.ToSwedishString("hh\\:mm")}<br/>{StartDate.ToSwedishString("yyyy-MM-dd")} mellan {FlexibleStartTime.ToSwedishString("hh\\:mm")}-{FlexibleEndTime.ToSwedishString("hh\\:mm")}";

    }
}
