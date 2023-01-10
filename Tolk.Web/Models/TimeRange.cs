using System;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class TimeRange : IModel
    {
        [Display(Name = "Datum")]
        [Required(ErrorMessage = "Ange datum")]
        public DateTime StartDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public DateTimeOffset? StartDateTime
        {
            get
            {
                if (StartDate.Year < 2)
                {
                    return null;
                }
                return StartDate.Add(StartTime).ToDateTimeOffsetSweden();
            }
            set
            {
                var valueSweden = value.Value.ToDateTimeOffsetSweden();
                StartDate = valueSweden.Date;
                StartTime = valueSweden.TimeOfDay;
            }
        }

        private DateTime GetEndDate(TimeSpan endTime)
        {
            return StartDate.AddDays(
                endTime < StartTime ? 1 : 0);
        }

        public DateTimeOffset? EndDateTime
        {
            get
            {
                if (StartDate.Year < 2)
                {
                    return null;
                }
                var endDate = GetEndDate(EndTime);

                return endDate.Add(EndTime).ToDateTimeOffsetSweden();
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

                EndTime = valueSweden.TimeOfDay;
            }
        }

        public TimeSpan Duration => (EndDateTime.HasValue && StartDateTime.HasValue) ? new(EndDateTime.Value.Ticks - StartDateTime.Value.Ticks) : new TimeSpan();

        public string AsSwedishString =>
            $"{StartDate.ToSwedishString("yyyy-MM-dd")} {StartTime.ToSwedishString("hh\\:mm")}-{EndTime.ToSwedishString("hh\\:mm")}";

    }
}
