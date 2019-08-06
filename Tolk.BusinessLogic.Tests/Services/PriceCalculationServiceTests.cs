using System;
using Tolk.BusinessLogic.Services;
using Xunit;
using FluentAssertions;
using Tolk.BusinessLogic.Data;
using Microsoft.EntityFrameworkCore;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Utilities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Tolk.BusinessLogic.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace Tolk.BusinessLogic.Tests.Services
{
    public class PriceCalculationServiceTests
    {

        private const string DbNameWithPriceData = "PriceCalculationService_WithPriceData";
        private const string DefaultStartDate = "2018-10-10 10:00:00";
        private const string DefaultEndDate = "2018-10-10 12:00:00";
        private const double SocialInsuranceCharge = 31.42;
        private const double AdministrativeCharge = 0.7;
        private const int DefaultRankingId = 1;

        //constants from MockEntities.PriceListRows
        private const double Price_1H_Court_Comp1 = 352;
        private const double Price_1_5H_Court_Comp1 = 479;
        private const double Price_1H_Court_Comp2 = 409;
        private const double Price_1H_Court_Comp3 = 480;
        private const double Price_1H_Court_Comp4 = 606;

        private const double Price_2H_Court_Comp1 = 606;
        private const double Price_2_5H_Court_Comp1 = 733;
        private const double Price_3H_Court_Comp1 = 860;
        private const double Price_3_5H_Court_Comp1 = 987;
        private const double Price_4H_Court_Comp1 = 1114;
        private const double Price_4_5H_Court_Comp1 = 1241;
        private const double Price_5H_Court_Comp1 = 1368;
        private const double Price_5_5H_Court_Comp1 = 1495;

        private const double Price_2H_Court_Comp4 = 1084;
        private const double Price_3H_Court_Comp4 = 1562;
        private const double Price_4H_Court_Comp4 = 2040;
        private const double Price_5H_Court_Comp4 = 2518;
        private const double Price_5_5H_Court_Comp4 = 2757;

        private const double Price_1H_Other_Comp4 = 511;
        private const double Price_2H_Other_Comp4 = 965;
        private const double Price_3H_Other_Comp4 = 1419;
        private const double Price_4H_Other_Comp4 = 1873;
        private const double Price_5H_Other_Comp4 = 2327;
        private const double Price_5_5H_Other_Comp4 = 2554;

        private const double Broker_Fee_Price_Comp1 = 35;
        private const double Broker_Fee_Price_Comp2 = 41;
        private const double Broker_Fee_Price_Comp3 = 48;
        private const double Broker_Fee_Price_Comp4 = 61;

        private const double Price_OverMaxTime_Court_Comp1 = 127;
        private const double Price_OverMaxTime_Court_Comp4 = 239;
        private const double Price_OverMaxTime_Other_Comp4 = 227;
        private const double Price_IWH_30M__Court_Comp1 = 77;
        private const double Price_IWH_Weekend_30M__Court_Comp1 = 127;
        private const double Price_IWH_BigHoliday_30M__Court_Comp1 = 154;

        private const double Price_LostTime_60M__Court_Comp1 = 191;
        private const double Price_IWH_LostTime_30M__Court_Comp1 = 77;

        public PriceCalculationServiceTests()
        {

            using (var tolkDbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                tolkDbContext.AddRange(MockEntities.PriceListRows.Where(newPrice =>
                !tolkDbContext.PriceListRows.Select(existPrice => existPrice.PriceListRowId).Contains(newPrice.PriceListRowId)));

                tolkDbContext.AddRange(MockEntities.PriceCalculationCharges.Where(newCharge =>
                !tolkDbContext.PriceCalculationCharges.Select(existCharge => existCharge.PriceCalculationChargeId).Contains(newCharge.PriceCalculationChargeId)));

                tolkDbContext.AddRange(MockEntities.Rankings.Where(newRank =>
                !tolkDbContext.Rankings.Select(existRank => existRank.RankingId).Contains(newRank.RankingId)));

                tolkDbContext.AddRange(MockEntities.Holidays.Where(newHoliday =>
                !tolkDbContext.Holidays.Select(existingHoliday => existingHoliday.Date).Contains(newHoliday.Date)));

                tolkDbContext.SaveChanges();
            }
        }

        private TolkDbContext CreateTolkDbContext(string databaseName = "empty")
        {
            var options = new DbContextOptionsBuilder<TolkDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new TolkDbContext(options);
        }

        [Theory]
        [InlineData(100, 0)]//should return row
        [InlineData(100.20, -0.20)]
        [InlineData(100.80, 0.20)]
        public void GetRoundedPriceRow(decimal price, decimal actual)
        {
            new PriceCalculationService().GetRoundedPriceRow(DateTime.Parse(DefaultStartDate), DateTime.Parse(DefaultEndDate), new List<PriceRowBase> { GetPriceRow(price, 1) }).Price.Should().Be(actual, "there are {0} rounded decimals in {1}", actual, price);
        }

        [Theory]
        [InlineData(new[] { 100.1234, 100.1234, 100.1234 }, -0.36)]
        [InlineData(new[] { 100.20, 100.20, 100.20 }, 0.40)]
        [InlineData(new[] { 100.82, 100.67, 100.8951 }, -0.39)]
        public void GetRoundedPriceRowWithManyRows(double[] prices, decimal actual)
        {
            List<PriceRowBase> priceRows = new List<PriceRowBase>();
            foreach (decimal price in prices)
            {
                priceRows.Add(GetPriceRow(price, 1));
            }
            new PriceCalculationService().GetRoundedPriceRow(DateTime.Parse(DefaultStartDate), DateTime.Parse(DefaultEndDate), priceRows).Price.Should().Be(actual, "there are {0} rounded decimals in {1}", actual, prices.Sum(pr => pr));
        }

        [Theory]
        [InlineData("2018-10-10", new[] { DateType.WeekDay })]
        [InlineData("2018-10-13", new[] { DateType.Weekend })]
        [InlineData("2018-12-24", new[] { DateType.WeekDay, DateType.BigHolidayFullDay })]

        public void GetDateTypes(string date, DateType[] actual)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                DateType[] found = new PriceCalculationService(tolkdbContext).GetDateTypes(DateTime.Parse(date)).ToArray();
                Assert.True(Enumerable.SequenceEqual(found.OrderBy(t => t), actual.OrderBy(t => t)));
            }
        }

        [Theory]
        [InlineData("2018-10-10 10:00:00", CompetenceLevel.OtherInterpreter, PriceListType.Other, 16)]
        [InlineData("2018-10-10 10:00:00", CompetenceLevel.EducatedInterpreter, PriceListType.Other, 16)]
        [InlineData("2018-10-10 10:00:00", CompetenceLevel.AuthorizedInterpreter, PriceListType.Other, 16)]
        [InlineData("2018-10-10 10:00:00", CompetenceLevel.SpecializedInterpreter, PriceListType.Other, 16)]
        [InlineData("2018-10-10 10:00:00", CompetenceLevel.OtherInterpreter, PriceListType.Court, 16)]
        [InlineData("2018-10-10 10:00:00", CompetenceLevel.EducatedInterpreter, PriceListType.Court, 16)]
        [InlineData("2018-10-10 10:00:00", CompetenceLevel.AuthorizedInterpreter, PriceListType.Court, 16)]
        [InlineData("2018-10-10 10:00:00", CompetenceLevel.SpecializedInterpreter, PriceListType.Court, 16)]
        public void GetPriceListTest(string startAt, CompetenceLevel compLevel, PriceListType priceListType, int actualNoOfRows)
        {
            GetPriceList(DateTime.Parse(startAt), compLevel, priceListType).Count().Should().Be(actualNoOfRows, "number of rows {0}", actualNoOfRows);
        }

        private List<PriceListRow> GetPriceList(DateTime startAt, CompetenceLevel compLevel, PriceListType priceListType)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                return new PriceCalculationService(tolkdbContext).GetPriceList(startAt, compLevel, priceListType);
            }
        }

        [Theory]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.OtherInterpreter, PriceListType.Other, 31, 0, 1)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.EducatedInterpreter, PriceListType.Other, 31, 0, 1)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.AuthorizedInterpreter, PriceListType.Other, 31, 0, 1)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.SpecializedInterpreter, PriceListType.Other, 31, 0, 1)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.OtherInterpreter, PriceListType.Court, 31, 0, 1)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.EducatedInterpreter, PriceListType.Court, 31, 0, 1)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.AuthorizedInterpreter, PriceListType.Court, 31, 0, 1)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.SpecializedInterpreter, PriceListType.Court, 31, 0, 1)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.OtherInterpreter, PriceListType.Other, 31, 31, 2)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.EducatedInterpreter, PriceListType.Other, 31, 31, 2)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.AuthorizedInterpreter, PriceListType.Other, 31, 31, 2)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.SpecializedInterpreter, PriceListType.Other, 31, 31, 2)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.OtherInterpreter, PriceListType.Court, 31, 31, 2)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.EducatedInterpreter, PriceListType.Court, 31, 31, 2)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.AuthorizedInterpreter, PriceListType.Court, 31, 31, 2)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.SpecializedInterpreter, PriceListType.Court, 31, 31, 2)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.OtherInterpreter, PriceListType.Other, 0, 0, 0)]

        public void GetLostTimePriceRows(string startAt, string endAt, CompetenceLevel compLevel, PriceListType priceListType, int lostTime, int lostTimeIWH, int actual)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                var prices = GetPriceList(DateTime.Parse(startAt), compLevel, priceListType);
                IEnumerable<PriceRowBase> list = new PriceCalculationService(tolkdbContext).GetLostTimePriceRows(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), lostTime, lostTimeIWH, prices);
                list.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(actual, "number of rows {0}", actual);
            }
        }

        private PriceRowBase GetPriceRow(decimal price, int quantity)
        {
            return GetPriceRow(DateTime.Parse(DefaultStartDate), DateTime.Parse(DefaultEndDate), price, quantity);
        }

        private PriceRowBase GetPriceRow(DateTime startAt, DateTime endAt, decimal price, int quantity)
        {
            return new PriceRowBase { StartAt = startAt, EndAt = endAt, Price = price, Quantity = 1 };
        }

        [Theory]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", 1)]
        [InlineData("2018-10-10 16:00:00", "2018-10-11 02:00:00", 2)]
        [InlineData("2018-10-10 23:00:00", "2018-10-11 00:00:00", 1)]
        public void GetNoOfDays(string startAt, string endAt, int actual)
        {
            new PriceCalculationService().GetNoOfDays(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden()).Should().Be(actual, "there are {0} days between {1} and {2}", actual, startAt, endAt);
        }

        [Theory]
        //baseprice Court competence level 1
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_1H_Court_Comp1, 1)]//1h 
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_2H_Court_Comp1, 1)]//2h 
        [InlineData("2018-10-10 10:00:00", "2018-10-10 13:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_3H_Court_Comp1, 1)]//3h 
        [InlineData("2018-10-10 10:00:00", "2018-10-10 14:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_4H_Court_Comp1, 1)]//4h 
        [InlineData("2018-10-10 10:00:00", "2018-10-10 15:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_5H_Court_Comp1, 1)]//5h
        [InlineData("2018-10-10 10:00:00", "2018-10-10 16:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_OverMaxTime_Court_Comp1), 2)]//(extra comp. for > 5,5h)
        //baseprice Court competence level 4
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Court, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Price_1H_Court_Comp4, 1)]//1h
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", PriceListType.Court, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Price_2H_Court_Comp4, 1)]//2h
        [InlineData("2018-10-10 10:00:00", "2018-10-10 13:00:00", PriceListType.Court, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Price_3H_Court_Comp4, 1)]//3h
        [InlineData("2018-10-10 10:00:00", "2018-10-10 14:00:00", PriceListType.Court, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Price_4H_Court_Comp4, 1)]//4h 
        [InlineData("2018-10-10 10:00:00", "2018-10-10 15:00:00", PriceListType.Court, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Price_5H_Court_Comp4, 1)]//5h
        [InlineData("2018-10-10 10:00:00", "2018-10-10 16:00:00", PriceListType.Court, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp4 + Price_OverMaxTime_Court_Comp4), 2)]//(extra comp. for > 5,5h)
        //baseprice Other competence level 4
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Other, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Price_1H_Other_Comp4, 1)]//1h
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", PriceListType.Other, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Price_2H_Other_Comp4, 1)]//2h
        [InlineData("2018-10-10 10:00:00", "2018-10-10 13:00:00", PriceListType.Other, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Price_3H_Other_Comp4, 1)]//3h
        [InlineData("2018-10-10 10:00:00", "2018-10-10 14:00:00", PriceListType.Other, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Price_4H_Other_Comp4, 1)]//4h 
        [InlineData("2018-10-10 10:00:00", "2018-10-10 15:00:00", PriceListType.Other, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Price_5H_Other_Comp4, 1)]//5h
        [InlineData("2018-10-10 10:00:00", "2018-10-10 16:00:00", PriceListType.Other, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, (Price_5_5H_Other_Comp4 + Price_OverMaxTime_Other_Comp4), 2)]//(extra comp. for > 5,5h)
        public void BasePrice_InterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        [InlineData("2018-10-10 17:00:00", "2018-10-10 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 2), 2)]//2h work 1h IWH
        [InlineData("2018-10-10 18:00:00", "2018-10-10 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 2), 2)]//1h work IWH
        [InlineData("2018-10-10 18:00:00", "2018-10-10 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 4), 2)]//2h work IWH
        [InlineData("2018-10-10 17:00:00", "2018-10-10 22:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 8), 2)]//5h work IWH
        public void IWH_WeekdayEveningInterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        [InlineData("2018-10-10 02:00:00", "2018-10-10 06:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_4H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 8), 2)]//4h work 4h IWH
        [InlineData("2018-10-10 06:00:00", "2018-10-10 09:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_3H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 2), 2)]//3h work 1h IWH
        [InlineData("2018-10-10 07:00:00", "2018-10-10 09:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_2H_Court_Comp1, 1)]//2h work no IWH
        public void IWH_WeekdayMorningInterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        [InlineData("2018-10-10 05:00:00", "2018-10-10 10:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 4), 2)]//5h 2h iwh
        [InlineData("2018-10-10 06:00:00", "2018-10-10 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_OverMaxTime_Court_Comp1 * 15 + Price_IWH_30M__Court_Comp1 * 4), 3)]//13h 2h iwh
        [InlineData("2018-10-10 17:00:00", "2018-10-10 22:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5H_Court_Comp1 + + Price_IWH_30M__Court_Comp1 * 8), 2)]//5h 4h iwh
        [InlineData("2018-10-10 17:00:00", "2018-10-11 02:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_OverMaxTime_Court_Comp1 * 7 + Price_IWH_30M__Court_Comp1 * 16), 3)]//9h work 8h iwh
        [InlineData("2018-10-10 23:00:00", "2018-10-11 02:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_3H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 6), 2)]//3h work 3h iwh
        public void IWH_WeekdayDifferentPeriodsInterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        [InlineData("2018-10-13 10:00:00", "2018-10-13 12:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work weekend
        [InlineData("2018-10-13 16:00:00", "2018-10-13 18:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work weekend
        [InlineData("2018-10-13 17:00:00", "2018-10-13 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work weekend
        [InlineData("2018-10-13 18:00:00", "2018-10-13 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work weekend
        [InlineData("2018-10-13 02:00:00", "2018-10-13 04:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work weekend
        [InlineData("2018-10-13 05:00:00", "2018-10-13 07:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work weekend
        [InlineData("2018-10-13 06:00:00", "2018-10-13 08:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work weekend
        [InlineData("2018-10-13 07:00:00", "2018-10-13 09:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work weekend
        public void IWH_WeekendInterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        [InlineData("2018-06-06 10:00:00", "2018-06-06 12:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work holiday
        [InlineData("2018-06-06 16:00:00", "2018-06-06 18:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work holiday
        [InlineData("2018-06-06 17:00:00", "2018-06-06 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work holiday
        [InlineData("2018-06-06 18:00:00", "2018-06-06 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work holiday
        [InlineData("2018-06-06 02:00:00", "2018-06-06 04:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work holiday
        [InlineData("2018-06-06 05:00:00", "2018-06-06 07:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work holiday
        [InlineData("2018-06-06 06:00:00", "2018-06-06 08:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work holiday
        [InlineData("2018-06-06 07:00:00", "2018-06-06 09:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work holiday
        [InlineData("2018-06-05 17:00:00", "2018-06-06 01:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_OverMaxTime_Court_Comp1 * 5 + Price_IWH_30M__Court_Comp1 * 12 + Price_IWH_Weekend_30M__Court_Comp1 * 2), 4)]//8h work 1h holiday + 6h iwh
        [InlineData("2018-06-06 17:00:00", "2018-06-07 01:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_OverMaxTime_Court_Comp1 * 5 + Price_IWH_30M__Court_Comp1 * 2 + Price_IWH_Weekend_30M__Court_Comp1 * 14), 4)]//8h work 7h holiday + 1h iwh
        public void IWH_HolidayInterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        //2020-06-06 is weekend (saturday)
        [InlineData("2020-06-06 10:00:00", "2020-06-06 12:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work holiday/weekend
        [InlineData("2020-06-06 16:00:00", "2020-06-06 18:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work holiday/weekend
        [InlineData("2020-06-06 17:00:00", "2020-06-06 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work holiday/weekend
        [InlineData("2020-06-06 18:00:00", "2020-06-06 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work holiday/weekend
        [InlineData("2020-06-06 02:00:00", "2020-06-06 04:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work holiday/weekend
        [InlineData("2020-06-06 05:00:00", "2020-06-06 07:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work holiday/weekend
        [InlineData("2020-06-06 06:00:00", "2020-06-06 08:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work holiday/weekend
        [InlineData("2020-06-06 07:00:00", "2020-06-06 09:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work holiday/weekend
        public void IWH_HolidayIsWeekendInterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        [InlineData("2018-10-13 22:00:00", "2018-10-14 02:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_4H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 8), 2)]//2h work weekend
        [InlineData("2018-10-13 16:00:00", "2018-10-14 02:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_OverMaxTime_Court_Comp1 * 9 + Price_IWH_Weekend_30M__Court_Comp1 * 20), 3)]//10h work weekend
        [InlineData("2018-10-13 23:45:00", "2018-10-14 00:15:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 1), 2)]//30m work weekend
        [InlineData("2018-10-13 23:00:00", "2018-10-14 12:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_OverMaxTime_Court_Comp1 * 15 + Price_IWH_Weekend_30M__Court_Comp1 * 26), 3)]//13h work weekend
        public void IWH_WeekendPassingMidnightToWeekendInterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        [InlineData("2018-10-14 22:00:00", "2018-10-15 02:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_4H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4 + Price_IWH_30M__Court_Comp1 * 4), 3)]//4h work 2h weekend, 2h iwh
        [InlineData("2018-10-14 16:00:00", "2018-10-15 02:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_OverMaxTime_Court_Comp1 * 9 + Price_IWH_Weekend_30M__Court_Comp1 * 16 + Price_IWH_30M__Court_Comp1 * 4), 4)]//10h work, 8h weekend, 2h iwh
        [InlineData("2018-10-14 23:45:00", "2018-10-15 00:15:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 1 + Price_IWH_30M__Court_Comp1 * 1), 3)]//15m work weekend + 15m work weekday
        [InlineData("2018-10-14 23:00:00", "2018-10-15 12:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_OverMaxTime_Court_Comp1 * 15 + Price_IWH_Weekend_30M__Court_Comp1 * 2 + Price_IWH_30M__Court_Comp1 * 14), 4)]//13h work 1h weekend + 12h no weekend
        public void IWH_WeekendPassingMidnightToWeekdayInterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        [InlineData("2018-10-10 22:00:00", "2018-10-11 02:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_4H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 8), 2)]//4h work iwh
        [InlineData("2018-10-10 16:00:00", "2018-10-11 02:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_OverMaxTime_Court_Comp1 * 9 + Price_IWH_30M__Court_Comp1 * 16), 3)]//10h work, 8h iwh 
        [InlineData("2018-10-10 23:45:00", "2018-10-11 00:15:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 1), 2)]//30m work weekday
        [InlineData("2018-10-10 23:00:00", "2018-10-11 12:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_OverMaxTime_Court_Comp1 * 15 + Price_IWH_30M__Court_Comp1 * 16), 3)]//13h work 8h iwh
        public void IWH_WeekdayPassingMidnightToWeekdayInterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        [InlineData("2018-10-12 22:00:00", "2018-10-13 02:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_4H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4 + Price_IWH_30M__Court_Comp1 * 4), 3)]//4h work 2h weekend, 2h iwh
        [InlineData("2018-10-12 16:00:00", "2018-10-13 02:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_OverMaxTime_Court_Comp1 * 9 + Price_IWH_Weekend_30M__Court_Comp1 * 4 + Price_IWH_30M__Court_Comp1 * 12), 4)]//10h work 8h weekday(6h iwh) +2h weekend
        [InlineData("2018-10-12 23:45:00", "2018-10-13 00:15:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 1 + Price_IWH_30M__Court_Comp1 * 1), 3)]//15m work weekend + 15m work weekday
        [InlineData("2018-10-12 23:00:00", "2018-10-13 12:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_OverMaxTime_Court_Comp1 * 15 + Price_IWH_Weekend_30M__Court_Comp1 * 24 + Price_IWH_30M__Court_Comp1 * 2), 4)]//13h work 1h weekday + 12h weekend
        public void IWH_WeekdayPassingMidnightToWeekendInterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        [InlineData("2018-10-10 13:00:00", "2018-10-10 16:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_2_5H_Court_Comp1, 1, "2018-10-10 14:00:00", "2018-10-10 14:30:00")]//3h assignment - 30m mealbreak = 2.5h comp 
        [InlineData("2018-10-10 17:00:00", "2018-10-10 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2_5H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 4), 2, "2018-10-10 17:45:00", "2018-10-10 18:15:00")]//3h assignment - 30m mealbreak = 2.5h comp, 2h iwh-15m iwh mealbreak = 1h45m = 4* 30m
        [InlineData("2018-10-10 17:00:00", "2018-10-10 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2_5H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 3), 2, "2018-10-10 17:45:00", "2018-10-10 18:30:00")]//3h assignment - 45m mealbreak = 2.25h => 2.5h comp, 2h iwh-30m iwh mealbreak = 1h30m = 3* 30m
        [InlineData("2018-10-10 17:00:00", "2018-10-10 21:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_3H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 4), 2, "2018-10-10 19:00:00", "2018-10-10 20:00:00")]//4h assignment -1h mealbreak = 3h comp (3h iwh - 1h mealbreak = 2h iwh time = 4 * 30m)
        [InlineData("2018-10-10 18:00:00", "2018-10-10 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 2), 2, "2018-10-10 18:20:00", "2018-10-10 18:40:00")]//1h work IWH just 20 min pause no change in compensation
        [InlineData("2018-10-10 18:30:00", "2018-10-10 19:50:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 2), 2, "2018-10-10 18:20:00", "2018-10-10 18:40:00")]//1h 20m assignment - 20m mealbreak = 1h comp (1h 20m iwh - 20m mealbreak = 1h iwh time = 2 * 30m)
        [InlineData("2018-10-10 23:45:00", "2018-10-11 01:45:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1_5H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 3), 2, "2018-10-10 23:50:00", "2018-10-11 00:30:00")]//2h assignment - 40m mealbreak = 1-1.5h comp (2h iwh - 40m mealbreak = 1.5h iwh time = 3 * 30m)
        [InlineData("2018-10-10 20:00:00", "2018-10-11 09:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 19 + Price_OverMaxTime_Court_Comp1 * 12), 3, "2018-10-10 23:00:00", "2018-10-11 00:30:00")]//13h assignment - 90m mealbreak = 11.5h 5.5h + 12*overmax comp (11h iwh - 90m mealbreak = 9.5h iwh time = 19 * 30m)
        public void InterpreterCompensationWithOneMealBreak(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows, string startMealbreak1, string endMealbreak1)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                //get a requestRow for broker fee
                List<PriceRowBase> requestPriceRows = new List<PriceRowBase>
                {
                    { GetPriceRowBaseForTest(startAt, endAt, PriceRowType.BrokerFee, (decimal)Broker_Fee_Price_Comp1) }
                };
                List<MealBreak> mealbreaks = new List<MealBreak>
                {
                    new MealBreak { StartAt = DateTime.Parse(startMealbreak1).ToDateTimeOffsetSweden(), EndAt = DateTime.Parse(endMealbreak1).ToDateTimeOffsetSweden() },
                };
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPricesRequisition(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId, out bool userequestrows, null, null, requestPriceRows, null, null, mealbreaks);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }


        [Theory]
        [InlineData("2018-10-10 13:00:00", "2018-10-10 16:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_2_5H_Court_Comp1, 1, "2018-10-10 14:00:00", "2018-10-10 14:30:00", "2018-10-10 15:30:00", "2018-10-10 15:45:00")]//3h assignment - 45m mealbreak = 2.25h => 2.5h comp 
        [InlineData("2018-10-10 16:00:00", "2018-10-10 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_3H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 4), 2, "2018-10-10 16:45:00", "2018-10-10 17:15:00", "2018-10-10 17:45:00", "2018-10-10 18:15:00")]//4h assignment - 1h mealbreak = 3h comp, 2h iwh-15m iwh mealbreak = 1h45m = 4* 30m
        [InlineData("2018-10-10 17:00:00", "2018-10-10 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 3), 2, "2018-10-10 17:45:00", "2018-10-10 18:30:00", "2018-10-10 18:45:00", "2018-10-10 19:10:00")]//3h assignment - 1h10m mealbreak = 1h50m => 2h comp, 2h iwh-55m iwh mealbreak = 1h05m = 3* 30m
        [InlineData("2018-10-10 17:00:00", "2018-10-10 23:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 8), 2, "2018-10-10 19:00:00", "2018-10-10 20:00:00", "2018-10-10 21:45:00", "2018-10-10 22:05:00")]//6h assignment - 1h20m mealbreak = 4h40m 5h comp (5h iwh - 1h 20m mealbreak = 3h 40m iwh time = 8 * 30m)
        [InlineData("2018-10-10 23:00:00", "2018-10-11 03:45:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_4H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 8), 2, "2018-10-10 23:20:00", "2018-10-10 23:40:00", "2018-10-11 01:45:00", "2018-10-11 02:20:00")]//4h45m assignment - 55m mealbreak = 3h50m 4h comp (4h45m iwh - 55m mealbreak = 3h50m iwh time = 8 * 30m)
        [InlineData("2018-10-10 20:00:00", "2018-10-11 09:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 19 + Price_OverMaxTime_Court_Comp1 * 11), 3, "2018-10-10 23:00:00", "2018-10-11 00:30:00", "2018-10-11 06:45:00", "2018-10-11 07:15:00")]//13h assignment - 2h mealbreak = 11h 5.5h + 11*overmax comp (11h iwh - 1h 45m mealbreak = 9.25h iwh time = 19 * 30m)
        public void InterpreterCompensationWithTwoMealBreaks(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows, string startMealbreak1, string endMealbreak1, string startMealbreak2, string endMealbreak2)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                //get a requestRow for broker fee
                List<PriceRowBase> requestPriceRows = new List<PriceRowBase>
                {
                    { GetPriceRowBaseForTest(startAt, endAt, PriceRowType.BrokerFee, (decimal)Broker_Fee_Price_Comp1) }
                };
                List<MealBreak> mealbreaks = new List<MealBreak>
                {
                    new MealBreak { StartAt = DateTime.Parse(startMealbreak1).ToDateTimeOffsetSweden(), EndAt = DateTime.Parse(endMealbreak1).ToDateTimeOffsetSweden() },
                    new MealBreak { StartAt = DateTime.Parse(startMealbreak2).ToDateTimeOffsetSweden(), EndAt = DateTime.Parse(endMealbreak2).ToDateTimeOffsetSweden() },
                };
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPricesRequisition(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId, out bool userequestrows, null, null, requestPriceRows, null, null, mealbreaks);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        //2018-10-12 friday
        //2018-10-13 saturday
        //2018-10-14 sunday
        //2018-10-15 monday
        [Theory]
        [InlineData("2018-10-12 22:00:00", "2018-10-13 02:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_3_5H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4 + Price_IWH_30M__Court_Comp1 * 4), 3, "2018-10-12 23:40:00", "2018-10-13 00:20:00")]//4h assignment - 40m mealbreak = 3.5h comp, normal iwh 2h - 20m = 1h40m 4*30m, weekend iwh 2h - 20m = 1h40m 4*30m
        [InlineData("2018-10-12 22:00:00", "2018-10-13 02:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_3_5H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4 + Price_IWH_30M__Court_Comp1 * 3), 3, "2018-10-12 23:30:00", "2018-10-13 00:10:00")]//4h assignment - 40m mealbreak = 3.5h comp, normal iwh 2h - 30m = 1h30m 3*30m, weekend iwh 2h - 10m = 1h50m 4*30m
        [InlineData("2018-10-13 13:00:00", "2018-10-13 16:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2_5H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 5), 2, "2018-10-13 14:00:00", "2018-10-13 14:30:00")]//3h assignment - 30m mealbreak = 2.5h comp, weekendiwh 3-0,5 = 5*30m
        [InlineData("2018-10-13 17:00:00", "2018-10-13 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2_5H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 5), 2, "2018-10-13 17:45:00", "2018-10-13 18:15:00")]//3h assignment - 30m mealbreak = 2.5h comp, 3h-30m weekend iwh mealbreak = = 5*30m
        [InlineData("2018-10-13 17:00:00", "2018-10-13 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2_5H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 5), 2, "2018-10-13 17:45:00", "2018-10-13 18:30:00")]//3h assignment - 45m mealbreak = 2.25h => 2.5h comp,  2.25h iwh weekend 5*30m
        [InlineData("2018-10-13 17:00:00", "2018-10-13 21:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_3H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 6), 2, "2018-10-13 19:00:00", "2018-10-13 20:00:00")]//4h assignment -1h mealbreak = 3h comp (3h iwh weekend 6 * 30m)
        [InlineData("2018-10-14 18:00:00", "2018-10-14 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 2), 2, "2018-10-14 18:20:00", "2018-10-14 18:40:00")]//1h assignment just 20 min pause no change in compensation
        [InlineData("2018-10-14 18:30:00", "2018-10-14 19:50:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 2), 2, "2018-10-14 18:40:00", "2018-10-14 19:00:00")]//1h 20m assignment - 20m mealbreak = 1h comp (1h 20m iwh - 20m mealbreak = 1h iwh weekend time = 2 * 30m)
        [InlineData("2018-10-14 23:45:00", "2018-10-15 01:45:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1_5H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 1 + Price_IWH_30M__Court_Comp1 * 3), 3, "2018-10-14 23:50:00", "2018-10-15 00:30:00")]//2h assignment - 40m mealbreak = 1-1.5h comp (15m iwh weekend - 5m mealbreak = 10m iwh weekend 1 * 30m) 1h 45m iwh time -30m = 3 * 30m)
        [InlineData("2018-10-13 20:00:00", "2018-10-14 09:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 23 + Price_OverMaxTime_Court_Comp1 * 12), 3, "2018-10-13 23:00:00", "2018-10-14 00:30:00")]//13h assignment - 90m mealbreak = 11.5h 5.5h + 12*overmax comp (13h iwh - 90m mealbreak = 11.5h iwh time = 23 * 30m)
        [InlineData("2018-10-14 20:00:00", "2018-10-15 09:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 6 + Price_OverMaxTime_Court_Comp1 * 12 + Price_IWH_30M__Court_Comp1 * 13), 4, "2018-10-14 23:00:00", "2018-10-15 00:30:00")]//13h assignment - 90m mealbreak = 11.5h 5.5h + 12*overmax comp (4h iwh weekend - 60m mealbreak = 3h iwh time = 6 * 30m, 7h iwh - 30m mealbreak = 6.5h iwh time = 13 * 30m)
        public void InterpreterCompensationWeekendWithOneMealBreak(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows, string startMealbreak1, string endMealbreak1)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                //get a requestRow for broker fee
                List<PriceRowBase> requestPriceRows = new List<PriceRowBase>
                {
                    { GetPriceRowBaseForTest(startAt, endAt, PriceRowType.BrokerFee, (decimal)Broker_Fee_Price_Comp1) }
                };
                List<MealBreak> mealbreaks = new List<MealBreak>
                {
                    new MealBreak { StartAt = DateTime.Parse(startMealbreak1).ToDateTimeOffsetSweden(), EndAt = DateTime.Parse(endMealbreak1).ToDateTimeOffsetSweden() },
                };
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPricesRequisition(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId, out bool userequestrows, null, null, requestPriceRows, null, null, mealbreaks);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        //(2018, 04, 01), DateType = DateType.BigHolidayFullDay},
        //(2018, 04, 02), DateType = DateType.BigHolidayFullDay},
        //(2018, 04, 03), DateType = DateType.DayAfterBigHoliday},
        [Theory]
        [InlineData("2018-04-02 13:00:00", "2018-04-02 16:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2_5H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 5), 2, "2018-04-02 14:00:00", "2018-04-02 14:30:00")]//3h assignment - 30m mealbreak = 2.5h comp, BigHolidayIWH 3-0,5 = 5*30m
        [InlineData("2018-04-02 17:00:00", "2018-04-02 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2_5H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 5), 2, "2018-04-02 17:45:00", "2018-04-02 18:15:00")]//3h assignment - 30m mealbreak = 2.5h comp, 3h-30m BigHolidayIWH mealbreak = = 5*30m
        [InlineData("2018-04-03 14:00:00", "2018-04-03 17:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_2_5H_Court_Comp1, 1, "2018-04-03 13:45:00", "2018-04-03 14:30:00")]//3h assignment - 45m mealbreak = 2.25h => 2.5h comp
        [InlineData("2018-04-02 17:00:00", "2018-04-02 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2_5H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 5), 2, "2018-04-02 17:45:00", "2018-04-02 18:30:00")]//3h assignment - 45m mealbreak = 2.25h => 2.5h comp + 5 * BigHolidayIWH
        [InlineData("2018-04-03 17:00:00", "2018-04-03 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2_5H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 3), 2, "2018-04-03 17:45:00", "2018-04-03 18:30:00")]//3h assignment - 45m mealbreak = 2.25h => 2.5h comp + 2h iwh-30m mealbreak 3 * normal iwh 
        [InlineData("2018-04-01 23:45:00", "2018-04-02 01:45:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1_5H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 3), 2, "2018-04-01 23:50:00", "2018-04-02 00:30:00")]//2h assignment - 40m mealbreak = 1-1.5h comp (3 BigHolidayIWH * 30m)
        [InlineData("2018-04-02 23:45:00", "2018-04-03 01:45:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1_5H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 3), 2, "2018-04-02 23:50:00", "2018-04-03 00:30:00")]//2h assignment - 40m mealbreak = 1-1.5h comp (3 BigHolidayIWH * 30m)
        [InlineData("2018-04-02 20:00:00", "2018-04-03 09:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 19 + Price_OverMaxTime_Court_Comp1 * 12), 3, "2018-04-02 23:00:00", "2018-04-03 00:30:00")]//13h assignment - 90m mealbreak = 11.5h 5.5h + 12*overmax comp (11h iwhBH - 90m mealbreak = 9.5h BigHolidayIWH time = 19 * 30m)
        public void InterpreterCompensationBigHolidayWithOneMealBreak(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows, string startMealbreak1, string endMealbreak1)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                //get a requestRow for broker fee
                List<PriceRowBase> requestPriceRows = new List<PriceRowBase>
                {
                    { GetPriceRowBaseForTest(startAt, endAt, PriceRowType.BrokerFee, (decimal)Broker_Fee_Price_Comp1) }
                };
                List<MealBreak> mealbreaks = new List<MealBreak>
                {
                    new MealBreak { StartAt = DateTime.Parse(startMealbreak1).ToDateTimeOffsetSweden(), EndAt = DateTime.Parse(endMealbreak1).ToDateTimeOffsetSweden() },
                };
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPricesRequisition(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId, out bool userequestrows, null, null, requestPriceRows, null, null, mealbreaks);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        //2018-10-13 saturday
        //2018-10-14 sunday
        //2018-10-15 monday
        [Theory]
        [InlineData("2018-10-13 13:00:00", "2018-10-13 16:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_3H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 6), 2, "2018-10-13 14:00:00", "2018-10-13 14:10:00", "2018-10-13 14:20:00", "2018-10-13 14:30:00")]//3h assignment - 20m mealbreak = 3h comp, weekendiwh 2h 40m = 6*30m
        [InlineData("2018-10-13 17:00:00", "2018-10-13 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2_5H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 5), 2, "2018-10-13 17:45:00", "2018-10-13 18:10:00", "2018-10-13 19:45:00", "2018-10-13 19:55:00")]//3h assignment - 35m mealbreak = 2.5h comp, 3h-35m weekend iwh mealbreak = = 5*30m
        [InlineData("2018-10-13 17:00:00", "2018-10-13 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1_5H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 3), 2, "2018-10-13 17:45:00", "2018-10-13 18:30:00", "2018-10-13 19:10:00", "2018-10-13 19:55:00")]//3h assignment - 90m mealbreak = 1.5h => 1.5h comp,  3h -90m =1.5h iwh weekend 3*30m
        [InlineData("2018-10-13 17:00:00", "2018-10-13 21:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1_5H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 3), 2, "2018-10-13 18:00:00", "2018-10-13 20:00:00", "2018-10-13 20:15:00", "2018-10-13 20:45:00")]//4h assignment -2.5h mealbreak = 1.5h comp (1.5h iwh weekend 3 * 30m)
        [InlineData("2018-10-14 18:00:00", "2018-10-14 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 2), 2, "2018-10-14 18:20:00", "2018-10-14 18:25:00", "2018-10-14 18:40:00", "2018-10-14 18:45:00")]//1h assignment just 10 min pause no change in compensation
        [InlineData("2018-10-14 18:30:00", "2018-10-14 19:50:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 2), 2, "2018-10-14 18:40:00", "2018-10-14 18:45:00", "2018-10-14 19:10:00", "2018-10-14 19:40:00")]//1h 20m assignment - 35m mealbreak = 1h comp (1h 20m iwh - 35m mealbreak = 45m iwh weekend time = 2 * 30m)
        [InlineData("2018-10-14 23:45:00", "2018-10-15 01:45:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1_5H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 1 + Price_IWH_30M__Court_Comp1 * 3), 3, "2018-10-14 23:50:00", "2018-10-15 00:30:00", "2018-10-15 00:45:00", "2018-10-15 00:55:00")]//2h assignment - 50m mealbreak = 1-1.5h comp (15m iwh weekend - 5m mealbreak = 10m iwh weekend 1 * 30m) 1h 45m -40miwh time = 3 * 30m)
        [InlineData("2018-10-13 20:00:00", "2018-10-14 09:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 22 + Price_OverMaxTime_Court_Comp1 * 11), 3, "2018-10-13 23:00:00", "2018-10-14 00:30:00", "2018-10-14 06:30:00", "2018-10-14 07:15:00")]//13h assignment - 2h 15m mealbreak = 10.75h 5.5h + 11*overmax comp (13h iwh - 2h 15m mealbreak = 10.75h iwh time = 22 * 30m)
        [InlineData("2018-10-14 20:00:00", "2018-10-15 09:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 6 + Price_OverMaxTime_Court_Comp1 * 11 + Price_IWH_30M__Court_Comp1 * 13), 4, "2018-10-14 23:00:00", "2018-10-15 00:30:00", "2018-10-15 06:45:00", "2018-10-15 07:15:00")]//13h assignment - 2h mealbreak = 11h 5.5h + 11*overmax comp (4h iwh weekend - 60m mealbreak = 3h iwh time = 6 * 30m, 7h iwh - 30+15m mealbreak = 6:25h iwh time = 13 * 30m)
        public void InterpreterCompensationWeekendWithTwoMealBreaks(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows, string startMealbreak1, string endMealbreak1, string startMealbreak2, string endMealbreak2)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                //get a requestRow for broker fee
                List<PriceRowBase> requestPriceRows = new List<PriceRowBase>
                {
                    { GetPriceRowBaseForTest(startAt, endAt, PriceRowType.BrokerFee, (decimal)Broker_Fee_Price_Comp1) }
                };
                List<MealBreak> mealbreaks = new List<MealBreak>
                {
                    new MealBreak { StartAt = DateTime.Parse(startMealbreak1).ToDateTimeOffsetSweden(), EndAt = DateTime.Parse(endMealbreak1).ToDateTimeOffsetSweden() },
                    new MealBreak { StartAt = DateTime.Parse(startMealbreak2).ToDateTimeOffsetSweden(), EndAt = DateTime.Parse(endMealbreak2).ToDateTimeOffsetSweden() },
                };
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPricesRequisition(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId, out bool userequestrows, null, null, requestPriceRows, null, null, mealbreaks);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        //(2018, 03, 29), DateType = DateType.DayBeforeBigHoliday},
        //(2018, 03, 30), DateType = DateType.BigHolidayFullDay},
        //(2018, 04, 02), DateType = DateType.BigHolidayFullDay},
        [Theory]
        [InlineData("2018-04-02 13:00:00", "2018-04-02 16:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2_5H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 5), 2, "2018-04-02 14:00:00", "2018-04-02 14:30:00", "2018-04-02 15:00:00", "2018-04-02 15:10:00")]//3h assignment - 40m mealbreak = 2.5h comp, BigHolidayIWH 3-40m = 5*30m
        [InlineData("2018-04-02 17:00:00", "2018-04-02 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2_5H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 5), 2, "2018-04-02 17:45:00", "2018-04-02 18:00:00", "2018-04-02 19:00:00", "2018-04-02 19:15:00")]//3h assignment - 30m mealbreak = 2.5h comp, 3h-30m BigHolidayIWH mealbreak = = 5*30m
        [InlineData("2018-03-29 14:00:00", "2018-03-29 17:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_2H_Court_Comp1, 1, "2018-03-29 13:45:00", "2018-03-29 14:30:00", "2018-03-29 15:00:00", "2018-03-29 15:30:00")]//3h assignment - 1h mealbreak = 2h => 2h comp
        [InlineData("2018-03-29 17:00:00", "2018-03-29 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2_5H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 3), 2, "2018-03-29 17:45:00", "2018-03-29 18:30:00", "2018-03-29 19:15:00", "2018-03-29 19:20:00")]//3h assignment - 50m mealbreak = 2.25h => 2.5h -35m = 1h 25m BigHolidayIWH comp = 3 * BigHolidayIWH
        [InlineData("2018-03-29 04:00:00", "2018-03-29 09:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_4_5H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 5), 2, "2018-03-29 04:45:00", "2018-03-29 05:15:00", "2018-03-29 07:50:00", "2018-03-29 08:10:00")]//5h assignment - 50m mealbreak = 4.25h => 4.5h 3h-30m = 2.5h Normal IWH comp = 5*30m
        [InlineData("2018-03-29 23:45:00", "2018-03-30 01:45:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 2), 2, "2018-03-29 23:50:00", "2018-03-30 00:30:00", "2018-03-30 00:50:00", "2018-03-30 01:10:00")]//2h assignment - 60m mealbreak = 1h comp (2 BigHolidayIWH * 30m)
        [InlineData("2018-03-29 20:00:00", "2018-03-30 09:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 23 + Price_OverMaxTime_Court_Comp1 * 12), 3, "2018-03-29 23:50:00", "2018-03-30 00:30:00", "2018-03-30 06:50:00", "2018-03-30 08:05:00")]//13h assignment - 1h55m mealbreak 11h5m  12*overmax comp, 11h 05m BHIWH = 23 * 30m
        [InlineData("2018-04-02 20:00:00", "2018-04-03 09:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5_5H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 19 + Price_OverMaxTime_Court_Comp1 * 11), 3, "2018-04-02 23:00:00", "2018-04-03 00:30:00", "2018-04-03 06:40:00", "2018-04-03 07:30:00")]//13h assignment - 2h20m mealbreak = 10h 40m   11*overmax comp (11h iwhBH - 1h50m mealbreak = 9h 10m BigHolidayIWH time = 19 * 30m)
        public void InterpreterCompensationBigHolidayWithTwoMealBreaks(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows, string startMealbreak1, string endMealbreak1, string startMealbreak2, string endMealbreak2)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                //get a requestRow for broker fee
                List<PriceRowBase> requestPriceRows = new List<PriceRowBase>
                {
                    { GetPriceRowBaseForTest(startAt, endAt, PriceRowType.BrokerFee, (decimal)Broker_Fee_Price_Comp1) }
                };
                List<MealBreak> mealbreaks = new List<MealBreak>
                {
                    new MealBreak { StartAt = DateTime.Parse(startMealbreak1).ToDateTimeOffsetSweden(), EndAt = DateTime.Parse(endMealbreak1).ToDateTimeOffsetSweden() },
                    new MealBreak { StartAt = DateTime.Parse(startMealbreak2).ToDateTimeOffsetSweden(), EndAt = DateTime.Parse(endMealbreak2).ToDateTimeOffsetSweden() },
                };
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPricesRequisition(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId, out bool userequestrows, null, null, requestPriceRows, null, null, mealbreaks);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        //Holiday and AfterBigHoliday coincide
        [Theory]
        [InlineData("2022-06-06 06:00:00", "2022-06-06 08:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_2H_Court_Comp1 + (Price_IWH_Weekend_30M__Court_Comp1 * 2) + (Price_IWH_BigHoliday_30M__Court_Comp1 * 2), 3)]//2h work 1h big holiday, 1h holiday day + Price_IWH_BigHoliday_30M__Court_Comp1 * 2
        [InlineData("2022-06-06 10:00:00", "2022-06-06 14:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_4H_Court_Comp1 + (Price_IWH_Weekend_30M__Court_Comp1 * 8), 2)]//4h work holiday 
        [InlineData("2022-06-06 17:00:00", "2022-06-06 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_2H_Court_Comp1 + (Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work holiday
        public void IWH_AfterBigHoliday_And_Holiday_InterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        //Holiday and BeforeBigHoliday coincide
        [Theory]
        [InlineData("2025-06-06 06:00:00", "2025-06-06 08:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_2H_Court_Comp1 + (Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work holiday
        [InlineData("2025-06-06 10:00:00", "2025-06-06 14:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_4H_Court_Comp1 + (Price_IWH_Weekend_30M__Court_Comp1 * 8), 2)]//4h work holiday 
        [InlineData("2025-06-06 17:00:00", "2025-06-06 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_2H_Court_Comp1 + (Price_IWH_Weekend_30M__Court_Comp1 * 2) + (Price_IWH_BigHoliday_30M__Court_Comp1 * 2), 3)]//2h work 1h big holiday, 1h holiday day + Price_IWH_BigHoliday_30M__Court_Comp1 * 2
        public void IWH_BeforeBigHoliday_And_Holiday_InterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        [InlineData("2018-10-10 12:00:00", "2018-10-10 18:00:00", "2018-10-10 15:00:00", "2018-10-10 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, "", "", false)]//no mealbreak, the requisition times should get more (4 *30m iwh)
        [InlineData("2018-10-10 12:00:00", "2018-10-10 18:00:00", "2018-10-10 15:00:00", "2018-10-10 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, "2018-10-10 16:00:00", "2018-10-10 16:30:00", false)]//30m mealbreak non iwh times => requisition times get more (4* 30m iwh) 
        [InlineData("2018-10-10 12:00:00", "2018-10-10 18:00:00", "2018-10-10 15:00:00", "2018-10-10 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, "2018-10-10 16:00:00", "2018-10-10 17:00:00", false)]//1h mealbreak non iwh times => requisition times get more (4* 30m iwh) 
        [InlineData("2018-10-10 12:00:00", "2018-10-10 18:00:00", "2018-10-10 15:00:00", "2018-10-10 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, "2018-10-10 18:00:00", "2018-10-10 18:30:00", true)]//30m mealbreak iwh times reduces iwh time with 1*30m => request is better payed
        [InlineData("2018-10-10 12:00:00", "2018-10-10 18:00:00", "2018-10-10 15:00:00", "2018-10-10 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, "2018-10-10 18:00:00", "2018-10-10 19:00:00", true)]//1h mealbreak iwh times reduces iwh time with 2* 30m => request is better payed
        [InlineData("2018-10-10 12:00:00", "2018-10-10 18:00:00", "2018-10-10 15:00:00", "2018-10-10 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, "2018-10-10 17:30:00", "2018-10-10 19:30:00", true)]//2h mealbreak 90m iwh times reduces iwh time with 3* 30m => request is better payed
        public void UseRequestPricerowsWithMealbreaks(string requestStartAt, string requestEndAt, string requisitionStartAt, string requisitionEndAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, string startMealbreak1, string endMealbreak1, bool useRequestRows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                //get a requestRow for broker fee
                List<PriceRowBase> requestPriceRows = new List<PriceRowBase>
                {
                    { GetPriceRowBaseForTest(requestStartAt, requestEndAt, PriceRowType.BrokerFee, (decimal)Broker_Fee_Price_Comp1) },
                    { GetPriceRowBaseForTest(requestStartAt, requestEndAt, PriceRowType.InterpreterCompensation, (decimal)Price_2H_Court_Comp1) }
                };
                List<MealBreak> mealbreaks = new List<MealBreak>();
                if (!string.IsNullOrEmpty(startMealbreak1))
                {
                    mealbreaks.Add(new MealBreak { StartAt = DateTime.Parse(startMealbreak1).ToDateTimeOffsetSweden(), EndAt = DateTime.Parse(endMealbreak1).ToDateTimeOffsetSweden() });
                }
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPricesRequisition(DateTime.Parse(requisitionStartAt).ToDateTimeOffsetSweden(), DateTime.Parse(requisitionEndAt).ToDateTimeOffsetSweden(), DateTime.Parse(requestStartAt).ToDateTimeOffsetSweden(), DateTime.Parse(requestEndAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId, out bool useRequestRowsToCompare, null, null, requestPriceRows, null, null, mealbreaks);
                useRequestRowsToCompare.Should().Be(useRequestRows, "useRequestRows should be {0}", useRequestRows);
            }
        }

        [Theory]
        [InlineData("2018-10-13 10:00:00", "2018-10-13 11:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 2), 2)]//1h work IWH weekend
        [InlineData("2018-10-13 10:00:00", "2018-10-13 15:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 10), 2)]//5h work IWH weekend
        [InlineData("2018-10-13 06:00:00", "2018-10-13 08:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 2)]//2h work IWH weekend
        [InlineData("2018-10-13 17:00:00", "2018-10-13 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_3H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 6), 2)]//3h work IWH weekend
        public void IWH_Weekend_InterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        //23/12 is weekday 
        [Theory]
        [InlineData("2019-12-23 05:00:00", "2019-12-23 08:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_3H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 4), 2)]//3h work, 2h before 07:00 normal iwh
        [InlineData("2019-12-23 10:00:00", "2019-12-23 15:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_5H_Court_Comp1, 1)]//5h normal work
        [InlineData("2019-12-23 17:00:00", "2019-12-23 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_3H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 4), 2)]//3h work, 2h IWH big holiday
        public void IWH_DayBeforeBigHoliday_InterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        //23/12 is weekend saturday or sunday
        [Theory]
        [InlineData("2018-12-23 05:00:00", "2018-12-23 08:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_3H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 6), 2)]//3h work weekend
        [InlineData("2018-12-23 10:00:00", "2018-12-23 15:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 10), 2)]//5h work weekend
        [InlineData("2018-12-23 17:00:00", "2018-12-23 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_3H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 2 + Price_IWH_BigHoliday_30M__Court_Comp1 * 4), 3)]//3h work, 1h normal weekend iwh IWH 2h big holiday
        public void IWH_DayBeforeBigHolidayWeekend_InterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        [InlineData("2018-12-24 05:00:00", "2018-12-24 09:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_4H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 8), 2)]//4h work IWH big holiday
        [InlineData("2018-12-24 10:00:00", "2018-12-24 11:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 2), 2)]//1h work IWH big holiday
        [InlineData("2018-12-24 10:00:00", "2018-12-24 15:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 10), 2)]//5h work IWH big holiday
        [InlineData("2018-12-24 17:00:00", "2018-12-24 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_3H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 6), 2)]//3h work IWH big holiday
        public void IWH_BigHoliday_InterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        //27/12 is weekday
        [Theory]
        [InlineData("2018-12-27 06:00:00", "2018-12-27 09:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_3H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 2), 2)]//3h work day after big holiday (1hour before 07:00 counts as big holiday)
        [InlineData("2018-12-27 10:00:00", "2018-12-27 15:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_5H_Court_Comp1, 1)]//5h work day after big holiday (should be no extra comp)
        [InlineData("2018-12-27 17:00:00", "2018-12-27 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_3H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 4, 2)]//3h work, 2h normal iwh 
        public void IWH_DayAfterBigHoliday_InterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        //27/12 is weekend saturday or sunday
        [Theory]
        [InlineData("2020-12-27 06:00:00", "2020-12-27 09:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_3H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 2 + Price_IWH_Weekend_30M__Court_Comp1 * 4), 3)]//3h work day after big holiday (1h before 07:00 counts as big holiday, 2h weekend iwh)
        [InlineData("2020-12-27 10:00:00", "2020-12-27 15:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_5H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 10, 2)]//5h work 5h weekend iwh
        [InlineData("2020-12-27 17:00:00", "2020-12-27 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_3H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 6, 2)]//5h work day after big holiday (should be no extra time)
        public void IWH_DayAfterBigHolidayWeekend_InterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        //all broker fees should be calculated from baseprice and PriceListType.Court, complevel 1 = 352 * 0,1 = 35 Rounded price (constant)
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Broker_Fee_Price_Comp1, 1)]//1h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Broker_Fee_Price_Comp1, 1)]//2h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 13:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Broker_Fee_Price_Comp1, 1)]//3h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 14:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Broker_Fee_Price_Comp1, 1)]//4h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 15:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Broker_Fee_Price_Comp1, 1)]//5h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 16:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Broker_Fee_Price_Comp1, 1)]//6h nwt (extra comp. for > 5,5h)
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Other, CompetenceLevel.OtherInterpreter, DefaultRankingId, Broker_Fee_Price_Comp1, 1)]//1h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", PriceListType.Other, CompetenceLevel.OtherInterpreter, DefaultRankingId, Broker_Fee_Price_Comp1, 1)]//2h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 13:00:00", PriceListType.Other, CompetenceLevel.OtherInterpreter, DefaultRankingId, Broker_Fee_Price_Comp1, 1)]//3h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 14:00:00", PriceListType.Other, CompetenceLevel.OtherInterpreter, DefaultRankingId, Broker_Fee_Price_Comp1, 1)]//4h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 15:00:00", PriceListType.Other, CompetenceLevel.OtherInterpreter, DefaultRankingId, Broker_Fee_Price_Comp1, 1)]//5h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 16:00:00", PriceListType.Other, CompetenceLevel.OtherInterpreter, DefaultRankingId, Broker_Fee_Price_Comp1, 1)]//6h nwt (extra comp. for > 5,5h)
        //all broker fees should be calculated from baseprice and PriceListType.Court, complevel 2 = 409 * 0,1 = 41 Rounded price (constant)
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Court, CompetenceLevel.EducatedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp2, 1)]//1h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", PriceListType.Court, CompetenceLevel.EducatedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp2, 1)]//2h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 13:00:00", PriceListType.Court, CompetenceLevel.EducatedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp2, 1)]//3h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 14:00:00", PriceListType.Court, CompetenceLevel.EducatedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp2, 1)]//4h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 15:00:00", PriceListType.Court, CompetenceLevel.EducatedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp2, 1)]//5h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 16:00:00", PriceListType.Court, CompetenceLevel.EducatedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp2, 1)]//6h nwt (extra comp. for > 5,5h)
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Other, CompetenceLevel.EducatedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp2, 1)]//1h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", PriceListType.Other, CompetenceLevel.EducatedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp2, 1)]//2h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 13:00:00", PriceListType.Other, CompetenceLevel.EducatedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp2, 1)]//3h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 14:00:00", PriceListType.Other, CompetenceLevel.EducatedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp2, 1)]//4h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 15:00:00", PriceListType.Other, CompetenceLevel.EducatedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp2, 1)]//5h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 16:00:00", PriceListType.Other, CompetenceLevel.EducatedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp2, 1)]//6h nwt (extra comp. for > 5,5h)
        //all broker fees should be calculated from baseprice and PriceListType.Court, complevel 3 = 480 * 0,1 = 48 Rounded price (constant)
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Court, CompetenceLevel.AuthorizedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp3, 1)]//1h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", PriceListType.Court, CompetenceLevel.AuthorizedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp3, 1)]//2h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 13:00:00", PriceListType.Court, CompetenceLevel.AuthorizedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp3, 1)]//3h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 14:00:00", PriceListType.Court, CompetenceLevel.AuthorizedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp3, 1)]//4h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 15:00:00", PriceListType.Court, CompetenceLevel.AuthorizedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp3, 1)]//5h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 16:00:00", PriceListType.Court, CompetenceLevel.AuthorizedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp3, 1)]//6h nwt (extra comp. for > 5,5h)
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Other, CompetenceLevel.AuthorizedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp3, 1)]//1h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", PriceListType.Other, CompetenceLevel.AuthorizedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp3, 1)]//2h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 13:00:00", PriceListType.Other, CompetenceLevel.AuthorizedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp3, 1)]//3h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 14:00:00", PriceListType.Other, CompetenceLevel.AuthorizedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp3, 1)]//4h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 15:00:00", PriceListType.Other, CompetenceLevel.AuthorizedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp3, 1)]//5h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 16:00:00", PriceListType.Other, CompetenceLevel.AuthorizedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp3, 1)]//6h nwt (extra comp. for > 5,5h)
        //all broker fees should be calculated from baseprice and PriceListType.Court, complevel 2 = 606 * 0,1 = 61 Rounded price (constant)
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Court, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp4, 1)]//1h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", PriceListType.Court, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp4, 1)]//2h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 13:00:00", PriceListType.Court, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp4, 1)]//3h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 14:00:00", PriceListType.Court, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp4, 1)]//4h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 15:00:00", PriceListType.Court, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp4, 1)]//5h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 16:00:00", PriceListType.Court, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp4, 1)]//6h nwt (extra comp. for > 5,5h)
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Other, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp4, 1)]//1h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", PriceListType.Other, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp4, 1)]//2h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 13:00:00", PriceListType.Other, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp4, 1)]//3h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 14:00:00", PriceListType.Other, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp4, 1)]//4h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 15:00:00", PriceListType.Other, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp4, 1)]//5h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 16:00:00", PriceListType.Other, CompetenceLevel.SpecializedInterpreter, DefaultRankingId, Broker_Fee_Price_Comp4, 1)]//6h nwt (extra comp. for > 5,5h)
        //double broker fee
        [InlineData("2018-10-10 23:00:00", "2018-10-11 01:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Broker_Fee_Price_Comp1 * 2, 1)]
        public void BrokerFeePriceRow(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.BrokerFee).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.BrokerFee).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1H_Court_Comp1 * SocialInsuranceCharge / 100), 1)]//1h nwt, complevel 1
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 * SocialInsuranceCharge / 100), 1)]//2h nwt comp.Level 1
        [InlineData("2018-10-10 10:00:00", "2018-10-10 13:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_3H_Court_Comp1 * SocialInsuranceCharge / 100), 1)]//3h nwt comp.Level 1
        [InlineData("2018-10-10 10:00:00", "2018-10-10 14:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_4H_Court_Comp1 * SocialInsuranceCharge / 100), 1)]//4h nwt comp.Level 1
        [InlineData("2018-10-10 10:00:00", "2018-10-10 15:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5H_Court_Comp1 * SocialInsuranceCharge / 100), 1)]//5h nwt comp.Level 1
        [InlineData("2018-10-10 10:00:00", "2018-10-10 16:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, ((Price_5_5H_Court_Comp1 + Price_OverMaxTime_Court_Comp1) * SocialInsuranceCharge / 100), 1)]//6h nwt comp.Level 1 (extra comp. for > 5,5h)
        public void SocialInsurancePriceRow_GetPrices(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.SocialInsuranceCharge).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.SocialInsuranceCharge).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", Price_1H_Court_Comp1, (Price_1H_Court_Comp1 * SocialInsuranceCharge / 100))]//1h nwt, complevel 1
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", Price_2H_Court_Comp1, (Price_2H_Court_Comp1 * SocialInsuranceCharge / 100))]//2h nwt comp.Level 1
        [InlineData("2018-10-10 10:00:00", "2018-10-10 13:00:00", Price_3H_Court_Comp1, (Price_3H_Court_Comp1 * SocialInsuranceCharge / 100))]//3h nwt comp.Level 1
        [InlineData("2018-10-10 10:00:00", "2018-10-10 14:00:00", Price_4H_Court_Comp1, (Price_4H_Court_Comp1 * SocialInsuranceCharge / 100))]//4h nwt comp.Level 1
        [InlineData("2018-10-10 10:00:00", "2018-10-10 15:00:00", Price_5H_Court_Comp1, (Price_5H_Court_Comp1 * SocialInsuranceCharge / 100))]//5h nwt comp.Level 1
        public void SocialInsurancePriceRow(string startAt, string endAt, decimal interpreterPrice, decimal actualPrice)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                List<PriceRowBase> priceRows = new List<PriceRowBase>
                {
                    { GetPriceRowBaseForTest(startAt, endAt, PriceRowType.InterpreterCompensation, interpreterPrice) }
                };
                PriceRowBase pr = new PriceCalculationService(tolkdbContext).GetPriceRowSocialInsuranceCharge(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), priceRows);
                pr.TotalPrice.Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pr.PriceRowType.Should().Be(PriceRowType.SocialInsuranceCharge, "price row type {0}", PriceRowType.SocialInsuranceCharge.GetDescription());
            }
        }

        [Fact]
        public void SocialInsurancePriceRow_InvalidOperationException()
        {
            using (var tolkDbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                var priceCalculationCharge = new PriceCalculationCharge { StartDate = new DateTime(2018, 01, 01), EndDate = new DateTime(2098, 01, 01), PriceCalculationChargeId = 4, ChargePercentage = (decimal)SocialInsuranceCharge, ChargeTypeId = ChargeType.SocialInsuranceCharge };
                //add extra row for SocialInsuranceCharge with date overlapping => should throw exception 
                tolkDbContext.PriceCalculationCharges.Add(priceCalculationCharge);
                tolkDbContext.SaveChanges();
                Action a = () => new PriceCalculationService(tolkDbContext).GetPriceRowSocialInsuranceCharge(DateTime.Parse(DefaultStartDate), DateTime.Parse(DefaultEndDate), new List<PriceRowBase> { InterpreterCompensationPriceRow });
                a.Should().Throw<InvalidOperationException>();
                tolkDbContext.PriceCalculationCharges.Remove(priceCalculationCharge);
                tolkDbContext.SaveChanges();
            }
        }

        [Fact]
        public void AdministrativePriceRow_InvalidOperationException()
        {
            using (var tolkDbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                var priceCalculationCharge = new PriceCalculationCharge { StartDate = new DateTime(2018, 01, 01), EndDate = new DateTime(2098, 01, 01), PriceCalculationChargeId = 3, ChargePercentage = (decimal)AdministrativeCharge, ChargeTypeId = ChargeType.AdministrativeCharge };

                //add extra row for AdministrativeCharge with date overlapping => should throw exception 
                tolkDbContext.PriceCalculationCharges.Add(priceCalculationCharge);
                tolkDbContext.SaveChanges();
                Action a = () => new PriceCalculationService(tolkDbContext).GetPriceRowAdministrativeCharge(DateTime.Parse(DefaultStartDate), DateTime.Parse(DefaultEndDate), new List<PriceRowBase> { InterpreterCompensationPriceRow });
                a.Should().Throw<InvalidOperationException>();
                tolkDbContext.PriceCalculationCharges.Remove(priceCalculationCharge);
                tolkDbContext.SaveChanges();
            }
        }

        [Theory]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, 300, 1)]//1h nwt, complevel 1
        public void TravelCostRow(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, bool useRequestRows, decimal actualOutlay, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                //get a requestRow for broker fee
                List<PriceRowBase> requestPriceRows = new List<PriceRowBase>
                {
                    { GetPriceRowBaseForTest(startAt, endAt, PriceRowType.BrokerFee, (decimal)Broker_Fee_Price_Comp1) }
                };
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPricesRequisition(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId, out bool useRequestRowsToCompare, null, null, requestPriceRows, actualOutlay, null);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.Outlay).Sum(pr => pr.TotalPrice).Should().Be(actualOutlay, "total price should be {0}", actualOutlay);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.Outlay).Should().Be(noOfrows, "number of rows {0}", noOfrows);
                useRequestRowsToCompare.Should().Be(useRequestRows, "cause useRequestRows should be {0}", useRequestRows);
            }
        }

        [Theory]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, Price_LostTime_60M__Court_Comp1, 31, 0, 1)]//31m nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, Price_LostTime_60M__Court_Comp1, 60, 0, 1)]//1h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, Price_LostTime_60M__Court_Comp1 * 2, 90, 0, 1)]//90m nwt
        [InlineData("2018-10-10 18:00:00", "2018-10-10 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, Price_LostTime_60M__Court_Comp1 + Price_IWH_LostTime_30M__Court_Comp1, 31, 30, 2)]//30m iwh
        [InlineData("2018-10-10 18:00:00", "2018-10-10 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, (Price_LostTime_60M__Court_Comp1 + (Price_IWH_LostTime_30M__Court_Comp1 * 2)), 60, 60, 2)]//60m iwh
        [InlineData("2018-10-10 18:00:00", "2018-10-10 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, ((Price_LostTime_60M__Court_Comp1 * 2) + (Price_IWH_LostTime_30M__Court_Comp1 * 3)), 90, 90, 2)]//90m iwh
        [InlineData("2018-10-10 18:00:00", "2018-10-10 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, Price_LostTime_60M__Court_Comp1 + Price_IWH_LostTime_30M__Court_Comp1, 31, 15, 2)]//31m nwt 15m iwh
        [InlineData("2018-10-10 18:00:00", "2018-10-10 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, ((Price_LostTime_60M__Court_Comp1 * 2) + Price_IWH_LostTime_30M__Court_Comp1), 90, 20, 2)]//90m nwt 20m iwh
        [InlineData("2018-10-10 18:00:00", "2018-10-10 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, ((Price_LostTime_60M__Court_Comp1 * 2) + (Price_IWH_LostTime_30M__Court_Comp1 * 2)), 90, 40, 2)]//90m 40iwh
        public void LostTimeRows(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, bool useRequestRows, decimal actualPrice, int lostTime, int iwhLostTime, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                //get a requestRow for broker fee
                List<PriceRowBase> requestPriceRows = new List<PriceRowBase>
                {
                    { GetPriceRowBaseForTest(startAt, endAt, PriceRowType.BrokerFee, (decimal)Broker_Fee_Price_Comp1) }
                };
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPricesRequisition(DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), DateTime.Parse(startAt).ToDateTimeOffsetSweden(), DateTime.Parse(endAt).ToDateTimeOffsetSweden(), competenceLevel, listType, rankingId, out bool useRequestRowsToCompare, lostTime, iwhLostTime, requestPriceRows, actualPrice, null);
                pi.PriceRows.Where(pr => pr.PriceListRow != null && (pr.PriceListRow.PriceListRowType == PriceListRowType.LostTime || pr.PriceListRow.PriceListRowType == PriceListRowType.LostTimeIWH)).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceListRow != null && (pr.PriceListRow.PriceListRowType == PriceListRowType.LostTime || pr.PriceListRow.PriceListRowType == PriceListRowType.LostTimeIWH)).Should().Be(noOfrows, "number of rows {0}", noOfrows);
                useRequestRowsToCompare.Should().Be(useRequestRows, "cause useRequestRows should be {0}", useRequestRows);
            }
        }

        [Theory]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", "2018-10-10 10:00:00", "2018-10-10 12:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", "2018-10-10 10:00:00", "2018-10-10 12:20:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", "2018-10-10 10:00:00", "2018-10-10 11:45:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false)]//Time is less but still 1,5-2h tax
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", "2018-10-10 10:00:00", "2018-10-10 11:29:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, true)]
        public void UseRequestPricerows(string requestStartAt, string requestEndAt, string requisitionStartAt, string requisitionEndAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, bool useRequestRows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                //get a requestRow for broker fee
                List<PriceRowBase> requestPriceRows = new List<PriceRowBase>
                {
                    { GetPriceRowBaseForTest(requestStartAt, requestEndAt, PriceRowType.BrokerFee, (decimal)Broker_Fee_Price_Comp1) },
                    { GetPriceRowBaseForTest(requestStartAt, requestEndAt, PriceRowType.InterpreterCompensation, (decimal)Price_2H_Court_Comp1) }
                };
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPricesRequisition(DateTime.Parse(requisitionStartAt), DateTime.Parse(requisitionEndAt), DateTime.Parse(requestStartAt), DateTime.Parse(requestEndAt), competenceLevel, listType, rankingId, out bool useRequestRowsToCompare, null, null, requestPriceRows, null, null);
                useRequestRowsToCompare.Should().Be(useRequestRows, "useRequestRows should be {0}", useRequestRows);
            }
        }

        [Theory]
        [InlineData("2018-10-10 23:00:00", "2018-10-11 00:00:00", "2018-10-11 01:00:00", "2018-10-11 00:00:00", "2018-10-11 01:00:00", "2018-10-11 02:00:00", 2, 2, 2, 1000)]
        [InlineData("2018-10-10 23:15:00", "2018-10-11 00:00:00", "2018-10-11 01:00:00", "2018-10-11 00:00:00", "2018-10-11 01:00:00", "2018-10-11 01:45:00", 1.5, 2, 1.5, 1000)]
        public void MergePriceListRowsOfSameType(string start1, string start2, string start3, string end1, string end2, string end3, decimal quant1, decimal quant2, decimal quant3, decimal price)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                int totalQuantity = (int)(quant1 + quant2 + quant3);
                decimal totalPrice = price * totalQuantity;
                DateTimeOffset minStartAt = new List<DateTime> { DateTime.Parse(start1), DateTime.Parse(start2), DateTime.Parse(start3) }.Min();
                DateTimeOffset maxEndAt = new List<DateTime> { DateTime.Parse(end1), DateTime.Parse(end2), DateTime.Parse(end3) }.Max();

                //decimal.Round(Price, 2, MidpointRounding.AwayFromZero)
                //generate rows
                List<PriceRowBase> priceRows = new List<PriceRowBase>
                {
                    { GetPriceRowWithPriceListRowForTest(start1, end1, price, (int)decimal.Round(quant1, MidpointRounding.AwayFromZero), 1101) },
                    { GetPriceRowWithPriceListRowForTest(start2, end2,  price, (int)decimal.Round(quant2, MidpointRounding.AwayFromZero), 1101) },
                    { GetPriceRowWithPriceListRowForTest(start3, end3, price, (int)decimal.Round(quant3, MidpointRounding.AwayFromZero), 1101) },
                };
                IEnumerable<PriceRowBase> mergedPriceRows = new PriceCalculationService(tolkdbContext).MergePriceListRowsAndReduceForMealBreak(priceRows);
                mergedPriceRows.Count().Should().Be(1);
                mergedPriceRows.First().StartAt.Should().Be(minStartAt);
                mergedPriceRows.First().EndAt.Should().Be(maxEndAt);
                mergedPriceRows.Sum(pr => pr.TotalPrice).Should().Be(totalPrice);
                mergedPriceRows.Sum(pr => pr.Quantity).Should().Be(totalQuantity);
            }
        }

        private PriceRowBase GetPriceRowBaseForTest(string startAt, string endAt, PriceRowType priceRowType, decimal price)
        {
            return new PriceRowBase { StartAt = DateTime.Parse(startAt).ToDateTimeOffsetSweden(), EndAt = DateTime.Parse(endAt).ToDateTimeOffsetSweden(), Quantity = 1, PriceRowType = priceRowType, Price = price };
        }

        private static PriceRowBase InterpreterCompensationPriceRow
        {
            get
            {
                return new PriceRowBase { StartAt = DateTime.Parse(DefaultStartDate).ToDateTimeOffsetSweden(), EndAt = DateTime.Parse(DefaultEndDate).ToDateTimeOffsetSweden(), Quantity = 1, PriceRowType = PriceRowType.InterpreterCompensation, Price = (decimal)Price_2H_Court_Comp1 };
            }
        }

        private PriceRowBase GetPriceRowWithPriceListRowForTest(string startAt, string endAt, decimal price, int quantity, int pricelistRowId)
        {
            return new PriceRowBase { StartAt = DateTime.Parse(startAt).ToDateTimeOffsetSweden(), EndAt = DateTime.Parse(endAt).ToDateTimeOffsetSweden(), Quantity = quantity, PriceRowType = PriceRowType.InterpreterCompensation, Price = price, PriceListRow = new PriceListRow { PriceListRowId = pricelistRowId, MaxMinutes = 30 } };
        }
    }
}
