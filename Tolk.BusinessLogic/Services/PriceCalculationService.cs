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

        public PriceCalculationService(TolkDbContext tolkDbContext = null, IMemoryCache cache = null)
        {
            _dbContext = tolkDbContext;
            _cache = cache;
        }

        public PriceInformation GetPrices(DateTimeOffset startAt, DateTimeOffset endAt, CompetenceLevel competenceLevel, PriceListType listType, int rankingId, decimal? travelCost = null)
        {
            var prices = GetPriceList(startAt, competenceLevel, listType);
            return CompletePricesWithExtraCharges(startAt, endAt, competenceLevel, GetPriceRowsPerType(startAt, endAt, prices).ToList(), rankingId, travelCost);
        }

        public PriceInformation GetPricesRequisition(DateTimeOffset startAt, DateTimeOffset endAt, CompetenceLevel competenceLevel, PriceListType listType, int rankingId, out bool useRequestPricerows, int? timeWasteNormalTime, int? timeWasteIWHTime, IEnumerable<PriceRowBase> requestPriceRows, decimal? travelCost)
        {
            var prices = GetPriceList(startAt, competenceLevel, listType);
            var priceListRowsPerPriceType = GetPriceRowsPerType(startAt, endAt, prices).ToList();

            //Check what price to use for requistion, broker should always get payed for original time of request/order if that exceeds time of requisition
            var priceRowsToCompareRequest = requestPriceRows.Where(plr =>
                 plr.PriceRowType == PriceRowType.InterpreterCompensation).ToList();

            useRequestPricerows = CheckRequisitionPriceToUse(priceListRowsPerPriceType, priceRowsToCompareRequest);
            if (useRequestPricerows)
            {
                priceListRowsPerPriceType = priceRowsToCompareRequest.Select(p => new PriceRowBase { StartAt = p.StartAt, EndAt = p.EndAt, PriceListRowId = p.PriceListRowId, Quantity = p.Quantity, Price = p.Price, PriceRowType = p.PriceRowType }).ToList();
            }
            //get lost time
            priceListRowsPerPriceType.AddRange(GetLostTimePriceRows(startAt, endAt, timeWasteNormalTime, timeWasteIWHTime, prices));
            return CompletePricesWithExtraCharges(startAt, endAt, competenceLevel, priceListRowsPerPriceType, rankingId, travelCost, requestPriceRows.Single(rpr => rpr.PriceRowType == PriceRowType.BrokerFee));
        }

        private PriceInformation CompletePricesWithExtraCharges(DateTimeOffset startAt, DateTimeOffset endAt, CompetenceLevel competenceLevel, List<PriceRowBase> priceListRowsPerPriceType, int rankingId, decimal? travelCost, PriceRowBase requestBrokerFeeForRequisition = null)
        {
            List<PriceRowBase> allPriceRows = new List<PriceRowBase>
            {
                GetPriceRowSocialInsuranceCharge(startAt, endAt, priceListRowsPerPriceType),
                GetPriceRowAdministrativeCharge(startAt, endAt, priceListRowsPerPriceType),
                GetPriceRowBrokerFee(startAt, endAt, competenceLevel, rankingId, requestBrokerFeeForRequisition)
            };
            allPriceRows.AddRange(priceListRowsPerPriceType);
            if (travelCost != null && travelCost > 0)
            {
                allPriceRows.Add(GetTravelCostRow(startAt.Date, startAt.Date.AddDays(1).ToDateTimeOffsetSweden(), travelCost));
            }
            allPriceRows.Add(GetRoundedPriceRow(startAt, endAt, allPriceRows));

            var priceInformation = new PriceInformation
            {
                PriceRows = allPriceRows
            };
            return priceInformation;
        }

        private PriceRowBase GetTravelCostRow(DateTimeOffset startAt, DateTimeOffset endAt, decimal? travelCost)
        {
            return new PriceRowBase { StartAt = startAt, EndAt = endAt, Price = travelCost.Value, Quantity = 1, PriceRowType = PriceRowType.TravelCost };
        }

        public PriceRowBase GetRoundedPriceRow(DateTimeOffset startAt, DateTimeOffset endAt, List<PriceRowBase> allPriceRows)
        {
            decimal roundings = 0;
            allPriceRows.Sum(pr => roundings += pr.Decimals);
            roundings = roundings - Math.Floor(roundings);
            roundings = roundings > Convert.ToDecimal(0.5) ? 1 - roundings : -roundings;
            //if roundings = 0 we create a row with 0 to display anyway
            return new PriceRowBase { StartAt = startAt, EndAt = endAt, Price = roundings, Quantity = 1, PriceRowType = PriceRowType.RoundedPrice };
        }

        public IEnumerable<PriceRowBase> GetLostTimePriceRows(DateTimeOffset startAt, DateTimeOffset endAt, int? timeWasteNormalTime, int? timeWasteIWHTime, List<PriceListRow> prices)
        {
            //Get lost times, if any, they should only get payed for timewaste more than 30 min
            if (timeWasteNormalTime.HasValue && timeWasteNormalTime.Value > 30)
            {
                yield return GetPriceInformation(startAt, startAt.AddMinutes(timeWasteNormalTime.Value).ToDateTimeOffsetSweden(), PriceListRowType.LostTime, prices);
            }
            if (timeWasteIWHTime.HasValue && timeWasteIWHTime.Value > 0)
            {
                yield return GetPriceInformation(startAt, startAt.AddMinutes(timeWasteIWHTime.Value).ToDateTimeOffsetSweden(), PriceListRowType.LostTimeIWH, prices);
            }
        }

        public List<PriceListRow> GetPriceList(DateTimeOffset startAt, CompetenceLevel competenceLevel, PriceListType listType)
        {
            return _dbContext.PriceListRows.Where(r =>
                r.CompetenceLevel == competenceLevel &&
                r.PriceListType == listType &&
                r.StartDate <= startAt.DateTime && r.EndDate >= startAt.DateTime).ToList();
        }

        private bool CheckRequisitionPriceToUse(List<PriceRowBase> priceToCompareRequsition, IEnumerable<PriceRowBase> priceToCompareRequest)
        {
            return priceToCompareRequest.Sum(p => p.TotalPrice) > priceToCompareRequsition.Sum(p => p.TotalPrice);
        }

        public PriceRowBase GetPriceRowSocialInsuranceCharge(DateTimeOffset startAt, DateTimeOffset endAt, List<PriceRowBase> priceListRowsPerPriceType)
        {
            return GetPriceCalculationCharge(startAt, endAt, priceListRowsPerPriceType, ChargeType.SocialInsuranceCharge);
        }

        public PriceRowBase GetPriceRowAdministrativeCharge(DateTimeOffset startAt, DateTimeOffset endAt, List<PriceRowBase> priceListRowsPerPriceType)
        {
            return GetPriceCalculationCharge(startAt, endAt, priceListRowsPerPriceType, ChargeType.AdministrativeCharge);
        }

        private PriceRowBase GetPriceCalculationCharge(DateTimeOffset startAt, DateTimeOffset endAt, List<PriceRowBase> priceListRowsPerPriceType, ChargeType chargeType)
        {
            var chargeRow = _dbContext.PriceCalculationCharges.Single(c => c.ChargeTypeId == chargeType && c.StartDate <= startAt.DateTime && c.EndDate >= startAt.DateTime);
            return new PriceRowBase { StartAt = startAt, EndAt = endAt, Price = chargeRow.ChargePercentage * priceListRowsPerPriceType.Sum(m => m.TotalPrice) / 100, Quantity = 1, PriceRowType = chargeType == ChargeType.SocialInsuranceCharge ? PriceRowType.SocialInsuranceCharge : PriceRowType.AdministrativeCharge, PriceCalculationChargeId = chargeRow.PriceCalculationChargeId };
        }

        private PriceRowBase GetPriceRowBrokerFee(DateTimeOffset startAt, DateTimeOffset endAt, CompetenceLevel competenceLevel, int rankingId, PriceRowBase brokerFeeToUse)
        {
            if (brokerFeeToUse != null)
            {
                return brokerFeeToUse;
            }
            else
            {
                //One broker fee per day
                int days = GetNoOfDays(startAt, endAt);
                
                var priceRow = BrokerFeePriceList.Single(br => br.RankingId == rankingId && br.CompetenceLevel == competenceLevel && br.StartDate <= startAt && br.EndDate >= startAt);

                return new PriceRowBase
                {
                    StartAt = startAt.Date.ToDateTimeOffsetSweden(),
                    EndAt = endAt.Date.ToDateTimeOffsetSweden(),
                    PriceRowType = PriceRowType.BrokerFee,
                    Quantity = days,
                    Price = priceRow.PriceToUse,
                    PriceListRowId = priceRow.PriceListRowId
                };
            }
        }

        public int GetNoOfDays(DateTimeOffset startAt, DateTimeOffset endAt)
        {
            int days = (endAt.Date - startAt.Date).Days + 1;
            days -= endAt.TimeOfDay == TimeSpan.Zero ? 1 : 0; //if ends at midnight no extra day
            return days;
        }

        private static PriceRowBase GetPriceInformation(DateTimeOffset startAt, DateTimeOffset endAt, PriceListRowType rowType, List<PriceListRow> prices)
        {
            PriceListRow priceInfo = prices.Single(r => r.PriceListRowType == rowType);
            var priceTime = new PriceRowBase
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
            priceTime.PriceListRow = priceInfo;
            priceTime.Price = priceInfo.Price;
            return priceTime;
        }

        private IEnumerable<PriceRowBase> GetPriceRowsPerType(DateTimeOffset startAt, DateTimeOffset endAt, List<PriceListRow> prices)
        {
            int maxMinutes = 330;
            TimeSpan span = endAt - startAt;
            int totalMinutes = (int)span.TotalMinutes;
            if (totalMinutes > maxMinutes)
            {
                DateTimeOffset extraTimeStartsAt = startAt.AddMinutes(maxMinutes);
                var basePrice = prices.Single(r => r.PriceListRowType == PriceListRowType.BasePrice && r.MaxMinutes >= maxMinutes);
                yield return new PriceRowBase
                {
                    StartAt = startAt,
                    EndAt = extraTimeStartsAt,
                    PriceRowType = PriceRowType.InterpreterCompensation,
                    Quantity = 1,
                    PriceListRow = basePrice,
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
                yield return new PriceRowBase
                {
                    StartAt = startAt,
                    EndAt = endAt,
                    PriceRowType = PriceRowType.InterpreterCompensation,
                    Quantity = 1,
                    PriceListRow = basePrice,
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

        public IEnumerable<DateType> GetDateTypes(DateTime date)
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
            DisplayPriceInformation dpiTotal = new DisplayPriceInformation();
            DisplayPriceInformation separateSubTotalInterpreterCompensation = new DisplayPriceInformation
            {
                HeaderDescription = PriceRowType.InterpreterCompensation.GetDescription()
            };

            decimal interpreterCompensation = priceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice);
            dpiTotal.DisplayPriceRows.Add(new DisplayPriceRow { Description = PriceRowType.InterpreterCompensation.GetDescription(), Price = interpreterCompensation, HasSeparateSubTotal = true, DisplayOrder = GetDisplayOrder(PriceRowType.InterpreterCompensation) });
            foreach (PriceRowBase priceRow in priceRows.OrderBy(r => r.PriceRowType))
            {
                if (priceRow.PriceRowType == PriceRowType.InterpreterCompensation && priceRow.PriceListRow != null && priceRow.PriceListRow.PriceListRowType == PriceListRowType.BasePrice)
                {
                    dpiTotal.HeaderDescription = $"Använd tolktaxa {priceRow.PriceListRow.PriceListType.GetDescription()}, typ av tolk: {priceRow.PriceListRow.CompetenceLevel.GetDescription()}";
                }
                //for interpreter compensation we get each row in separate subtotal
                if (priceRow.PriceRowType == PriceRowType.InterpreterCompensation)
                {
                    string hourTaxDescription = priceRow.PriceListRow.PriceListRowType == PriceListRowType.BasePrice ? $", taxa {GetDescriptionHourTax(priceRow.PriceListRow.MaxMinutes)} h" : string.Empty;
                    string description = priceRow.PriceListRow.PriceListRowType.GetDescription() + hourTaxDescription + GetQuantityAndPricePerUnit(priceRow);
                    separateSubTotalInterpreterCompensation.DisplayPriceRows.Add(new DisplayPriceRow { Description = description, Price = priceRow.Price * priceRow.Quantity, DisplayOrder = (int)priceRow.PriceListRow.PriceListRowType });
                }
                else
                {
                    dpiTotal.DisplayPriceRows.Add(new DisplayPriceRow { Description = priceRow.PriceRowType.GetDescription() + GetQuantityAndPricePerUnit(priceRow), Price = priceRow.Price * priceRow.Quantity, DisplayOrder = GetDisplayOrder(priceRow.PriceRowType) });
                }
            }
            dpiTotal.SeparateSubTotal.Add(separateSubTotalInterpreterCompensation);
            return dpiTotal;
        }

        private int GetDisplayOrder(PriceRowType type)
        {
            switch (type)
            {
                case PriceRowType.InterpreterCompensation:
                    return 1;
                case PriceRowType.SocialInsuranceCharge:
                    return 2;
                case PriceRowType.AdministrativeCharge:
                    return 3;
                case PriceRowType.BrokerFee:
                    return 4;
                case PriceRowType.TravelCost:
                    return 5;
                case PriceRowType.RoundedPrice:
                    return 100;
                default:
                    return 30;
            }
        }

        private string GetQuantityAndPricePerUnit(PriceRowBase priceRow)
        {
            return priceRow.Quantity > 1 ? $" ({priceRow.Quantity} st á {priceRow.Price})" : string.Empty;
        }

        private string GetDescriptionHourTax(int maxMinutes)
        {
            double noOfHours = (double)maxMinutes / 60;
            return noOfHours == 1 ? $"0-{noOfHours}" : $"{noOfHours - 0.5}-{noOfHours}";
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
                priceListBrokerFee.AddRange(ranks.Select(r => new PriceInformationBrokerFee { BrokerFee = r.BrokerFee, FirstValidDateRanking = r.FirstValidDate, LastValidDateRanking = r.LastValidDate, RankingId = r.RankingId, CompetenceLevel = item.CompetenceLevel, EndDatePriceList = item.EndDate, BasePrice = item.Price, PriceListRowId = item.PriceListRowId.Value, StartDatePriceList = item.StartDate, RoundDecimals = _options == null ? true : _options.RoundPriceDecimals }).ToList());
            }
            return priceListBrokerFee;
        }
    }
}
