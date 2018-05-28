using System;
using System.Collections.Generic;
using System.Text;
using Tolk.BusinessLogic.Data;

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
                rest = rest == 1 ? 0 : rest - 2; // Special case if start is Saturday and end is Sunday.
            }
            else
            {
                if(firstDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    rest -= 1;
                }

                if (secondDate.DayOfWeek == DayOfWeek.Saturday)
                {
                    rest -= 1;
                }
            }

            return fullWeeks * 5 + rest;
        }
    }
}
