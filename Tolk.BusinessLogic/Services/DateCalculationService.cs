using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Helpers
{
    public class DateCalculationService
    {
        private TolkDbContext _tolkDbContext;

        public DateCalculationService(TolkDbContext tolkDbContext)
        {
            _tolkDbContext = tolkDbContext;
        }

        public int GetWorkDaysBetween(DateTime firstDate, DateTime secondDate)
        {
            if (secondDate < firstDate)
            {
                throw new ArgumentException("First Date must be before secondDate");
            }

            if (firstDate.TimeOfDay.Ticks != 0)
            {
                throw new ArgumentException($"{nameof(firstDate)} includes a time other than midnight. Use the Date property to get a pure date.", nameof(firstDate));
            }

            if (secondDate.TimeOfDay.Ticks != 0)
            {
                throw new ArgumentException($"{nameof(secondDate)} includes a time other than midnight. Use the Date property to get a pure date.", nameof(secondDate));
            }

            if(firstDate.Kind != secondDate.Kind)
            {
                throw new ArgumentException($"{nameof(firstDate)} has kind {firstDate.Kind} which is different from {nameof(secondDate)} kind {secondDate.Kind}");
            }

            int fullWeeks = ((secondDate - firstDate).Days / 7);

            int rest = ((secondDate - firstDate).Days % 7);

            if(secondDate.DayOfWeek < firstDate.DayOfWeek) // Wraps over a weekend.
            {
                if(secondDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    rest -= 1;
                }
                else
                {
                    rest -= 2;
                }
            }
            else
            {
                if(firstDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    rest -= 1;
                }
            }

            var nonWorkingHolidays = new[] { DateType.BigHolidayFullDay, DateType.Holiday };

            rest -= _tolkDbContext.Holidays.Where(h =>
            nonWorkingHolidays.Contains(h.DateType)
            && h.Date >= firstDate && h.Date < secondDate)
            .AsEnumerable()
            .Where(h => h.Date.DayOfWeek >= DayOfWeek.Monday && h.Date.DayOfWeek <= DayOfWeek.Friday)
            .Count();

            return fullWeeks * 5 + rest;
        }
    }
}
