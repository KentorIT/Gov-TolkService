using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.BusinessLogic.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Tolk.BusinessLogic.Services
{
    public class PriceCalculationService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly TolkOptions _options;
        private readonly IMemoryCache _cache;

        private const string brokerFeesCacheKey = nameof(brokerFeesCacheKey);

        public PriceCalculationService(TolkDbContext dbContext,
            ILogger<PriceCalculationService> logger,
            IOptions<TolkOptions> options,
            IMemoryCache cache = null
            )
        {
            _logger = logger;
            _dbContext = dbContext;
            _options = options.Value;
            _cache = cache;
        }

        public PriceInformation GetPrices(DateTimeOffset startAt, DateTimeOffset endAt, CompetenceLevel competenceLevel, PriceListType listType, int rankingId, int? timeWasteNormalTime = null, int? timeWasteIWHTime = null, IEnumerable<PriceRowBase> brokerFeeToUse = null)
        {
            var prices = _dbContext.PriceListRows
                .Where(r =>
                    r.CompetenceLevel == competenceLevel &&
                    r.PriceListType == listType &&
                    r.StartDate <= startAt.DateTime && r.EndDate >= endAt.DateTime).ToList();
            var minutesPerPriceType = GetPriceRowsPerType(startAt, endAt, prices, timeWasteNormalTime, timeWasteIWHTime).ToList();
            minutesPerPriceType.AddRange(GetPriceRowsBrokerFee(startAt, endAt, competenceLevel, rankingId, brokerFeeToUse));

            var priceInformation = new PriceInformation
            {
                PriceRows = minutesPerPriceType
            };
            return priceInformation;
        }

        private IEnumerable<PriceRow> GetPriceRowsBrokerFee(DateTimeOffset startAt, DateTimeOffset endAt, CompetenceLevel competenceLevel, int rankingId, IEnumerable<PriceRowBase> brokerFeeToUse = null)
        {
            if (brokerFeeToUse != null)
            {
                foreach (var bf in brokerFeeToUse)
                {
                    yield return new PriceRow { StartAt = bf.StartAt, EndAt = bf.EndAt, PriceRowType = PriceRowType.BasePrice, Quantity = 1, Price = bf.TotalPrice, PriceListRowId = bf.PriceListRowId, IsBrokerFee = bf.IsBrokerFee };
                }
            }
            else
            {
                int days = GetNoOfDays(startAt, endAt);
                //One broker fee per calender day.
                var priceRow = BrokerFeePriceList.Where(br => br.RankingId == rankingId && br.CompetenceLevel == competenceLevel && br.StartDate < startAt && br.EndDate > startAt).Single();
                for (int i = 1; i <= days; i++)
                {
                    yield return new PriceRow
                    {
                        StartAt = startAt.Date.AddDays(i - 1).ToDateTimeOffsetSweden(),
                        EndAt = startAt.Date.AddDays(i).ToDateTimeOffsetSweden(),
                        PriceRowType = PriceRowType.BasePrice,
                        Quantity = 1,
                        Price = priceRow.PriceToUse,
                        PriceListRowId = priceRow.PriceListRowId,
                        IsBrokerFee = true
                    };
                }
            }
        }

        private int GetNoOfDays(DateTimeOffset startAt, DateTimeOffset endAt)
        {
            int days = (endAt.Date - startAt.Date).Days + 1;
            days -= endAt.TimeOfDay == TimeSpan.Zero ? 1 : 0; //if ends at midnight no extra day
            return days;
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

        private IEnumerable<PriceRow> GetPriceRowsPerType(DateTimeOffset startAt, DateTimeOffset endAt, List<PriceListRow> prices, int? timeWasteNormalTime, int? timeWasteIWHTime)
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

            var start = startAt;

            while (start < endAt)
            {
                var dateTypes = GetDateTypes(start.Date);
                if (dateTypes.Contains(DateType.BigHolidayFullDay) ||
                    dateTypes.Contains(DateType.Holiday) ||
                    (dateTypes.Contains(DateType.Weekend) && !dateTypes.Any(t => t == DateType.DayBeforeBigHoliday || t == DateType.DayAfterBigHoliday)))
                {
                    yield return GetPriceInformation(
                        start,
                        start.Date == endAt.Date ? endAt : start.Date.AddDays(1).ToDateTimeOffsetSweden(),
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
                        start.Date < endAt.Date || endAt.TimeOfDay > new TimeSpan(7, 0, 0) ? start.Date.AddHours(7).ToDateTimeOffsetSweden() : endAt,
                        dateTypes.Contains(DateType.DayAfterBigHoliday) ? PriceRowType.BigHolidayWeekendIWH : PriceRowType.InconvenientWorkingHours,
                        prices
                    );
                }

                if (!dateTypes.Contains(DateType.Holiday) &&
                    dateTypes.Any(t => t == DateType.WeekDay || t == DateType.DayBeforeBigHoliday) &&
                    (start.Date < endAt.Date || endAt.TimeOfDay > new TimeSpan(18, 0, 0)))
                {
                    yield return GetPriceInformation(
                        start.Hour < 18 ? start.Date.AddHours(18).ToDateTimeOffsetSweden() : start,
                        start.Date == endAt.Date ? endAt : start.Date.AddDays(1).ToDateTimeOffsetSweden(),
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
                        start.Date < endAt.Date || endAt.TimeOfDay > new TimeSpan(18, 0, 0) ? start.Date.AddHours(18).ToDateTimeOffsetSweden() : endAt,
                        PriceRowType.WeekendIWH,
                        prices
                     );
                }
                //dateTypes.Contains(DateType.Weekend) && dateTypes.Any( t => t == DateType.DayAfterBigHoliday) 07:00 => 24:00
                if ((dateTypes.Contains(DateType.Weekend) || dateTypes.Contains(DateType.Holiday)) && dateTypes.Any(t => t == DateType.DayAfterBigHoliday) &&
                    (start.Date < endAt.Date || endAt.TimeOfDay > new TimeSpan(7, 0, 0)))
                {
                    yield return GetPriceInformation(
                        start.Date.AddHours(7).ToDateTimeOffsetSweden(),
                        start.Date == endAt.Date ? endAt : start.Date.AddDays(1).ToDateTimeOffsetSweden(),
                        PriceRowType.WeekendIWH,
                        prices
                    );
                }

                //Start counting from the first minute on next day
                start = start.AddDays(1).Date.ToDateTimeOffsetSweden();
            }

            //Get lost times, if any, they should not get payed for less than 30 min
            if (timeWasteNormalTime.HasValue && timeWasteNormalTime.Value >= 30)
            {
                yield return GetPriceInformation(startAt, startAt.AddMinutes(timeWasteNormalTime.Value).ToDateTimeOffsetSweden(), PriceRowType.LostTime, prices);
            }
            if (timeWasteIWHTime.HasValue && timeWasteIWHTime.Value > 0)
            {
                yield return GetPriceInformation(startAt, startAt.AddMinutes(timeWasteIWHTime.Value).ToDateTimeOffsetSweden(), PriceRowType.LostTimeIWH, prices);
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

        public DisplayPriceInformation GetPriceInformationToDisplay(List<PriceRowBase> priceRows, decimal? travelcost)
        {
            DisplayPriceInformation dpi = new DisplayPriceInformation();
            int numberOfBrokerFees = 0;
            string extraBrokerFee = string.Empty;
            string hourTaxDescription = string.Empty;
            foreach (PriceRowBase priceRow in priceRows.OrderByDescending(r => r.IsBrokerFee))
            {
                dpi.TaxTypeAndCompetenceLevelDescription = $"Använd tolktaxa {priceRow.PriceListRow.PriceListType.GetDescription()}, typ av tolk: {priceRow.PriceListRow.CompetenceLevel.GetDescription()}";
                if (priceRow.IsBrokerFee)
                {
                    numberOfBrokerFees += 1;
                    extraBrokerFee = numberOfBrokerFees > 1 ? $" dag {numberOfBrokerFees}" : string.Empty;
                }
                else
                {
                    hourTaxDescription = priceRow.PriceListRow.PriceRowType == PriceRowType.BasePrice ? $", taxa {GetDescriptionHourTax(priceRow.PriceListRow.MaxMinutes)} h" : string.Empty;
                }
                string startDescription = priceRow.IsBrokerFee ? $"Förmedlingsavgift{extraBrokerFee}" : priceRow.PriceListRow.PriceRowType.GetDescription() + hourTaxDescription;
                dpi.DisplayPriceRows.Add(new DisplayPriceRow { DescriptionWithCompetenceLevel = $"{startDescription} för tolktyp {priceRow.PriceListRow.CompetenceLevel.GetDescription()}", ShortDescription = startDescription, Price = priceRow.TotalPrice });
            }
            //do not check if zero since sometimes you want to display thet it was 0 in travelcost
            //might be better to have different descriptions of travel costs (estimated, actual etc)
            if (travelcost != null)
            {
                dpi.DisplayPriceRows.Add(new DisplayPriceRow { ShortDescription = "Total reskostnad", Price = travelcost.Value });
            }
            return dpi;
        }

        private string GetDescriptionHourTax(int maxMinutes)
        {
            double noOfHours = (double)maxMinutes / 60;

            switch (noOfHours)
            {
                case 1:
                    return $"0-{noOfHours}";
                default:
                    return $"{noOfHours - 0.5}-{noOfHours}";
            }
        }

        public IEnumerable<PriceInformationBrokerFee> BrokerFeePriceList
        {
            get
            {
                if (_cache == null)
                {
                    return GetBrokerFeePriceList();
                }
                if (!_cache.TryGetValue(brokerFeesCacheKey, out IEnumerable<PriceInformationBrokerFee> brokerFees))
                {
                    brokerFees = GetBrokerFeePriceList();
                    _cache.Set(brokerFeesCacheKey, brokerFees);
                }
                return brokerFees;
            }
        }

        private List<PriceInformationBrokerFee> GetBrokerFeePriceList()
        {
            List<PriceListRow> prices = _dbContext.PriceListRows.Where(p => p.MaxMinutes == 60 && p.PriceRowType == PriceRowType.BasePrice && p.PriceListType == PriceListType.Court).ToList();
            List<Ranking> ranks = _dbContext.Rankings.ToList();

            List<PriceInformationBrokerFee> priceListBrokerFee = new List<PriceInformationBrokerFee>();
            foreach (var item in prices)
            {
                priceListBrokerFee.AddRange(ranks.Select(r => new PriceInformationBrokerFee { BrokerFee = r.BrokerFee, FirstValidDateRanking = r.FirstValidDate, LastValidDateRanking = r.LastValidDate, RankingId = r.RankingId, CompetenceLevel = item.CompetenceLevel, EndDatePriceList = item.EndDate, BasePrice = item.Price, PriceListRowId = item.PriceListRowId, StartDatePriceList = item.StartDate, RoundDecimals = _options.RoundPriceDecimals }).ToList());
            }
            return priceListBrokerFee;
        }
    }
}
