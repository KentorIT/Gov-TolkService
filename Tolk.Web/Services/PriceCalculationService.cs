using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Services
{
    public class PriceCalculationService
    {
        private readonly TolkDbContext _dbContext;

        public PriceCalculationService(TolkDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public decimal GetPrices(DateTimeOffset startDateTime, DateTimeOffset endDateTime, CompetenceLevel competenceLevel, PriceListType listType, decimal brokerFeePercent)
        {
            //TODO: Handle if there are no rows due to start/end date restrictions. Should take the one with the latest end date in that case.
            decimal extraTimePrice = 0;
            var minutesPerPriceType = GetMinutesPerPriceType(startDateTime, endDateTime).ToList();
            //Get the rows for the list type, startdate and competence first, to get one call to db...
            var prices = _dbContext.PriceListRows
                .Where(r =>
                    r.CompetenceLevel == competenceLevel &&
                    r.PriceListType == listType &&
                    r.StartDate <= startDateTime.DateTime && r.EndDate >= endDateTime.DateTime).ToList();
            decimal price = prices
                .OrderBy(r => r.MaxMinutes)
                .First(r =>
                r.PriceRowType == PriceRowType.BasePrice &&
                r.MaxMinutes >= minutesPerPriceType.Single(c => c.PriceRowType == PriceRowType.BasePrice).Minutes).Price;
            foreach (var priceTime in minutesPerPriceType.Where(c => c.Minutes > 0 && c.PriceRowType != PriceRowType.BasePrice))
            {
                var extraPriceInfo = prices.Single(r => r.PriceRowType == priceTime.PriceRowType);
                int n = priceTime.Minutes / extraPriceInfo.MaxMinutes;
                if (priceTime.Minutes % extraPriceInfo.MaxMinutes > 0)
                {
                    n++;
                }
                extraTimePrice += n * extraPriceInfo.Price;
            }
            //The broker fee should be calculated from baseBrice maxMinutes 60!
            var brokerFee = brokerFeePercent * prices.Single(r => r.PriceRowType == PriceRowType.BasePrice && r.MaxMinutes == 60).Price;
            return price + extraTimePrice + brokerFee;
        }

        private IEnumerable<PriceTime> GetMinutesPerPriceType(DateTimeOffset startDateTime, DateTimeOffset endDateTime)
        {
            int maxMinutes = 330;
            TimeSpan span = endDateTime - startDateTime;
            int totalMinutes = (int)span.TotalMinutes;
            int extraMinutes = 0;
            if (totalMinutes > maxMinutes)
            {
                extraMinutes = totalMinutes - maxMinutes;
                totalMinutes = maxMinutes;
            }
            yield return new PriceTime { Minutes = totalMinutes, PriceRowType = PriceRowType.BasePrice };
            yield return new PriceTime { Minutes = extraMinutes, PriceRowType = PriceRowType.PriceOverMaxTime };

            var start = startDateTime.LocalDateTime;
            var stop = endDateTime.LocalDateTime;
            var bigHolidayWeekendIWH = new PriceTime { Minutes = 0, PriceRowType = PriceRowType.BigHolidayWeekendIWH };
            var weekendIWH = new PriceTime { Minutes = 0, PriceRowType = PriceRowType.WeekendIWH };
            var weekdayIWH = new PriceTime { Minutes = 0, PriceRowType = PriceRowType.InconvenientWorkingHours };
            while (start <= stop)
            {
                var dateTypes = GetDateTypes(start);
                if (dateTypes.Contains(DateType.BigHolidayFullDay) ||
                    dateTypes.Contains(DateType.Holiday) ||
                    (dateTypes.Contains(DateType.Weekend) && !dateTypes.Any(t => t == DateType.DayBeforeBigHoliday || t == DateType.DayAfterBigHoliday)))
                {
                    DateTimeOffset endOfDay = (start.Date == stop.Date ? stop : start.Date.AddDays(1));
                    TimeSpan iwhSpan = endOfDay - start;
                    if (dateTypes.Contains(DateType.BigHolidayFullDay))
                    {
                        bigHolidayWeekendIWH.Minutes += (int)iwhSpan.TotalMinutes;
                    }
                    else
                    {
                        weekendIWH.Minutes += (int)iwhSpan.TotalMinutes;
                    }
                }

                //Find any minutes before 07:00
                if (dateTypes.Any(t => t == DateType.WeekDay || t == DateType.DayAfterBigHoliday) && start.TimeOfDay < new TimeSpan(7, 0, 0))
                {
                    DateTimeOffset endOfPeriod = (start.Date < stop.Date || stop.TimeOfDay > new TimeSpan(7, 0, 0) ? start.Date.AddHours(7) : stop);
                    TimeSpan iwhSpan = endOfPeriod - start;
                    if (dateTypes.Contains(DateType.DayAfterBigHoliday))
                    {
                        bigHolidayWeekendIWH.Minutes += (int)iwhSpan.TotalMinutes;
                    }
                    else
                    {
                        weekdayIWH.Minutes += (int)iwhSpan.TotalMinutes;
                    }
                }

                if (dateTypes.Any(t => t == DateType.WeekDay || t == DateType.DayBeforeBigHoliday) && (start.Date < stop.Date || stop.TimeOfDay > new TimeSpan(18, 0, 0)))
                {
                    DateTimeOffset endOfPeriod = (start.Date == stop.Date ? stop : start.Date.AddDays(1));
                    TimeSpan iwhSpan = endOfPeriod - start.Date.AddHours(18);
                    if (dateTypes.Contains(DateType.DayBeforeBigHoliday))
                    {
                        bigHolidayWeekendIWH.Minutes += (int)iwhSpan.TotalMinutes;
                    }
                    else
                    {
                        weekdayIWH.Minutes += (int)iwhSpan.TotalMinutes;
                    }
                }
                //dateTypes.Contains(DateType.Weekend) && dateTypes.Any( t => t == DateType.DayBeforeBigHoliday) 00:00 => 18:00
                if (dateTypes.Contains(DateType.Weekend) && dateTypes.Any(t => t == DateType.DayBeforeBigHoliday) &&
                   start.TimeOfDay < new TimeSpan(18, 0, 0))
                {
                    DateTimeOffset endOfPeriod = (start.Date < stop.Date || stop.TimeOfDay > new TimeSpan(18, 0, 0) ? start.Date.AddHours(18) : stop);
                    TimeSpan iwhSpan = endOfPeriod - start;
                    weekendIWH.Minutes += (int)iwhSpan.TotalMinutes;
                }
                //dateTypes.Contains(DateType.Weekend) && dateTypes.Any( t => t == DateType.DayAfterBigHoliday) 07:00 => 24:00
                if (dateTypes.Contains(DateType.Weekend) && dateTypes.Any(t => t == DateType.DayAfterBigHoliday) &&
                    (start.Date < stop.Date || stop.TimeOfDay > new TimeSpan(7, 0, 0)))
                {
                    DateTimeOffset endOfPeriod = (start.Date == stop.Date ? stop : start.Date.AddDays(1));
                    TimeSpan iwhSpan = endOfPeriod - start.Date.AddHours(7);
                    weekendIWH.Minutes += (int)iwhSpan.TotalMinutes;
                }

                //Start counting from the first minute on next day
                start = start.AddDays(1).Date;
            }
            yield return weekendIWH;
            yield return weekdayIWH;
            yield return bigHolidayWeekendIWH;
        }

        private IEnumerable<DateType> GetDateTypes(DateTime date)
        {
            yield return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday ? DateType.Weekend : DateType.WeekDay;
            var holidayDayType = GetHolidayDayTypeIfAny(date);
            if (holidayDayType.HasValue)
            {
                yield return holidayDayType.Value;
            }
        }

        private DateType? GetHolidayDayTypeIfAny(DateTime date)
        {
            //TODO, Cache this table...
            return _dbContext.Holidays.SingleOrDefault(h => h.Date.Date == date.Date)?.DateType;
        }
    }
}
