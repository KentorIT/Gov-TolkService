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

        public PriceInformation GetPrices(DateTimeOffset startAt, DateTimeOffset endAt, CompetenceLevel competenceLevel, PriceListType listType, decimal brokerFeePercent, DateTimeOffset? wasteStartAt = null, DateTimeOffset? wasteEndAt = null)
        {
            //TODO: Should get the prices from the creation date, not the start date. At least on order creation...
            var prices = _dbContext.PriceListRows
                .Where(r =>
                    r.CompetenceLevel == competenceLevel &&
                    r.PriceListType == listType &&
                    r.StartDate <= startAt.DateTime && r.EndDate >= endAt.DateTime).ToList();
            var minutesPerPriceType = GetMinutesPerPriceType(startAt, endAt, prices, wasteStartAt, wasteEndAt).ToList();

            //The broker fee should be calculated from baseBrice maxMinutes 60.
            int days = (endAt.Date - startAt.Date).Days + 1;

            var brokerFee = prices.Single(r => r.PriceRowType == PriceRowType.BasePrice && r.MaxMinutes == 60);
            minutesPerPriceType.Add(new PriceTime
            {
                StartAt = startAt,
                EndAt = startAt,
                PriceRowType = PriceRowType.BasePrice,
                Quantity = days,
                Price = brokerFee.Price * brokerFeePercent,
                IsBrokerFee = true
            });
            var priceInformation = new PriceInformation
            {
                StartAt = startAt,
                EndAt = endAt,
                PriceRows = minutesPerPriceType
            };

            return priceInformation;
        }

        private static PriceTime GetPriceInformation(List<PriceListRow> prices, PriceTime priceTime)
        {
            var extraPriceInfo = prices.Single(r => r.PriceRowType == priceTime.PriceRowType);
            priceTime.Quantity = priceTime.Minutes / extraPriceInfo.MaxMinutes;
            if (priceTime.Minutes % extraPriceInfo.MaxMinutes > 0)
            {
                priceTime.Quantity++;
            }
            priceTime.PriceListRowId = extraPriceInfo.PriceListRowId;
            priceTime.Price = extraPriceInfo.Price;
            return priceTime;
        }

        private IEnumerable<PriceTime> GetMinutesPerPriceType(DateTimeOffset startAt, DateTimeOffset endAt, List<PriceListRow> prices, DateTimeOffset? wasteStartAt, DateTimeOffset? wasteEndAt)
        {
            int maxMinutes = 330;
            TimeSpan span = endAt - startAt;
            int totalMinutes = (int)span.TotalMinutes;
            if (totalMinutes > maxMinutes)
            {
                DateTimeOffset extraTimeStartsAt = startAt.AddMinutes(maxMinutes);
                var basePrice = prices.Single(r => r.PriceRowType == PriceRowType.BasePrice && r.MaxMinutes >= maxMinutes);
                yield return new PriceTime
                {
                    StartAt = startAt,
                    EndAt = extraTimeStartsAt,
                    PriceRowType = PriceRowType.BasePrice,
                    Quantity = 1,
                    PriceListRowId = basePrice.PriceListRowId,
                    Price = basePrice.Price
                };
                //Calculate when the extra time starts, date wize.
                yield return GetPriceInformation(
                    prices,
                    new PriceTime { StartAt = extraTimeStartsAt, EndAt = endAt, PriceRowType = PriceRowType.PriceOverMaxTime });
            }
            else
            {
                var basePrice = prices.Single(r => r.PriceRowType == PriceRowType.BasePrice && r.MaxMinutes >= totalMinutes);
                yield return new PriceTime
                {
                    StartAt = startAt,
                    EndAt = endAt,
                    PriceRowType = PriceRowType.BasePrice,
                    Quantity = 1,
                    PriceListRowId = basePrice.PriceListRowId,
                    Price = basePrice.Price
                };
            }
            var start = startAt.LocalDateTime;
            var stop = endAt.LocalDateTime;
            while (start <= stop)
            {
                var dateTypes = GetDateTypes(start);
                if (dateTypes.Contains(DateType.BigHolidayFullDay) ||
                    dateTypes.Contains(DateType.Holiday) ||
                    (dateTypes.Contains(DateType.Weekend) && !dateTypes.Any(t => t == DateType.DayBeforeBigHoliday || t == DateType.DayAfterBigHoliday)))
                {
                    yield return GetPriceInformation(
                        prices,
                        new PriceTime
                        {
                            StartAt = start,
                            EndAt = (start.Date == stop.Date ? stop : start.Date.AddDays(1)),
                            PriceRowType = dateTypes.Contains(DateType.BigHolidayFullDay) ? PriceRowType.BigHolidayWeekendIWH : PriceRowType.WeekendIWH
                        }
                    );
                }

                //Find any minutes before 07:00
                if (!dateTypes.Contains(DateType.Holiday) &&
                    dateTypes.Any(t => t == DateType.WeekDay || t == DateType.DayAfterBigHoliday) &&
                    start.TimeOfDay < new TimeSpan(7, 0, 0))
                {
                    yield return GetPriceInformation(
                        prices,
                        new PriceTime
                        {
                            StartAt = start,
                            EndAt = (start.Date < stop.Date || stop.TimeOfDay > new TimeSpan(7, 0, 0) ? start.Date.AddHours(7) : stop),
                            PriceRowType = dateTypes.Contains(DateType.DayAfterBigHoliday) ? PriceRowType.BigHolidayWeekendIWH : PriceRowType.InconvenientWorkingHours
                        }
                    );
                }

                if (!dateTypes.Contains(DateType.Holiday) &&
                    dateTypes.Any(t => t == DateType.WeekDay || t == DateType.DayBeforeBigHoliday) &&
                    (start.Date < stop.Date || stop.TimeOfDay > new TimeSpan(18, 0, 0)))
                {
                    yield return GetPriceInformation(
                        prices,
                        new PriceTime
                        {
                            StartAt = (start.Hour < 18 ? start.Date.AddHours(18) : start),
                            EndAt = (start.Date == stop.Date ? stop : start.Date.AddDays(1)),
                            PriceRowType = dateTypes.Contains(DateType.DayBeforeBigHoliday) ? PriceRowType.BigHolidayWeekendIWH : PriceRowType.InconvenientWorkingHours
                        }
                    );
                }
                //dateTypes.Contains(DateType.Weekend) && dateTypes.Any( t => t == DateType.DayBeforeBigHoliday) 00:00 => 18:00
                if ((dateTypes.Contains(DateType.Weekend) || dateTypes.Contains(DateType.Holiday)) && dateTypes.Any(t => t == DateType.DayBeforeBigHoliday) &&
                   start.TimeOfDay < new TimeSpan(18, 0, 0))
                {
                    yield return GetPriceInformation(
                        prices,
                         new PriceTime
                        {
                            StartAt = start,
                            EndAt = (start.Date < stop.Date || stop.TimeOfDay > new TimeSpan(18, 0, 0) ? start.Date.AddHours(18) : stop),
                            PriceRowType = PriceRowType.WeekendIWH
                        }
                     );
                }
                //dateTypes.Contains(DateType.Weekend) && dateTypes.Any( t => t == DateType.DayAfterBigHoliday) 07:00 => 24:00
                if ((dateTypes.Contains(DateType.Weekend) || dateTypes.Contains(DateType.Holiday)) && dateTypes.Any(t => t == DateType.DayAfterBigHoliday) &&
                    (start.Date < stop.Date || stop.TimeOfDay > new TimeSpan(7, 0, 0)))
                {
                    yield return GetPriceInformation(
                        prices,
                        new PriceTime
                        {
                            StartAt = start.Date.AddHours(7),
                            EndAt = (start.Date == stop.Date ? stop : start.Date.AddDays(1)),
                            PriceRowType = PriceRowType.WeekendIWH
                        }
                    );
                }

                //Start counting from the first minute on next day
                start = start.AddDays(1).Date;
            }

            //Get lost times, if any
            if (wasteStartAt.HasValue)
            {
                yield return GetPriceInformation(
                    prices,
                    new PriceTime { StartAt = wasteStartAt.Value, EndAt = startAt, PriceRowType = PriceRowType.LostTime }
                );
               foreach (var x in GetIWHWasteMinutes(wasteStartAt.Value, startAt, prices))
                {
                    yield return x;
                }

            }
            if (wasteEndAt.HasValue)
            {
                yield return GetPriceInformation(
                    prices,
                    new PriceTime { StartAt = endAt, EndAt = wasteEndAt.Value, PriceRowType = PriceRowType.LostTime }
                );
                foreach (var x in GetIWHWasteMinutes(endAt, wasteEndAt.Value, prices))
                {
                    yield return x;
                }
            }
        }

        private IEnumerable<PriceTime> GetIWHWasteMinutes(DateTimeOffset startedAt, DateTimeOffset endedAt, List<PriceListRow> prices)
        {
            var start = startedAt.LocalDateTime;
            var stop = endedAt.LocalDateTime;
            while (start <= stop)
            {
                var dateTypes = GetDateTypes(start);
                if (dateTypes.Contains(DateType.BigHolidayFullDay) ||
                    dateTypes.Contains(DateType.Holiday) ||
                    dateTypes.Contains(DateType.Weekend))
                {
                    yield return GetPriceInformation(
                        prices,
                        new PriceTime
                        {
                            StartAt = start,
                            EndAt = (start.Date == stop.Date ? stop : start.Date.AddDays(1)),
                            PriceRowType = PriceRowType.LostTimeIWH
                        }
                    );
                }
                else
                {
                    //Find any minutes before 07:00, only on a weekday.
                    if (start.TimeOfDay < new TimeSpan(7, 0, 0))
                    {
                        yield return GetPriceInformation(
                            prices,
                            new PriceTime
                            {
                                StartAt = start,
                                EndAt = (start.Date < stop.Date || stop.TimeOfDay > new TimeSpan(7, 0, 0) ? start.Date.AddHours(7) : stop),
                                PriceRowType = PriceRowType.LostTimeIWH
                            }
                        );
                    }

                    if ((start.Date < stop.Date || stop.TimeOfDay > new TimeSpan(18, 0, 0)))
                    {
                        yield return GetPriceInformation(
                        prices,
                        new PriceTime
                            {
                                StartAt = (start.Hour < 18 ? start.Date.AddHours(18) : start),
                                EndAt = (start.Date == stop.Date ? stop : start.Date.AddDays(1)),
                                PriceRowType = PriceRowType.LostTimeIWH
                            }
                        );
                    }
                }
                //Start counting from the first minute on next day
                start = start.AddDays(1).Date;
            }
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
