﻿using DocumentFormat.OpenXml.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class PriceCalculationService
    {
        private readonly TolkDbContext _dbContext;
        private readonly CacheService _cacheService;
        private readonly DateCalculationService _dateCalculationService;


        public PriceCalculationService(TolkDbContext dbContext,
            DateCalculationService dateCalculationService,
            CacheService cacheService
            )
        {
            _dbContext = dbContext;
            _dateCalculationService = dateCalculationService;
            _cacheService = cacheService;
        }

        public PriceCalculationService(TolkDbContext tolkDbContext, CacheService cacheService)
        {
            _dbContext = tolkDbContext;
            _cacheService = cacheService;
        }

        public PriceInformation GetPrices(Request request, DateTimeOffset calculateFrom, CompetenceAndSpecialistLevel competenceLevel, InterpreterLocation? interpreterLocation, decimal? expectedTravelCost)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var order = request.Order;
            var interpreterLocationCorCalculation = interpreterLocation ?? (request.InterpreterLocation.HasValue ? (InterpreterLocation)request.InterpreterLocation.Value :
                order.InterpreterLocations.OrderBy(l => l.Rank).First().InterpreterLocation);
            return GetPrices(
                order.StartAt,
                order.Duration,
                EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>(competenceLevel),
                order.CustomerOrganisation.PriceListType,
                order.CreatedAt,
                GetCalculatedBrokerFee(order, calculateFrom, request.Ranking.FrameworkAgreement.BrokerFeeCalculationType, EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>(competenceLevel), request.RankingId, interpreterLocationCorCalculation),
                expectedTravelCost);
        }

        public PriceInformation GetPrices(DateTimeOffset startAt, TimeSpan duration, CompetenceLevel competenceLevel, PriceListType listType, DateTimeOffset orderCreatedDate, PriceRowBase brokerFee, decimal? travelCost = null)
        {
            return CompletePricesWithExtraCharges(startAt, duration,
                MergePriceListRowsAndReduceForMealBreak(GetPriceRowsPerType(startAt, duration, GetPriceList(startAt, competenceLevel, listType))).ToList(),
                travelCost,
                brokerFee);
        }

        public PriceRowBase GetCalculatedBrokerFee(Order order, DateTimeOffset calculateFrom, BrokerFeeCalculationType brokerFeeCalculationType, CompetenceLevel cl, int rankingId, InterpreterLocation interpreterLocation)
        {
            return brokerFeeCalculationType switch
            {
                BrokerFeeCalculationType.ByRegionAndBroker =>
                    GetPriceRowBrokerFeeByRanking(GetNoOfDays(order.StartAt, order.EndAt), calculateFrom, cl, rankingId),
                BrokerFeeCalculationType.ByRegionAndServiceType=>
                    GetPriceRowBrokerFeeByServiceType(
                        GetNoOfDays(order.StartAt, order.EndAt),
                        calculateFrom,
                        cl,
                        interpreterLocation,
                        order.RegionId),
                _ => throw new NotImplementedException($"Broker fee cannot be calculated for the unknown {nameof(BrokerFeeCalculationType)}: {brokerFeeCalculationType}")
            };
        }

        public PriceInformation GetPricesRequisition(DateTimeOffset startAt, TimeSpan duration, DateTimeOffset originStartAt, TimeSpan originDuration, CompetenceLevel competenceLevel, PriceListType listType, out bool useRequestPricerows, int? timeWasteNormalTime, int? timeWasteIWHTime, IEnumerable<PriceRowBase> requestPriceRows, decimal? outlay, Order replacingOrder, DateTimeOffset orderCreatedDate, List<MealBreak> mealbreaks = null)
        {
            var prices = GetPriceList(startAt, competenceLevel, listType);

            //get mealbeaks if any
            IDictionary<PriceListRowType, int> mealbreakTimes = GetMealBreakTimesAndTypes(mealbreaks);

            //compare compensation for origin times with requisition times (including mealbreaks), broker/interpreter should have the compensation that get most payed
            var pricesRequisition = MergePriceListRowsAndReduceForMealBreak(GetPriceRowsPerType(startAt, duration, prices, mealbreaks), mealbreakTimes);
            var pricesOriginTimes = MergePriceListRowsAndReduceForMealBreak(GetPriceRowsPerType(originStartAt, originDuration, prices, mealbreaks), mealbreakTimes);

            useRequestPricerows = UsePricesOriginTimes(pricesRequisition, pricesOriginTimes);

            //if we should use request, set start and end from request start and end
            var pricesToUse = useRequestPricerows ? pricesOriginTimes.ToList() : pricesRequisition.ToList();

            //if replacementorder - check and compare start- and endtime from replacing order
            if (replacingOrder != null)
            {
                var pricesReplacingOrder = MergePriceListRowsAndReduceForMealBreak(GetPriceRowsPerType(replacingOrder.StartAt, replacingOrder.Duration, prices, mealbreaks), mealbreakTimes);

                bool useReplacingOrderTimes = UsePricesOriginTimes(useRequestPricerows ? pricesOriginTimes : pricesRequisition, pricesReplacingOrder);
                if (useReplacingOrderTimes)
                {
                    pricesToUse = pricesReplacingOrder.ToList();
                    useRequestPricerows = useReplacingOrderTimes;
                }
            }
            //get lost time and extra charges
            pricesToUse.AddRange(GetLostTimePriceRows(startAt, timeWasteNormalTime, timeWasteIWHTime, prices));
            return CompletePricesWithExtraCharges(startAt, duration, pricesToUse, null, requestPriceRows.Single(rpr => rpr.PriceRowType == PriceRowType.BrokerFee), outlay);
        }

        /// <summary>
        /// If order passes midnight their might be two rows of same pricetype, merge these to one 
        /// Also check if two duplicate rows have less or equal compensation time together. Then it should be reduced, e.g. 23:45-00:45 (should be two 30 min periods not three)
        /// </summary>
        public static IEnumerable<PriceRowBase> MergePriceListRowsAndReduceForMealBreak(IEnumerable<PriceRowBase> pricePerType, IDictionary<PriceListRowType, int> mealbreakDictionary = null)
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

        private static int GetReducedQuantityForMealbreak(PriceRowBase newPriceRow, int mealbreakMinutes)
        {
            int reducedMinutes = newPriceRow.Minutes - mealbreakMinutes;
            return reducedMinutes > 0 ? reducedMinutes % newPriceRow.PriceListRow.MaxMinutes > 0 ? (reducedMinutes / newPriceRow.PriceListRow.MaxMinutes) + 1 : reducedMinutes / newPriceRow.PriceListRow.MaxMinutes : 0;
        }

        private PriceInformation CompletePricesWithExtraCharges(DateTimeOffset startAt, TimeSpan duration, List<PriceRowBase> priceListRowsPerPriceType, decimal? travelCost, PriceRowBase brokerFeePriceRow, decimal? outlay = null)
        {
            DateTimeOffset endAt = startAt.AddTicks(duration.Ticks);
            List<PriceRowBase> allPriceRows = new List<PriceRowBase>
            {
                GetPriceRowSocialInsuranceCharge(startAt, endAt, priceListRowsPerPriceType),
                GetPriceRowAdministrativeCharge(startAt, endAt, priceListRowsPerPriceType),
                brokerFeePriceRow
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

            allPriceRows.Add(GetRoundedPriceRow(startAt, endAt, allPriceRows));

            var priceInformation = new PriceInformation
            {
                PriceRows = allPriceRows
            };
            return priceInformation;
        }

        private static PriceRowBase GetTravelCostRow(DateTimeOffset startAt, DateTimeOffset endAt, decimal? travelCost, PriceRowType travelcostType)
        {
            return new PriceRowBase { StartAt = startAt, EndAt = endAt, Price = travelCost.Value, Quantity = 1, PriceRowType = travelcostType };
        }

        public static PriceRowBase GetRoundedPriceRow(DateTimeOffset startAt, DateTimeOffset endAt, List<PriceRowBase> allPriceRows)
        {
            decimal roundings = 0;
            allPriceRows.Sum(pr => roundings += pr.Decimals);
            roundings -= Math.Floor(roundings);
            roundings = roundings > Convert.ToDecimal(0.5) ? 1 - roundings : -roundings;
            //if roundings = 0 we create a row with 0 to display anyway
            return new PriceRowBase { StartAt = startAt, EndAt = endAt, Price = roundings, Quantity = 1, PriceRowType = PriceRowType.RoundedPrice };
        }

        public static IEnumerable<PriceRowBase> GetLostTimePriceRows(DateTimeOffset startAt, int? timeWasteNormalTime, int? timeWasteIWHTime, List<PriceListRow> prices)
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

        private static bool UsePricesOriginTimes(IEnumerable<PriceRowBase> priceRequsition, IEnumerable<PriceRowBase> priceOriginTimes)
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

        public PriceRowBase GetPriceRowBrokerFeeByRanking(int days, DateTimeOffset calculateFrom, CompetenceLevel competenceLevel, int rankingId)
        {
            //One broker fee per day                                    
            var priceRow = _cacheService.BrokerFeeByRegionAndBrokerPriceList.Where(br => br.RankingId == rankingId && br.CompetenceLevel == competenceLevel && br.StartDate.Date <= calculateFrom.Date && br.EndDate.Date >= calculateFrom.Date).Count() == 1 ?
                 _cacheService.BrokerFeeByRegionAndBrokerPriceList.Single(br => br.RankingId == rankingId && br.CompetenceLevel == competenceLevel && br.StartDate.Date <= calculateFrom.Date && br.EndDate.Date >= calculateFrom.Date) :
                 _cacheService.BrokerFeeByRegionAndBrokerPriceList.Where(br => br.RankingId == rankingId && br.CompetenceLevel == competenceLevel && br.StartDate.Date <= calculateFrom.Date).OrderByDescending(br => br.EndDate).First();
            return new PriceRowBase
            {
                StartAt = calculateFrom,
                EndAt = calculateFrom,
                PriceRowType = PriceRowType.BrokerFee,
                Quantity = days,
                Price = priceRow.PriceToUse,
                PriceListRowId = priceRow.PriceListRowId
            };
        }

        public PriceRowBase GetPriceRowBrokerFeeByServiceType(int days, DateTimeOffset calculateFrom, CompetenceLevel competenceLevel, InterpreterLocation interpreterLocation, int regionId)
        {
            return _cacheService.BrokerFeeByRegionAndServiceTypePriceList
                .Where(br =>
                    br.CompetenceLevel == competenceLevel &&
                    br.InterpreterLocation == interpreterLocation &&
                    br.RegionId == regionId &&
                    br.StartDate.Date <= calculateFrom.Date && br.EndDate.Date >= calculateFrom.Date)
                .Select(f => new PriceRowBase
                {
                    StartAt = calculateFrom,
                    EndAt = calculateFrom,
                    PriceRowType = PriceRowType.BrokerFee,
                    Quantity = days,
                    Price = f.BrokerFee,
                }).SingleOrDefault();


        }

        private PriceRowBase GetPriceCalculationCharge(DateTimeOffset startAt, DateTimeOffset endAt, List<PriceRowBase> priceListRowsPerPriceType, ChargeType chargeType)
        {
            var chargeRow = _dbContext.PriceCalculationCharges.Single(c => c.ChargeTypeId == chargeType && c.StartDate <= startAt.DateTime && c.EndDate >= startAt.DateTime);
            return new PriceRowBase { StartAt = startAt, EndAt = endAt, Price = chargeRow.ChargePercentage * priceListRowsPerPriceType.Sum(m => m.TotalPrice) / 100, Quantity = 1, PriceRowType = chargeType == ChargeType.SocialInsuranceCharge ? PriceRowType.SocialInsuranceCharge : PriceRowType.AdministrativeCharge, PriceCalculationChargeId = chargeRow.PriceCalculationChargeId };
        }

        public static int GetNoOfDays(DateTimeOffset startAt, DateTimeOffset endAt)
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
                    (dateTypes.Contains(DateType.Holiday) && !dateTypes.Any(t => t == DateType.DayBeforeBigHoliday || t == DateType.DayAfterBigHoliday)) ||
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

                    // => 07:00
                    if (((!dateTypes.Any(t => t == DateType.Holiday || t == DateType.BigHolidayFullDay) &&
                    dateTypes.Any(t => t == DateType.WeekDay)) || dateTypes.Any(t => t == DateType.DayAfterBigHoliday)) && start.TimeOfDay < new TimeSpan(7, 0, 0))
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
                    // 18:00 =>
                    if (((!dateTypes.Any(t => t == DateType.Holiday || t == DateType.BigHolidayFullDay)) &&
                        dateTypes.Any(t => t == DateType.WeekDay) || dateTypes.Any(t => t == DateType.DayBeforeBigHoliday)) &&
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
                    // => 18:00 (or end)
                    if (dateTypes.Any(t => t == DateType.Weekend || t == DateType.Holiday) && dateTypes.Any(t => t == DateType.DayBeforeBigHoliday) &&
                        start.TimeOfDay < new TimeSpan(18, 0, 0))
                    {
                        tempstart = start;
                        tempstop = start.Date < endAt.Date || endAt.TimeOfDay > new TimeSpan(18, 0, 0) ? start.Date.AddHours(18).ToDateTimeOffsetSweden() : endAt;
                        minWeekendsIWH += (int)(tempstop - tempstart).TotalMinutes;
                    }
                    // 07:00 (or start) => 
                    if (dateTypes.Any(t => t == DateType.Weekend || t == DateType.Holiday) && dateTypes.Any(t => t == DateType.DayAfterBigHoliday) &&
                        (start.Date < endAt.Date || endAt.TimeOfDay > new TimeSpan(7, 0, 0)))
                    {
                        tempstart = start.Hour > 7 ? start : start.Date.AddHours(7).ToDateTimeOffsetSweden();
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

        private IEnumerable<PriceRowBase> GetPriceRowsPerType(DateTimeOffset startAt, TimeSpan duration, List<PriceListRow> prices, List<MealBreak> mealbreaks = null)
        {
            int maxMinutes = 330;
            DateTimeOffset endAt = startAt.AddTicks(duration.Ticks);
            int mealbreakMinutes = mealbreaks == null ? 0 : mealbreaks.Sum(mb => mb.Minutes);
            int totalMinutes = (int)duration.TotalMinutes - mealbreakMinutes;
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
                //for weeekend and holidays check that no DayBeforeBigHoliday or DayAfterBigHoliday (then they should be calculated separately)
                var dateTypes = GetDateTypes(start.Date);
                if (dateTypes.Contains(DateType.BigHolidayFullDay) ||
                    (dateTypes.Contains(DateType.Holiday) && !dateTypes.Any(t => t == DateType.DayBeforeBigHoliday || t == DateType.DayAfterBigHoliday)) ||
                    (dateTypes.Contains(DateType.Weekend) && !dateTypes.Any(t => t == DateType.DayBeforeBigHoliday || t == DateType.DayAfterBigHoliday)))
                {
                    yield return GetPriceInformation(
                        start,
                        start.Date == endAt.Date ? endAt : start.Date.AddDays(1).ToDateTimeOffsetSweden(),
                        dateTypes.Contains(DateType.BigHolidayFullDay) ? PriceListRowType.BigHolidayWeekendIWH : PriceListRowType.WeekendIWH,
                        prices
                    );
                }

                // => 07:00 
                if (((!dateTypes.Any(t => t == DateType.Holiday || t == DateType.BigHolidayFullDay) &&
                    dateTypes.Any(t => t == DateType.WeekDay)) || dateTypes.Any(t => t == DateType.DayAfterBigHoliday)) && start.TimeOfDay < new TimeSpan(7, 0, 0))
                {
                    yield return GetPriceInformation(
                        start,
                        start.Date < endAt.Date || endAt.TimeOfDay > new TimeSpan(7, 0, 0) ? start.Date.AddHours(7).ToDateTimeOffsetSweden() : endAt,
                        dateTypes.Contains(DateType.DayAfterBigHoliday) ? PriceListRowType.BigHolidayWeekendIWH : PriceListRowType.InconvenientWorkingHours,
                        prices
                    );
                }
                // 18:00 => 
                if (((!dateTypes.Any(t => t == DateType.Holiday || t == DateType.BigHolidayFullDay)) &&
                    dateTypes.Any(t => t == DateType.WeekDay) || dateTypes.Any(t => t == DateType.DayBeforeBigHoliday)) &&
                    (start.Date < endAt.Date || endAt.TimeOfDay > new TimeSpan(18, 0, 0)))
                {
                    yield return GetPriceInformation(
                        start.Hour < 18 ? start.Date.AddHours(18).ToDateTimeOffsetSweden() : start,
                        start.Date == endAt.Date ? endAt : start.Date.AddDays(1).ToDateTimeOffsetSweden(),
                        dateTypes.Contains(DateType.DayBeforeBigHoliday) ? PriceListRowType.BigHolidayWeekendIWH : PriceListRowType.InconvenientWorkingHours,
                        prices
                    );
                }
                // => 18:00 (or end)
                if (dateTypes.Any(t => t == DateType.Weekend || t == DateType.Holiday) && dateTypes.Any(t => t == DateType.DayBeforeBigHoliday) &&
                   start.TimeOfDay < new TimeSpan(18, 0, 0))
                {
                    yield return GetPriceInformation(
                        start,
                        start.Date < endAt.Date || endAt.TimeOfDay > new TimeSpan(18, 0, 0) ? start.Date.AddHours(18).ToDateTimeOffsetSweden() : endAt,
                        PriceListRowType.WeekendIWH,
                        prices
                     );
                }
                // 07:00 (or start) =>
                if (dateTypes.Any(t => t == DateType.Weekend || t == DateType.Holiday) && dateTypes.Any(t => t == DateType.DayAfterBigHoliday) &&
                    (start.Date < endAt.Date || endAt.TimeOfDay > new TimeSpan(7, 0, 0)))
                {
                    yield return GetPriceInformation(
                        start = start.Hour > 7 ? start : start.Date.AddHours(7).ToDateTimeOffsetSweden(),
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
            var holidayDayTypes = GetHolidayDayTypesIfAny(date);
            if (holidayDayTypes.Any())
            {
                foreach (var holidayDayType in holidayDayTypes)
                    yield return holidayDayType;
            }
        }

        public IEnumerable<DateType> GetHolidayDayTypesIfAny(DateTime date)
        {
            if (_dateCalculationService == null)
            {
                return _dbContext.Holidays.Where(h => h.Date.Date == date.Date)?.Select(h => h.DateType);
            }
            return _cacheService.Holidays.Where(h => h.Date.Date == date.Date)?.Select(h => h.DateType);
        }

        public static DisplayPriceInformation GetPriceInformationToDisplay(IEnumerable<PriceRowBase> priceRows)
        {
            DisplayPriceInformation dpiTotal = new DisplayPriceInformation();
            DisplayPriceInformation separateSubTotalInterpreterCompensation = new DisplayPriceInformation
            {
                SubPriceHeader = PriceRowType.InterpreterCompensation.GetDescription()
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
                else
                {
                    dpiTotal.DisplayPriceRows.Add(new DisplayPriceRow { Description = priceRow.PriceRowType.GetDescription() + GetQuantityAndPricePerUnit(priceRow), Price = priceRow.Price * priceRow.Quantity, DisplayOrder = GetDisplayOrder(priceRow.PriceRowType) });
                }
            }
            dpiTotal.SeparateSubTotal.Add(separateSubTotalInterpreterCompensation);
            return dpiTotal;
        }

        private static int GetDisplayOrder(PriceRowType type)
        {
            return type switch
            {
                PriceRowType.InterpreterCompensation => 1,
                PriceRowType.SocialInsuranceCharge => 2,
                PriceRowType.AdministrativeCharge => 3,
                PriceRowType.BrokerFee => 4,
                PriceRowType.TravelCost => 5,
                PriceRowType.Outlay => 6,
                PriceRowType.RoundedPrice => 100,
                _ => 30,
            };
        }

        private static string GetQuantityAndPricePerUnit(PriceRowBase priceRow)
        {
            return priceRow.Quantity > 1 ? $" ({priceRow.Quantity} st á {priceRow.Price})" : string.Empty;
        }

        private static string GetDescriptionHourTax(int maxMinutes)
        {
            double noOfHours = (double)maxMinutes / 60;
            return noOfHours == 1 ? $"0-{noOfHours}" : $"{noOfHours - 0.5}-{noOfHours}";
        }
    }
}
