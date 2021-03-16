using System;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Services
{
    public class DateCalculationService
    {
        private readonly CacheService _cacheService;

        public DateCalculationService(CacheService cacheService)
        {
            _cacheService = cacheService;
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

            if (firstDate.Kind != secondDate.Kind)
            {
                throw new ArgumentException($"{nameof(firstDate)} has kind {firstDate.Kind} which is different from {nameof(secondDate)} kind {secondDate.Kind}");
            }

            int fullWeeks = (secondDate - firstDate).Days / 7;

            int rest = (secondDate - firstDate).Days % 7;

            if (secondDate.DayOfWeek < firstDate.DayOfWeek) // Wraps over a weekend.
            {
                rest -= secondDate.DayOfWeek == DayOfWeek.Sunday ? 1 : 2;
            }
            else
            {
                rest -= firstDate.DayOfWeek == DayOfWeek.Sunday ? 1 : 0;
            }

            rest -= _cacheService.Holidays.Where(h =>
            NonWorkingHolidays.Contains(h.DateType)
            && h.Date >= firstDate && h.Date < secondDate)
            .AsEnumerable()
            .Where(h => h.Date.DayOfWeek >= DayOfWeek.Monday && h.Date.DayOfWeek <= DayOfWeek.Friday)
            .Count();

            return fullWeeks * 5 + rest;
        }


        /// <summary>
        /// Returns -1 if firstDate > secondDate
        /// </summary>
        /// <param name="firstDate"></param>
        /// <param name="secondDate"></param>
        /// <returns></returns>
        public int GetNoOf24HsPeriodsWorkDaysBetween(DateTime firstDate, DateTime secondDate)
        {
            return secondDate < firstDate ? -1 : ReturnWorkingPeriod(firstDate, secondDate, true);
        }

        public int GetNoOfHoursOfWorkDaysBetween(DateTime firstDate, DateTime secondDate)
        {
            return ReturnWorkingPeriod(firstDate, secondDate, false);
        }

        private int ReturnWorkingPeriod(DateTime firstDate, DateTime secondDate, bool returnDays)
        {
            if (secondDate < firstDate)
            {
                throw new ArgumentException("First Date must be before secondDate");
            }
            if (firstDate.Kind != secondDate.Kind)
            {
                throw new ArgumentException($"{nameof(firstDate)} has kind {firstDate.Kind} which is different from {nameof(secondDate)} kind {secondDate.Kind}");
            }

            firstDate = !IsWorkingDay(firstDate.Date) ? GetFirstWorkDay(firstDate.Date) : firstDate; //get midnight if we get a new first workday

            secondDate = !IsWorkingDay(secondDate.Date) ? GetLastWorkDay(secondDate.Date).AddDays(1) : secondDate; //add one day if we get a new last workday since the period should go to midnight since we back the date

            if (firstDate < secondDate)
            {
                int noOfNonWorkDaysBetween = 0;

                DateTime testDate = firstDate.Date;

                //do not try secondDate since one extra day might be added (if not we know it's a non-work-day)
                while (testDate < secondDate.Date)
                {
                    if (!IsWorkingDay(testDate))
                    {
                        noOfNonWorkDaysBetween++;
                    }
                    testDate = testDate.AddDays(1);
                }
                if (returnDays)
                {
                    return (int)((secondDate - firstDate).TotalSeconds - (noOfNonWorkDaysBetween * 24 * 60 * 60)) / (24 * 60 * 60);
                }
                return (int)((secondDate - firstDate).TotalSeconds - (noOfNonWorkDaysBetween * 24 * 60 * 60)) / (60 * 60);
            }
            else return 0;
        }

        public bool IsWorkingDay(DateTime testDate)
        {
            if (testDate.DayOfWeek == DayOfWeek.Saturday || testDate.DayOfWeek == DayOfWeek.Sunday)
            {
                return false;
            }
            if (_cacheService.Holidays.Any(h => NonWorkingHolidays.Contains(h.DateType) && h.Date == testDate.Date))
            {
                return false;
            }
            return true;
        }

        private readonly DateType[] NonWorkingHolidays = new[] { DateType.BigHolidayFullDay, DateType.Holiday };

        /// <summary>
        /// If date is not a workday it returns the first workday after date, else it returns date.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public DateTime GetFirstWorkDay(DateTime date)
        {
            while (true)
            {
                switch (date.DayOfWeek)
                {
                    case DayOfWeek.Saturday:
                        date = date.AddDays(2);
                        break;
                    case DayOfWeek.Sunday:
                        date = date.AddDays(1);
                        break;
                    default:
                        break;
                }

                if (_cacheService.Holidays.Any(h => NonWorkingHolidays.Contains(h.DateType) && h.Date == date.Date))
                {
                    date = date.AddDays(1);
                    continue;
                }
                break;
            }

            return date;
        }

        /// <summary>
        /// If date is not a workday it returns the last workday before date, else it returns date.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public DateTime GetLastWorkDay(DateTime date)
        {
            while (true)
            {
                switch (date.DayOfWeek)
                {
                    case DayOfWeek.Saturday:
                        date = date.AddDays(-1);
                        break;
                    case DayOfWeek.Sunday:
                        date = date.AddDays(-2);
                        break;
                    default:
                        break;
                }
                if (_cacheService.Holidays.Any(h => NonWorkingHolidays.Contains(h.DateType) && h.Date == date.Date))
                {
                    date = date.AddDays(-1);
                    continue;
                }
                break;
            }
            return date;
        }
    }
}
