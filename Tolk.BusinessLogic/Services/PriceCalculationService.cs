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
        private readonly DateCalculationService _dateCalculationService;

        private const string brokerFeesCacheKey = nameof(brokerFeesCacheKey);

        public PriceCalculationService(TolkDbContext dbContext,
            ILogger<PriceCalculationService> logger,
            DateCalculationService dateCalculationService,
            IOptions<TolkOptions> options,
            IMemoryCache cache = null
            )
        {
            _logger = logger;
            _dbContext = dbContext;
            _dateCalculationService = dateCalculationService;
            _options = options.Value;
            _cache = cache;
        }

        public PriceCalculationService(TolkDbContext tolkDbContext = null, IMemoryCache cache = null)
        {
            _dbContext = tolkDbContext;
            _cache = cache;
        }

        public PriceInformation GetPrices(Request request, CompetenceAndSpecialistLevel competenceLevel, decimal? expectedTravelCost)
        {
            return GetPrices(
                request.Order.StartAt,
                request.Order.EndAt,
                EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>(competenceLevel),
                request.Order.CustomerOrganisation.PriceListType,
                request.RankingId,
                expectedTravelCost);
        }

        public PriceInformation GetPrices(DateTimeOffset startAt, DateTimeOffset endAt, CompetenceLevel competenceLevel, PriceListType listType, int rankingId, decimal? travelCost = null)
        {
            var prices = GetPriceList(startAt, competenceLevel, listType);
            return CompletePricesWithExtraCharges(startAt, endAt, competenceLevel, MergePriceListRowsAndReduceForMealBreak(GetPriceRowsPerType(startAt, endAt, prices)).ToList(), rankingId, travelCost);
        }

        public PriceInformation GetPricesRequisition(DateTimeOffset startAt, DateTimeOffset endAt, DateTimeOffset originStartAt, DateTimeOffset originEndAt, CompetenceLevel competenceLevel, PriceListType listType, int rankingId, out bool useRequestPricerows, int? timeWasteNormalTime, int? timeWasteIWHTime, IEnumerable<PriceRowBase> requestPriceRows, decimal? outlay, decimal? perdiem, decimal? carCompensation, Order replacingOrder, List<MealBreak> mealbreaks = null)
        {
            var prices = GetPriceList(startAt, competenceLevel, listType);

            //get mealbeaks if any
            IDictionary<PriceListRowType, int> mealbreakTimes = GetMealBreakTimesAndTypes(mealbreaks);

            //compare compensation for origin times with requisition times (including mealbreaks), broker/interpreter should have the compensation that get most payed
            var pricesRequisition = MergePriceListRowsAndReduceForMealBreak(GetPriceRowsPerType(startAt, endAt, prices, mealbreaks), mealbreakTimes);
            var pricesOriginTimes = MergePriceListRowsAndReduceForMealBreak(GetPriceRowsPerType(originStartAt, originEndAt, prices, mealbreaks), mealbreakTimes);

            useRequestPricerows = UsePricesOriginTimes(pricesRequisition, pricesOriginTimes);

            //if we should use request, set start and end from request start and end
            var pricesToUse = useRequestPricerows ? pricesOriginTimes.ToList() : pricesRequisition.ToList();

            //if replacementorder - check and compare start- and endtime from replacing order
            if (replacingOrder != null)
            {
                var pricesReplacingOrder = MergePriceListRowsAndReduceForMealBreak(GetPriceRowsPerType(replacingOrder.StartAt, replacingOrder.EndAt, prices, mealbreaks), mealbreakTimes);

                bool useReplacingOrderTimes = UsePricesOriginTimes(useRequestPricerows ? pricesOriginTimes : pricesRequisition, pricesReplacingOrder);
                if (useReplacingOrderTimes)
                {
                    pricesToUse = pricesReplacingOrder.ToList();
                    useRequestPricerows = useReplacingOrderTimes;
                }
            }
            //get lost time and extra charges
            pricesToUse.AddRange(GetLostTimePriceRows(startAt, endAt, timeWasteNormalTime, timeWasteIWHTime, prices));
            return CompletePricesWithExtraCharges(startAt, endAt, competenceLevel, pricesToUse, rankingId, null, outlay, perdiem, carCompensation, requestPriceRows.Single(rpr => rpr.PriceRowType == PriceRowType.BrokerFee));
        }

        /// <summary>
        /// If order passes midnight their might be two rows of same pricetype, merge these to one 
        /// Also check if two duplicate rows have less or equal compensationtime together. Then it should be reduced, e.g. 23:45-00:45 (should be two 30 min periods not three)
        /// </summary>
        public IEnumerable<PriceRowBase> MergePriceListRowsAndReduceForMealBreak(IEnumerable<PriceRowBase> pricePerType, IDictionary<PriceListRowType, int> mealbreakDictionary = null)
        {
            int newQuantity = 0;
            var duplicatesPriceListrows = pricePerType.GroupBy(x => x.PriceListRow.PriceListRowId)
                             .Where(g => g.Count() > 1)
                             .Select(g => g.Key)
                             .Distinct().ToList();

            if (!duplicatesPriceListrows.Any())
            {
                if (mealbreakDictionary != null)
                {
                    List<PriceRowBase> pl = pricePerType.Where(pr => pr.PriceListRow.PriceListRowType != PriceListRowType.BigHolidayWeekendIWH && pr.PriceListRow.PriceListRowType != PriceListRowType.WeekendIWH && pr.PriceListRow.PriceListRowType != PriceListRowType.InconvenientWorkingHours).ToList();
                    foreach (PriceRowBase pricerow in pricePerType.Where(pr => pr.PriceListRow.PriceListRowType == PriceListRowType.BigHolidayWeekendIWH || pr.PriceListRow.PriceListRowType == PriceListRowType.WeekendIWH || pr.PriceListRow.PriceListRowType == PriceListRowType.InconvenientWorkingHours))
                    {
                        //reduce quantity with mealbreaks, check if quantity > 0 before adding
                        newQuantity = GetReducedQuantityForMealbreak(pricerow, mealbreakDictionary[pricerow.PriceListRow.PriceListRowType]);
                        if (newQuantity > 0)
                        {
                            pricerow.Quantity = newQuantity;
                            pl.Add(pricerow);
                        }
                    }
                    return pl;
                }
                else { return pricePerType; }
            }
            else
            {
                List<PriceRowBase> mergedList = new List<PriceRowBase>();
                foreach (int priceListRowId in duplicatesPriceListrows)
                {
                    PriceRowBase newPriceRow = pricePerType.OrderBy(pr => pr.StartAt).First(pr => pr.PriceListRow.PriceListRowId == priceListRowId);
                    var noOfMinutes = pricePerType.Where(pr => pr.PriceListRow.PriceListRowId == priceListRowId).Sum(pr => pr.Minutes);

                    //reduce noOfMinutes with mealbreak minutes
                    if (mealbreakDictionary != null && mealbreakDictionary.ContainsKey(newPriceRow.PriceListRow.PriceListRowType))
                    {
                        noOfMinutes -= mealbreakDictionary[newPriceRow.PriceListRow.PriceListRowType];
                    }
                    var compensationPeriod = newPriceRow.PriceListRow.MaxMinutes;
                    newQuantity = compensationPeriod == 0 ? newPriceRow.Quantity : noOfMinutes % compensationPeriod > 0 ? (noOfMinutes / compensationPeriod) + 1 : noOfMinutes / compensationPeriod;
                    newPriceRow.Quantity = newQuantity;
                    newPriceRow.EndAt = pricePerType.OrderBy(pr => pr.EndAt).Last(pr => pr.PriceListRow.PriceListRowId == priceListRowId).EndAt;
                    if (newQuantity > 0)
                    {
                        newPriceRow.Quantity = newQuantity;
                        mergedList.Add(newPriceRow);
                    }
                }
                mergedList.AddRange(pricePerType.Where(pri => !duplicatesPriceListrows.Contains(pri.PriceListRow.PriceListRowId)));
                return mergedList;
            }
        }

        private int GetReducedQuantityForMealbreak(PriceRowBase newPriceRow, int mealbreakMinutes)
        {
            int reducedMinutes = newPriceRow.Minutes - mealbreakMinutes;
            return reducedMinutes > 0 ? reducedMinutes % newPriceRow.PriceListRow.MaxMinutes > 0 ? (reducedMinutes / newPriceRow.PriceListRow.MaxMinutes) + 1 : reducedMinutes / newPriceRow.PriceListRow.MaxMinutes : 0;
        }

        private PriceInformation CompletePricesWithExtraCharges(DateTimeOffset startAt, DateTimeOffset endAt, CompetenceLevel competenceLevel, List<PriceRowBase> priceListRowsPerPriceType, int rankingId, decimal? travelCost, decimal? outlay = null, decimal? perDiem = null, decimal? carCompensation = null, PriceRowBase requestBrokerFeeForRequisition = null)
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
                allPriceRows.Add(GetTravelCostRow(startAt.Date, startAt.Date.AddDays(1).ToDateTimeOffsetSweden(), travelCost, PriceRowType.TravelCost));
            }
            if (outlay != null && outlay > 0)
            {
                allPriceRows.Add(GetTravelCostRow(startAt.Date, startAt.Date.AddDays(1).ToDateTimeOffsetSweden(), outlay, PriceRowType.Outlay));
            }
            if (perDiem != null && perDiem > 0)
            {
                allPriceRows.Add(GetTravelCostRow(startAt.Date, startAt.Date.AddDays(1).ToDateTimeOffsetSweden(), perDiem, PriceRowType.PerDiem));
            }
            if (carCompensation != null && carCompensation > 0)
            {
                allPriceRows.Add(GetTravelCostRow(startAt.Date, startAt.Date.AddDays(1).ToDateTimeOffsetSweden(), carCompensation, PriceRowType.CarCompensation));
            }
            allPriceRows.Add(GetRoundedPriceRow(startAt, endAt, allPriceRows));

            var priceInformation = new PriceInformation
            {
                PriceRows = allPriceRows
            };
            return priceInformation;
        }

        private PriceRowBase GetTravelCostRow(DateTimeOffset startAt, DateTimeOffset endAt, decimal? travelCost, PriceRowType travelcostType)
        {
            return new PriceRowBase { StartAt = startAt, EndAt = endAt, Price = travelCost.Value, Quantity = 1, PriceRowType = travelcostType };
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
            //Get lost times, if any
            if (timeWasteNormalTime.HasValue && timeWasteNormalTime.Value > 0)
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
                r.StartDate.Date <= startAt.Date && r.EndDate.Date >= startAt.Date).ToList();
        }

        private bool UsePricesOriginTimes(IEnumerable<PriceRowBase> priceRequsition, IEnumerable<PriceRowBase> priceOriginTimes)
        {
            return priceOriginTimes.Sum(p => p.TotalPrice) > priceRequsition.Sum(p => p.TotalPrice);
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

                var priceRow = BrokerFeePriceList.Single(br => br.RankingId == rankingId && br.CompetenceLevel == competenceLevel && br.StartDate.Date <= startAt.Date && br.EndDate.Date >= startAt.Date);

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

        private IDictionary<PriceListRowType, int> GetMealBreakTimesAndTypes(List<MealBreak> mealbreaks)
        {
            if (mealbreaks == null || !mealbreaks.Any()) { return null; }

            int minIWH = 0; // normal iwh time
            int minWeekendsIWH = 0; // weekends, holiday
            int minIWHBigHoliday = 0; // big holiday

            DateTimeOffset tempstart;
            DateTimeOffset tempstop;

            foreach (MealBreak mb in mealbreaks)
            {
                var start = mb.StartAt;
                var endAt = mb.EndAt;

                while (start < endAt)
                {
                    var dateTypes = GetDateTypes(start.Date);
                    if (dateTypes.Contains(DateType.BigHolidayFullDay) ||
                        dateTypes.Contains(DateType.Holiday) ||
                        (dateTypes.Contains(DateType.Weekend) && !dateTypes.Any(t => t == DateType.DayBeforeBigHoliday || t == DateType.DayAfterBigHoliday)))
                    {
                        tempstart = start;
                        tempstop = start.Date == endAt.Date ? endAt : start.Date.AddDays(1).ToDateTimeOffsetSweden();
                        if (dateTypes.Contains(DateType.BigHolidayFullDay))
                        {
                            minIWHBigHoliday += (int)(tempstop - tempstart).TotalMinutes;
                        }
                        else
                        {
                            minWeekendsIWH += (int)(tempstop - tempstart).TotalMinutes;
                        }
                    }

                    //Find any minutes before 07:00
                    if (!dateTypes.Contains(DateType.Holiday) && !dateTypes.Contains(DateType.BigHolidayFullDay) &&
                        dateTypes.Any(t => t == DateType.WeekDay || t == DateType.DayAfterBigHoliday) &&
                        start.TimeOfDay < new TimeSpan(7, 0, 0))
                    {
                        tempstart = start;
                        tempstop = start.Date < endAt.Date || endAt.TimeOfDay > new TimeSpan(7, 0, 0) ? start.Date.AddHours(7).ToDateTimeOffsetSweden() : endAt;
                        if (dateTypes.Contains(DateType.DayAfterBigHoliday))
                        {
                            minIWHBigHoliday += (int)(tempstop - tempstart).TotalMinutes;
                        }
                        else
                        {
                            minIWH += (int)(tempstop - tempstart).TotalMinutes;
                        }
                    }
                    //takes wrong when day after big holiday and after 18:00
                    if (!dateTypes.Contains(DateType.Holiday) && !dateTypes.Contains(DateType.BigHolidayFullDay) &&
                        dateTypes.Any(t => t == DateType.WeekDay || t == DateType.DayBeforeBigHoliday) &&
                        (start.Date < endAt.Date || endAt.TimeOfDay > new TimeSpan(18, 0, 0)))
                    {
                        tempstart = start.Hour < 18 ? start.Date.AddHours(18).ToDateTimeOffsetSweden() : start;
                        tempstop = start.Date == endAt.Date ? endAt : start.Date.AddDays(1).ToDateTimeOffsetSweden();
                        if (dateTypes.Contains(DateType.DayBeforeBigHoliday))
                        {
                            minIWHBigHoliday += (int)(tempstop - tempstart).TotalMinutes;
                        }
                        else
                        {
                            minIWH += (int)(tempstop - tempstart).TotalMinutes;
                        }
                    }
                    //00:00 => 18:00
                    if ((dateTypes.Contains(DateType.Weekend) || dateTypes.Contains(DateType.Holiday)) && dateTypes.Any(t => t == DateType.DayBeforeBigHoliday) &&
                       start.TimeOfDay < new TimeSpan(18, 0, 0))
                    {
                        tempstart = start;
                        tempstop = start.Date < endAt.Date || endAt.TimeOfDay > new TimeSpan(18, 0, 0) ? start.Date.AddHours(18).ToDateTimeOffsetSweden() : endAt;
                        minWeekendsIWH += (int)(tempstop - tempstart).TotalMinutes;
                    }
                    //07:00 => 24:00
                    if ((dateTypes.Contains(DateType.Weekend) || dateTypes.Contains(DateType.Holiday)) && dateTypes.Any(t => t == DateType.DayAfterBigHoliday) &&
                        (start.Date < endAt.Date || endAt.TimeOfDay > new TimeSpan(7, 0, 0)))
                    {
                        tempstart = start.Date.AddHours(7).ToDateTimeOffsetSweden();
                        tempstop = start.Date == endAt.Date ? endAt : start.Date.AddDays(1).ToDateTimeOffsetSweden();
                        minWeekendsIWH += (int)(tempstop - tempstart).TotalMinutes;
                    }
                    //Start counting from the first minute on next day
                    start = start.AddDays(1).Date.ToDateTimeOffsetSweden();
                }
            }
            return new Dictionary<PriceListRowType, int>
            {
                { PriceListRowType.InconvenientWorkingHours, minIWH },
                { PriceListRowType.WeekendIWH, minWeekendsIWH },
                { PriceListRowType.BigHolidayWeekendIWH, minIWHBigHoliday }
            };
        }

        private IEnumerable<PriceRowBase> GetPriceRowsPerType(DateTimeOffset startAt, DateTimeOffset endAt, List<PriceListRow> prices, List<MealBreak> mealbreaks = null)
        {
            int maxMinutes = 330;
            TimeSpan span = endAt - startAt;
            int mealbreakMinutes = mealbreaks == null ? 0 : mealbreaks.Sum(mb => mb.Minutes);
            int totalMinutes = (int)span.TotalMinutes - mealbreakMinutes;
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
                yield return GetPriceInformation(extraTimeStartsAt.AddMinutes(mealbreakMinutes), endAt, PriceListRowType.PriceOverMaxTime, prices);
            }
            else
            {
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
                if (!dateTypes.Contains(DateType.Holiday) && !dateTypes.Contains(DateType.BigHolidayFullDay) &&
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

                if (!dateTypes.Contains(DateType.Holiday) && !dateTypes.Contains(DateType.BigHolidayFullDay) &&
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
            if (_dateCalculationService == null)
            {
                return _dbContext.Holidays.SingleOrDefault(h => h.Date.Date == date.Date)?.DateType;
            }
            return _dateCalculationService.Holidays.SingleOrDefault(h => h.Date.Date == date.Date)?.DateType;
        }

        public DisplayPriceInformation GetPriceInformationToDisplay(List<PriceRowBase> priceRows)
        {
            DisplayPriceInformation dpiTotal = new DisplayPriceInformation();
            DisplayPriceInformation separateSubTotalInterpreterCompensation = new DisplayPriceInformation
            {
                SubPriceHeader = PriceRowType.InterpreterCompensation.GetDescription()
            };
            DisplayPriceInformation separateSubTotalTravelcosts = new DisplayPriceInformation
            {
                SubPriceHeader = PriceRowType.TravelCost.GetDescription()
            };

            decimal interpreterCompensation = priceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice);
            dpiTotal.DisplayPriceRows.Add(new DisplayPriceRow { Description = PriceRowType.InterpreterCompensation.GetDescription(), Price = interpreterCompensation, HasSeparateSubTotal = true, DisplayOrder = GetDisplayOrder(PriceRowType.InterpreterCompensation) });
            foreach (PriceRowBase priceRow in priceRows.OrderBy(r => r.PriceRowType))
            {
                if (priceRow.PriceRowType == PriceRowType.InterpreterCompensation && priceRow.PriceListRow != null && priceRow.PriceListRow.PriceListRowType == PriceListRowType.BasePrice)
                {
                    dpiTotal.PriceListTypeDescription = $"Taxa för {priceRow.PriceListRow.PriceListType.GetDescription()}";
                    dpiTotal.CompetencePriceDescription = $"Typ av tolk: {priceRow.PriceListRow.CompetenceLevel.GetDescription()}";
                }
                //for interpreter compensation we get each row in separate subtotal
                if (priceRow.PriceRowType == PriceRowType.InterpreterCompensation)
                {
                    string hourTaxDescription = priceRow.PriceListRow.PriceListRowType == PriceListRowType.BasePrice ? $", taxa {GetDescriptionHourTax(priceRow.PriceListRow.MaxMinutes)} h" : string.Empty;
                    string description = priceRow.PriceListRow.PriceListRowType.GetDescription() + hourTaxDescription + GetQuantityAndPricePerUnit(priceRow);
                    separateSubTotalInterpreterCompensation.DisplayPriceRows.Add(new DisplayPriceRow { Description = description, Price = priceRow.Price * priceRow.Quantity, DisplayOrder = (int)priceRow.PriceListRow.PriceListRowType });
                }
                //for requisition if pricerowType has Travelcost as parent
                else if (EnumHelper.Parent<PriceRowType, PriceRowType?>(priceRow.PriceRowType).HasValue && EnumHelper.Parent<PriceRowType, PriceRowType?>(priceRow.PriceRowType).Value == PriceRowType.TravelCost)
                {
                    separateSubTotalTravelcosts.DisplayPriceRows.Add(new DisplayPriceRow { Description = priceRow.PriceRowType.GetDescription(), Price = priceRow.Price * priceRow.Quantity, DisplayOrder = GetDisplayOrder(priceRow.PriceRowType) });
                }
                else
                {
                    dpiTotal.DisplayPriceRows.Add(new DisplayPriceRow { Description = priceRow.PriceRowType.GetDescription() + GetQuantityAndPricePerUnit(priceRow), Price = priceRow.Price * priceRow.Quantity, DisplayOrder = GetDisplayOrder(priceRow.PriceRowType) });
                }
            }
            dpiTotal.SeparateSubTotal.Add(separateSubTotalInterpreterCompensation);
            if (separateSubTotalTravelcosts.DisplayPriceRows.Any())
            {
                dpiTotal.SeparateSubTotal.Add(separateSubTotalTravelcosts);
                dpiTotal.DisplayPriceRows.Add(new DisplayPriceRow { Description = PriceRowType.TravelCost.GetDescription(), Price = separateSubTotalTravelcosts.TotalPrice, HasSeparateSubTotal = true, DisplayOrder = GetDisplayOrder(PriceRowType.TravelCost) });
            }
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
                case PriceRowType.Outlay:
                    return 6;
                case PriceRowType.CarCompensation:
                    return 7;
                case PriceRowType.PerDiem:
                    return 8;
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
