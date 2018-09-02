using System;
using Tolk.BusinessLogic.Helpers;

namespace Tolk.Web.Models
{
    public class TimeRange
    {
        public DateTime StartDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public DateTimeOffset StartDateTime
        {
            get
            {
                return StartDate.Add(StartTime).ToDateTimeOffsetSweden();
            }
            set
            {
                StartDate = value.Date;
                StartTime = value.TimeOfDay;
            }
        }

        private DateTime GetEndDate(TimeSpan endTime)
        {
            return StartDate.AddDays(
                endTime < StartTime ? 1 : 0);
        }

        public DateTimeOffset EndDateTime
        {
            get
            {
                var endDate = GetEndDate(EndTime);

                return endDate.Add(EndTime).ToDateTimeOffsetSweden();
            }
            set
            {
                var valueSweden = value.ToDateTimeOffsetSweden();

                var endDateFromTime = GetEndDate(valueSweden.TimeOfDay);

                if(endDateFromTime != valueSweden.Date)
                {
                    throw new InvalidOperationException("TimeRange can only express ranges of up to 24 hours. "
                        + $"Automatically calculated end date {endDateFromTime} doesn't match supplied end date {value.Date}");
                }

                EndDateTime = valueSweden.Date;
                EndTime = valueSweden.TimeOfDay;
            }
        }
    }
}
