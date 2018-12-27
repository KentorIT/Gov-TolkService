using System;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Helpers;

namespace Tolk.Web.Models
{
    public class SplitTimeRange : TimeRange
    {
        [Display(Name = "Starttid")]
        [Required(ErrorMessage = "Ange starttid")]
        public int StartTimeHour { get; set; }

        [Required(ErrorMessage = "Ange starttid")]
        public int StartTimeMinutes { get; set; }

        [Display(Name = "Sluttid")]
        [Required(ErrorMessage = "Ange sluttid")]
        public int EndTimeHour { get; set; }

        [Required(ErrorMessage = "Ange sluttid")]
        public int EndTimeMinutes { get; set; }

        public DateTimeOffset StartAt
        {
            get
            {
                DateTimeOffset test = StartDate.AddHours(StartTimeHour).AddMinutes(StartTimeMinutes).ToDateTimeOffsetSweden();
                return StartDate.AddHours(StartTimeHour).AddMinutes(StartTimeMinutes).ToDateTimeOffsetSweden();
            }
            set
            {
                var valueSweden = value.ToDateTimeOffsetSweden();
                StartDate = valueSweden.Date;
                StartTimeHour = valueSweden.Hour;
                StartTimeMinutes = valueSweden.Minute;
            }
        }

        private DateTime GetEndDate(int endHour, int endMinute)
        {
            return StartDate.AddDays((endHour < StartTimeHour) || (endHour == StartTimeHour && endMinute < StartTimeMinutes) ? 1 : 0);
        }

        public DateTimeOffset EndAt
        {
            get => GetEndDate(EndTimeHour, EndTimeMinutes).AddHours(EndTimeHour).AddMinutes(EndTimeMinutes).ToDateTimeOffsetSweden();
            set
            {
                var valueSweden = value.ToDateTimeOffsetSweden();

                var endDateFromTime = GetEndDate(valueSweden.Hour, valueSweden.Minute);

                if (endDateFromTime != valueSweden.Date)
                {
                    throw new InvalidOperationException("TimeRange can only express positive ranges of up to 24 hours. "
                        + $"Automatically calculated end date {endDateFromTime.ToShortDateString()} "
                        + $"doesn't match supplied end date {value.Date.ToShortDateString()}");
                }

                EndTimeHour = valueSweden.Hour;
                EndTimeMinutes = valueSweden.Minute;
            }
        }
    }
}
