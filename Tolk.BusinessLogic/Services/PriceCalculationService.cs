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

        public PriceInformation GetPrices(DateTimeOffset startAt, DateTimeOffset endAt, CompetenceLevel competenceLevel, PriceListType listType, int rankingId)
        {
            var prices = GetPriceList(startAt, endAt, competenceLevel, listType);
            return CompletePricesWithExtraCharges(startAt, endAt, competenceLevel, GetPriceRowsPerType(startAt, endAt, prices).ToList(), rankingId);
        }

        public PriceInformation GetPricesRequisition(DateTimeOffset startAt, DateTimeOffset endAt, CompetenceLevel competenceLevel, PriceListType listType, int rankingId, out bool useRequestPricerows, int? timeWasteNormalTime, int? timeWasteIWHTime, IEnumerable<PriceRowBase> requestPriceRows)
        {
            var prices = GetPriceList(startAt, endAt, competenceLevel, listType);
            var priceListRowsPerPriceType = GetPriceRowsPerType(startAt, endAt, prices).ToList();

            //Check what price to use for requistion, broker should always get payed for original time of request/order if that exceeds time of requisition
            var priceRowsToCompareRequest = requestPriceRows.Where(plr =>
                 plr.PriceRowType == PriceRowType.InterpreterCompensation &&
                 (plr.PriceListRow.PriceListRowType == PriceListRowType.BasePrice ||
                 plr.PriceListRow.PriceListRowType == PriceListRowType.PriceOverMaxTime ||
                 plr.PriceListRow.PriceListRowType == PriceListRowType.InconvenientWorkingHours ||
                 plr.PriceListRow.PriceListRowType == PriceListRowType.WeekendIWH ||
                 plr.PriceListRow.PriceListRowType == PriceListRowType.BigHolidayWeekendIWH)).ToList();

            useRequestPricerows = CheckRequisitionPriceToUse(priceListRowsPerPriceType, priceRowsToCompareRequest);
            if (useRequestPricerows)
            {
                priceListRowsPerPriceType = priceRowsToCompareRequest.Select(p => new PriceRow { StartAt = p.StartAt, EndAt = p.EndAt, PriceListRowId = p.PriceListRowId, Quantity = p.Quantity, Price = p.Price, PriceRowType = p.PriceRowType }).ToList();
            }
            //get lost time
            priceListRowsPerPriceType.AddRange(GetLostTimePriceRows(startAt, endAt, timeWasteNormalTime, timeWasteIWHTime, prices));

            return CompletePricesWithExtraCharges(startAt, endAt, competenceLevel, priceListRowsPerPriceType, rankingId, requestPriceRows.Where(rpr => rpr.PriceRowType == PriceRowType.BrokerFee));
        }

        private PriceInformation CompletePricesWithExtraCharges(DateTimeOffset startAt, DateTimeOffset endAt, CompetenceLevel competenceLevel, List<PriceRow> priceListRowsPerPriceType, int rankingId, IEnumerable<PriceRowBase> requestBrokerFeesForRequisition = null)
        {
            List<PriceRow> allPriceRows = new List<PriceRow>
            {
                GetPriceRowSocialInsuranceCharge(startAt, endAt, priceListRowsPerPriceType),
                GetPriceRowAdministrativeCharge(startAt, endAt, priceListRowsPerPriceType)
            };
            allPriceRows.AddRange(GetPriceRowsBrokerFee(startAt, endAt, competenceLevel, rankingId, requestBrokerFeesForRequisition));
            allPriceRows.AddRange(priceListRowsPerPriceType);
            allPriceRows.Add(GetRoundedPriceRow(startAt, endAt, allPriceRows));

            var priceInformation = new PriceInformation
            {
                PriceRows = allPriceRows
            };
            return priceInformation;
        }

        private PriceRow GetRoundedPriceRow(DateTimeOffset startAt, DateTimeOffset endAt, List<PriceRow> allPriceRows)
        {
            decimal roundings = 0;
            allPriceRows.Sum(pr => roundings += pr.Quantity * (pr.Price - Math.Floor(pr.Price)));
            roundings = roundings - Math.Floor(roundings);
            roundings = roundings > Convert.ToDecimal(0.5) ? 1 - roundings : -roundings;
            return new PriceRow { StartAt = startAt, EndAt = endAt, Price = roundings, Quantity = 1, PriceRowType = PriceRowType.RoundedPrice };
        }

        private IEnumerable<PriceRow> GetLostTimePriceRows(DateTimeOffset startAt, DateTimeOffset endAt, int? timeWasteNormalTime, int? timeWasteIWHTime, List<PriceListRow> prices)
        {
            //Get lost times, if any, they should not get payed for less than 30 min
            if (timeWasteNormalTime.HasValue && timeWasteNormalTime.Value >= 30)
            {
                yield return GetPriceInformation(startAt, startAt.AddMinutes(timeWasteNormalTime.Value).ToDateTimeOffsetSweden(), PriceListRowType.LostTime, prices);
            }
            if (timeWasteIWHTime.HasValue && timeWasteIWHTime.Value > 0)
            {
                yield return GetPriceInformation(startAt, startAt.AddMinutes(timeWasteIWHTime.Value).ToDateTimeOffsetSweden(), PriceListRowType.LostTimeIWH, prices);
            }
        }

        private List<PriceListRow> GetPriceList(DateTimeOffset startAt, DateTimeOffset endAt, CompetenceLevel competenceLevel, PriceListType listType)
        {
            return _dbContext.PriceListRows.Where(r =>
                r.CompetenceLevel == competenceLevel &&
                r.PriceListType == listType &&
                r.StartDate <= startAt.DateTime && r.EndDate >= endAt.DateTime).ToList();
        }

        private bool CheckRequisitionPriceToUse(List<PriceRow> priceToCompareRequsition, IEnumerable<PriceRowBase> priceToCompareRequest)
        {
            return priceToCompareRequest.Sum(p => p.Price * p.Quantity) > priceToCompareRequsition.Sum(p => p.TotalPrice);
        }

        private PriceRow GetPriceRowSocialInsuranceCharge(DateTimeOffset startAt, DateTimeOffset endAt, List<PriceRow> priceListRowsPerPriceType)
        {
            return GetPriceCalculationCharge(startAt, endAt, priceListRowsPerPriceType, ChargeType.SocialInsuranceCharge);
        }

        private PriceRow GetPriceRowAdministrativeCharge(DateTimeOffset startAt, DateTimeOffset endAt, List<PriceRow> priceListRowsPerPriceType)
        {
            return GetPriceCalculationCharge(startAt, endAt, priceListRowsPerPriceType, ChargeType.AdministrativeCharge);
        }

        private PriceRow GetPriceCalculationCharge(DateTimeOffset startAt, DateTimeOffset endAt, List<PriceRow> priceListRowsPerPriceType, ChargeType chargeType)
        {
            decimal charge = _dbContext.PriceCalculationCharges.Single(c => c.ChargeTypeId == chargeType && startAt.Date > c.StartDate && endAt.Date < c.EndDate).ChargePercentage / 100;
            return new PriceRow { StartAt = startAt, EndAt = endAt, Price = charge * priceListRowsPerPriceType.Sum(m => m.TotalPrice), Quantity = 1, PriceRowType = chargeType == ChargeType.SocialInsuranceCharge ? PriceRowType.SocialInsuranceCharge : PriceRowType.AdministrativeCharge };
        }

        private IEnumerable<PriceRow> GetPriceRowsBrokerFee(DateTimeOffset startAt, DateTimeOffset endAt, CompetenceLevel competenceLevel, int rankingId, IEnumerable<PriceRowBase> brokerFeeToUse)
        {
            if (brokerFeeToUse != null)
            {
                foreach (var bf in brokerFeeToUse)
                {
                    yield return new PriceRow { StartAt = bf.StartAt, EndAt = bf.EndAt, PriceRowType = PriceRowType.BrokerFee, Quantity = 1, Price = bf.Price, PriceListRowId = bf.PriceListRowId.Value, };
                }
            }
            else
            {
                int days = GetNoOfDays(startAt, endAt);
                //One broker fee per calender day.
                var priceRow = BrokerFeePriceList.Single(br => br.RankingId == rankingId && br.CompetenceLevel == competenceLevel && br.StartDate < startAt && br.EndDate > startAt);
                for (int i = 1; i <= days; i++)
                {
                    yield return new PriceRow
                    {
                        StartAt = startAt.Date.AddDays(i - 1).ToDateTimeOffsetSweden(),
                        EndAt = startAt.Date.AddDays(i).ToDateTimeOffsetSweden(),
                        PriceRowType = PriceRowType.BrokerFee,
                        Quantity = 1,
                        Price = priceRow.PriceToUse,
                        PriceListRowId = priceRow.PriceListRowId
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

        private static PriceRow GetPriceInformation(DateTimeOffset startAt, DateTimeOffset endAt, PriceListRowType rowType, List<PriceListRow> prices)
        {
            PriceListRow priceInfo = prices.Single(r => r.PriceListRowType == rowType);
            var priceTime = new PriceRow
            {
                StartAt = startAt,
                EndAt = endAt,
                PriceRowType = PriceRowType.InterpreterCompensation
            };
            priceTime.Quantity = priceTime.Minutes / priceInfo.MaxMinutes;
            if (priceTime.Minutes % priceInfo.MaxMinutes > 0)
            {
                priceTime.Quantity++;
            }
            priceTime.PriceListRowId = priceInfo.PriceListRowId.Value;
            priceTime.Price = priceInfo.Price;
            return priceTime;
        }

        private IEnumerable<PriceRow> GetPriceRowsPerType(DateTimeOffset startAt, DateTimeOffset endAt, List<PriceListRow> prices)
        {
            int maxMinutes = 330;
            TimeSpan span = endAt - startAt;
            int totalMinutes = (int)span.TotalMinutes;
            if (totalMinutes > maxMinutes)
            {
                DateTimeOffset extraTimeStartsAt = startAt.AddMinutes(maxMinutes);
                var basePrice = prices.Single(r => r.PriceListRowType == PriceListRowType.BasePrice && r.MaxMinutes >= maxMinutes);
                yield return new PriceRow
                {
                    StartAt = startAt,
                    EndAt = extraTimeStartsAt,
                    PriceRowType = PriceRowType.InterpreterCompensation,
                    Quantity = 1,
                    PriceListRowId = basePrice.PriceListRowId.Value,
                    Price = basePrice.Price
                };
                //Calculate when the extra time starts, date wize.
                yield return GetPriceInformation(extraTimeStartsAt, endAt, PriceListRowType.PriceOverMaxTime, prices);
            }
            else
            {
                var basePriceTest = prices.OrderBy(p => p.MaxMinutes);
                var basePrice = prices.OrderBy(p => p.MaxMinutes)
                    .First(r => r.PriceListRowType == PriceListRowType.BasePrice && r.MaxMinutes >= totalMinutes);
                yield return new PriceRow
                {
                    StartAt = startAt,
                    EndAt = endAt,
                    PriceRowType = PriceRowType.InterpreterCompensation,
                    Quantity = 1,
                    PriceListRowId = basePrice.PriceListRowId.Value,
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
                        dateTypes.Contains(DateType.BigHolidayFullDay) ? PriceListRowType.BigHolidayWeekendIWH : PriceListRowType.WeekendIWH,
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
                        dateTypes.Contains(DateType.DayAfterBigHoliday) ? PriceListRowType.BigHolidayWeekendIWH : PriceListRowType.InconvenientWorkingHours,
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
                        dateTypes.Contains(DateType.DayBeforeBigHoliday) ? PriceListRowType.BigHolidayWeekendIWH : PriceListRowType.InconvenientWorkingHours,
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
                        PriceListRowType.WeekendIWH,
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
                        PriceListRowType.WeekendIWH,
                        prices
                    );
                }
                //Start counting from the first minute on next day
                start = start.AddDays(1).Date.ToDateTimeOffsetSweden();
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
            foreach (PriceRowBase priceRow in priceRows.OrderBy(r => r.PriceRowType))
            {
                if (priceRow.PriceListRow != null && priceRow.PriceListRow.PriceListRowType == PriceListRowType.BasePrice)
                {
                    dpi.TaxTypeAndCompetenceLevelDescription = $"Använd tolktaxa {priceRow.PriceListRow.PriceListType.GetDescription()}, typ av tolk: {priceRow.PriceListRow.CompetenceLevel.GetDescription()}";
                }
                if (priceRow.PriceRowType == PriceRowType.BrokerFee)
                {
                    numberOfBrokerFees += 1;
                    extraBrokerFee = numberOfBrokerFees > 1 ? $" dag {numberOfBrokerFees}" : string.Empty;
                }
                else if (priceRow.PriceListRow != null)
                {
                    hourTaxDescription = priceRow.PriceListRow.PriceListRowType == PriceListRowType.BasePrice ? $", taxa {GetDescriptionHourTax(priceRow.PriceListRow.MaxMinutes)} h" : string.Empty;
                }
                string description = (priceRow.PriceListRow != null && priceRow.PriceRowType != PriceRowType.BrokerFee) ? priceRow.PriceListRow.PriceListRowType.GetDescription() + hourTaxDescription : priceRow.PriceRowType == PriceRowType.BrokerFee ? priceRow.PriceRowType.GetDescription() + extraBrokerFee : priceRow.PriceRowType.GetDescription();
                dpi.DisplayPriceRows.Add(new DisplayPriceRow { Description = description, Price = priceRow.Price * priceRow.Quantity });
            }
            //do not check if zero since sometimes you want to display thet it was 0 in travelcost
            //might be better to have different descriptions of travel costs (estimated, actual etc)
            if (travelcost != null)
            {
                dpi.DisplayPriceRows.Add(new DisplayPriceRow { Description = "Total reskostnad", Price = travelcost.Value });
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

        private IEnumerable<PriceInformationBrokerFee> BrokerFeePriceList
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
            List<PriceListRow> prices = _dbContext.PriceListRows.Where(p => p.MaxMinutes == 60 && p.PriceListRowType == PriceListRowType.BasePrice && p.PriceListType == PriceListType.Court).ToList();
            List<Ranking> ranks = _dbContext.Rankings.ToList();

            List<PriceInformationBrokerFee> priceListBrokerFee = new List<PriceInformationBrokerFee>();
            foreach (var item in prices)
            {
                priceListBrokerFee.AddRange(ranks.Select(r => new PriceInformationBrokerFee { BrokerFee = r.BrokerFee, FirstValidDateRanking = r.FirstValidDate, LastValidDateRanking = r.LastValidDate, RankingId = r.RankingId, CompetenceLevel = item.CompetenceLevel, EndDatePriceList = item.EndDate, BasePrice = item.Price, PriceListRowId = item.PriceListRowId.Value, StartDatePriceList = item.StartDate, RoundDecimals = _options.RoundPriceDecimals }).ToList());
            }
            return priceListBrokerFee;
        }
    }
}
