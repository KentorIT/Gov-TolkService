using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
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
            var minutesPerPriceType = GetPriceRowsPerType(startAt, endAt, prices, wasteStartAt, wasteEndAt).ToList();

            //The broker fee should be calculated from baseBrice maxMinutes 60.
            int days = (endAt.Date - startAt.Date).Days + 1;

            var brokerFee = prices.Single(r => r.PriceRowType == PriceRowType.BasePrice && r.MaxMinutes == 60);
            minutesPerPriceType.Add(new PriceRow
            {
                StartAt = startAt,
                EndAt = startAt,
                PriceRowType = PriceRowType.BasePrice,
                Quantity = days,
                Price = brokerFee.Price * brokerFeePercent,
                PriceListRowId = brokerFee.PriceListRowId,
                IsBrokerFee = true
            });
            var priceInformation = new PriceInformation
            {
                PriceRows = minutesPerPriceType
            };

            return priceInformation;
        }

        private static PriceRow GetPriceInformation(DateTimeOffset startAt, DateTimeOffset endAt, PriceRowType rowType, List<PriceListRow> prices)
        {
            PriceListRow priceInfo = prices.Single(r => r.PriceRowType == rowType);
            var priceTime = new PriceRow
            {
                StartAt = startAt,
                EndAt = endAt,
                PriceRowType = rowType
            };
            priceTime.Quantity = priceTime.Minutes / priceInfo.MaxMinutes;
            if (priceTime.Minutes % priceInfo.MaxMinutes > 0)
            {
                priceTime.Quantity++;
            }
            priceTime.PriceListRowId = priceInfo.PriceListRowId;
            priceTime.Price = priceInfo.Price;
            return priceTime;
        }

        private IEnumerable<PriceRow> GetPriceRowsPerType(DateTimeOffset startAt, DateTimeOffset endAt, List<PriceListRow> prices, DateTimeOffset? wasteStartAt, DateTimeOffset? wasteEndAt)
        {
            int maxMinutes = 330;
            TimeSpan span = endAt - startAt;
            int totalMinutes = (int)span.TotalMinutes;
            if (totalMinutes > maxMinutes)
            {
                DateTimeOffset extraTimeStartsAt = startAt.AddMinutes(maxMinutes);
                var basePrice = prices.Single(r => r.PriceRowType == PriceRowType.BasePrice && r.MaxMinutes >= maxMinutes);
                yield return new PriceRow
                {
                    StartAt = startAt,
                    EndAt = extraTimeStartsAt,
                    PriceRowType = PriceRowType.BasePrice,
                    Quantity = 1,
                    PriceListRowId = basePrice.PriceListRowId,
                    Price = basePrice.Price
                };
                //Calculate when the extra time starts, date wize.
                yield return GetPriceInformation(extraTimeStartsAt, endAt, PriceRowType.PriceOverMaxTime, prices);
            }
            else
            {
                var basePrice = prices.OrderBy(p => p.MaxMinutes)
                    .First(r => r.PriceRowType == PriceRowType.BasePrice && r.MaxMinutes >= totalMinutes);
                yield return new PriceRow
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
                        start,
                        (start.Date == stop.Date ? stop : start.Date.AddDays(1)),
                        dateTypes.Contains(DateType.BigHolidayFullDay) ? PriceRowType.BigHolidayWeekendIWH : PriceRowType.WeekendIWH,
                        prices
                    );
                }

                //Find any minutes before 07:00
                if (!dateTypes.Contains(DateType.Holiday) &&
                    dateTypes.Any(t => t == DateType.WeekDay || t == DateType.DayAfterBigHoliday) &&
                    start.TimeOfDay < new TimeSpan(7, 0, 0))
                {
                    yield return GetPriceInformation(
                        start,
                        (start.Date < stop.Date || stop.TimeOfDay > new TimeSpan(7, 0, 0) ? start.Date.AddHours(7) : stop),
                        dateTypes.Contains(DateType.DayAfterBigHoliday) ? PriceRowType.BigHolidayWeekendIWH : PriceRowType.InconvenientWorkingHours,
                        prices
                    );
                }

                if (!dateTypes.Contains(DateType.Holiday) &&
                    dateTypes.Any(t => t == DateType.WeekDay || t == DateType.DayBeforeBigHoliday) &&
                    (start.Date < stop.Date || stop.TimeOfDay > new TimeSpan(18, 0, 0)))
                {
                    yield return GetPriceInformation(
                        (start.Hour < 18 ? start.Date.AddHours(18) : start),
                        (start.Date == stop.Date ? stop : start.Date.AddDays(1)),
                        dateTypes.Contains(DateType.DayBeforeBigHoliday) ? PriceRowType.BigHolidayWeekendIWH : PriceRowType.InconvenientWorkingHours,
                        prices
                    );
                }
                //dateTypes.Contains(DateType.Weekend) && dateTypes.Any( t => t == DateType.DayBeforeBigHoliday) 00:00 => 18:00
                if ((dateTypes.Contains(DateType.Weekend) || dateTypes.Contains(DateType.Holiday)) && dateTypes.Any(t => t == DateType.DayBeforeBigHoliday) &&
                   start.TimeOfDay < new TimeSpan(18, 0, 0))
                {
                    yield return GetPriceInformation(
                        start,
                        (start.Date < stop.Date || stop.TimeOfDay > new TimeSpan(18, 0, 0) ? start.Date.AddHours(18) : stop),
                        PriceRowType.WeekendIWH,
                        prices
                     );
                }
                //dateTypes.Contains(DateType.Weekend) && dateTypes.Any( t => t == DateType.DayAfterBigHoliday) 07:00 => 24:00
                if ((dateTypes.Contains(DateType.Weekend) || dateTypes.Contains(DateType.Holiday)) && dateTypes.Any(t => t == DateType.DayAfterBigHoliday) &&
                    (start.Date < stop.Date || stop.TimeOfDay > new TimeSpan(7, 0, 0)))
                {
                    yield return GetPriceInformation(
                        start.Date.AddHours(7),
                        (start.Date == stop.Date ? stop : start.Date.AddDays(1)),
                        PriceRowType.WeekendIWH,
                        prices
                    );
                }

                //Start counting from the first minute on next day
                start = start.AddDays(1).Date;
            }

            //Get lost times, if any
            if (wasteStartAt.HasValue)
            {
                yield return GetPriceInformation(wasteStartAt.Value, startAt, PriceRowType.LostTime, prices);
                foreach (var row in GetIWHWasteTimePriceRows(wasteStartAt.Value, startAt, prices))
                {
                    yield return row;
                }

            }
            if (wasteEndAt.HasValue)
            {
                yield return GetPriceInformation(endAt, wasteEndAt.Value, PriceRowType.LostTime, prices);
                foreach (var row in GetIWHWasteTimePriceRows(endAt, wasteEndAt.Value, prices))
                {
                    yield return row;
                }
            }
        }

        private IEnumerable<PriceRow> GetIWHWasteTimePriceRows(DateTimeOffset startAt, DateTimeOffset endAt, List<PriceListRow> prices)
        {
            var start = startAt.LocalDateTime;
            var stop = endAt.LocalDateTime;
            while (start <= stop)
            {
                var dateTypes = GetDateTypes(start);
                if (dateTypes.Contains(DateType.BigHolidayFullDay) ||
                    dateTypes.Contains(DateType.Holiday) ||
                    dateTypes.Contains(DateType.Weekend))
                {
                    yield return GetPriceInformation(start, (start.Date == stop.Date ? stop : start.Date.AddDays(1)), PriceRowType.LostTimeIWH, prices);
                }
                else
                {
                    //Find any minutes before 07:00, only on a weekday.
                    if (start.TimeOfDay < new TimeSpan(7, 0, 0))
                    {
                        yield return GetPriceInformation(
                            start,
                            (start.Date < stop.Date || stop.TimeOfDay > new TimeSpan(7, 0, 0) ? start.Date.AddHours(7) : stop),
                            PriceRowType.LostTimeIWH,
                            prices
                        );
                    }

                    if ((start.Date < stop.Date || stop.TimeOfDay > new TimeSpan(18, 0, 0)))
                    {
                        yield return GetPriceInformation(
                            (start.Hour < 18 ? start.Date.AddHours(18) : start),
                            (start.Date == stop.Date ? stop : start.Date.AddDays(1)),
                            PriceRowType.LostTimeIWH,
                            prices
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

        public DisplayPriceInformation GetPriceInformationToDisplay(List<PriceRowBase> priceRows)
        {

            DisplayPriceInformation dpi = new DisplayPriceInformation();
            int numberOfBrokerFees = 0;
            string extraBrokerFee = string.Empty;
            string hourTaxDescription = string.Empty;
            foreach (PriceRowBase priceRow in priceRows.OrderByDescending(r => r.IsBrokerFee))
            {
                if (priceRow.IsBrokerFee)
                {
                    numberOfBrokerFees += 1;
                    extraBrokerFee = numberOfBrokerFees > 1 ? $" dygn {numberOfBrokerFees}" : string.Empty;
                }
                else
                {
                    hourTaxDescription = priceRow.PriceListRow.PriceRowType == PriceRowType.BasePrice ?  $", taxa {GetDescriptionHourTax(priceRow.PriceListRow.MaxMinutes)} h" : string.Empty;
                }
                string startDescription = priceRow.IsBrokerFee ? $"Förmedlingsavgift{extraBrokerFee}" : priceRow.PriceListRow.PriceRowType.GetDescription() + hourTaxDescription;
                dpi.DisplayPriceRows.Add(new DisplayPriceRow { Description = $"{startDescription} för tolktyp {priceRow.PriceListRow.CompetenceLevel.GetDescription()}", Price = priceRow.TotalPrice });
            }
            return dpi;
        }

        private string GetDescriptionHourTax(int maxMinutes)
        {
            double noOfHours = (double)maxMinutes/60;

            switch (noOfHours)
            {
                case 1:
                    return $"0-{noOfHours}";
                default:
                    return $"{noOfHours-0.5}-{noOfHours}";
            }
        }
    }
}
