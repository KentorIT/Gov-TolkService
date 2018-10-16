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
        private const double Price_1H_Court_Comp2 = 409;
        private const double Price_1H_Court_Comp3 = 480;
        private const double Price_1H_Court_Comp4 = 606;

        private const double Price_2H_Court_Comp1 = 606;
        private const double Price_3H_Court_Comp1 = 860;
        private const double Price_4H_Court_Comp1 = 1114;
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
        private const int PriceListRowId_LostTime_60M__Court_Comp1 = 1113;
        private const int PriceListRowId_IWH_LostTime_30M__Court_Comp1 = 1121;

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
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.OtherInterpreter, PriceListType.Other, 16)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.EducatedInterpreter, PriceListType.Other, 16)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.AuthorizedInterpreter, PriceListType.Other, 16)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.SpecializedInterpreter, PriceListType.Other, 16)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.OtherInterpreter, PriceListType.Court, 16)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.EducatedInterpreter, PriceListType.Court, 16)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.AuthorizedInterpreter, PriceListType.Court, 16)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", CompetenceLevel.SpecializedInterpreter, PriceListType.Court, 16)]
        public void GetPriceListTest(string startAt, string endAt, CompetenceLevel compLevel, PriceListType priceListType, int actualNoOfRows)
        {
            GetPriceList(DateTime.Parse(startAt), DateTime.Parse(endAt), compLevel, priceListType).Count().Should().Be(actualNoOfRows, "number of rows {0}", actualNoOfRows);
        }

        private List<PriceListRow> GetPriceList(DateTime startAt, DateTime endAt, CompetenceLevel compLevel, PriceListType priceListType)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                return new PriceCalculationService(tolkdbContext).GetPriceList(startAt, endAt, compLevel, priceListType);
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
                var prices = GetPriceList(DateTime.Parse(startAt), DateTime.Parse(endAt), compLevel, priceListType);
                IEnumerable<PriceRowBase> list = new PriceCalculationService(tolkdbContext).GetLostTimePriceRows(DateTime.Parse(startAt), DateTime.Parse(endAt), lostTime, lostTimeIWH, prices);
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
            new PriceCalculationService().GetNoOfDays(DateTime.Parse(startAt), DateTime.Parse(endAt)).Should().Be(actual, "there are {0} days between {1} and {2}", actual, startAt, endAt);
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
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt), DateTime.Parse(endAt), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        [InlineData("2018-10-10 17:00:00", "2018-10-10 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 2), 2)]//2h work 1h IWH
        [InlineData("2018-10-10 18:00:00", "2018-10-10 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 2), 2)]//1h work IWH
        [InlineData("2018-10-10 18:00:00", "2018-10-10 20:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_2H_Court_Comp1 + Price_IWH_30M__Court_Comp1 * 4), 2)]//2h work IWH
        public void IWH_InterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt), DateTime.Parse(endAt), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        [InlineData("2018-10-13 10:00:00", "2018-10-13 11:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 2), 2)]//1h work IWH weekend
        [InlineData("2018-10-13 10:00:00", "2018-10-13 15:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5H_Court_Comp1 + Price_IWH_Weekend_30M__Court_Comp1 * 10), 2)]//5h work IWH weekend
        public void IWH_Weekend_InterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt), DateTime.Parse(endAt), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        [InlineData("2018-12-24 10:00:00", "2018-12-24 11:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_1H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 2), 2)]//1h work IWH big holiday
        [InlineData("2018-12-24 10:00:00", "2018-12-24 15:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, (Price_5H_Court_Comp1 + Price_IWH_BigHoliday_30M__Court_Comp1 * 10), 2)]//5h work IWH big holiday
        public void IWH_BigHoliday_InterpreterCompensation(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt), DateTime.Parse(endAt), competenceLevel, listType, rankingId);
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
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt), DateTime.Parse(endAt), competenceLevel, listType, rankingId);
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
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPrices(DateTime.Parse(startAt), DateTime.Parse(endAt), competenceLevel, listType, rankingId);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.SocialInsuranceCharge).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.SocialInsuranceCharge).Should().Be(noOfrows, "number of rows {0}", noOfrows);
            }
        }

        [Theory]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_1H_Court_Comp1, (Price_1H_Court_Comp1 * SocialInsuranceCharge / 100))]//1h nwt, complevel 1
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_2H_Court_Comp1, (Price_2H_Court_Comp1 * SocialInsuranceCharge / 100))]//2h nwt comp.Level 1
        [InlineData("2018-10-10 10:00:00", "2018-10-10 13:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_3H_Court_Comp1, (Price_3H_Court_Comp1 * SocialInsuranceCharge / 100))]//3h nwt comp.Level 1
        [InlineData("2018-10-10 10:00:00", "2018-10-10 14:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_4H_Court_Comp1, (Price_4H_Court_Comp1 * SocialInsuranceCharge / 100))]//4h nwt comp.Level 1
        [InlineData("2018-10-10 10:00:00", "2018-10-10 15:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, Price_5H_Court_Comp1, (Price_5H_Court_Comp1 * SocialInsuranceCharge / 100))]//5h nwt comp.Level 1
        public void SocialInsurancePriceRow(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, decimal interpreterPrice, decimal actualPrice)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                List<PriceRowBase> priceRows = new List<PriceRowBase>
                {
                    { GetPriceRowBaseForTest(startAt, endAt, listType, competenceLevel, rankingId, PriceRowType.InterpreterCompensation, interpreterPrice) }
                };
                PriceRowBase pr = new PriceCalculationService(tolkdbContext).GetPriceRowSocialInsuranceCharge(DateTime.Parse(startAt), DateTime.Parse(endAt), priceRows);
                pr.TotalPrice.Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pr.PriceRowType.Should().Be(PriceRowType.SocialInsuranceCharge, "price row type {0}", PriceRowType.SocialInsuranceCharge.GetDescription());
            }
        }

        [Fact]
        public void SocialInsurancePriceRow_InvalidOperationException()
        {
            using (var tolkDbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                //add extra row for SocialInsuranceCharge with date overlapping => should throw exception 
                tolkDbContext.PriceCalculationCharges.Add(new PriceCalculationCharge { StartDate = new DateTime(2018, 01, 01), EndDate = new DateTime(2098, 01, 01), PriceCalculationChargeId = 4, ChargePercentage = (decimal)SocialInsuranceCharge, ChargeTypeId = ChargeType.SocialInsuranceCharge });
                tolkDbContext.SaveChanges();
                Action a = () => new PriceCalculationService(tolkDbContext).GetPriceRowSocialInsuranceCharge(DateTime.Parse(DefaultStartDate), DateTime.Parse(DefaultEndDate), new List<PriceRowBase> { InterpreterCompensationPriceRow });
                a.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void AdministrativePriceRow_InvalidOperationException()
        {
            using (var tolkDbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                //add extra row for AdministrativeCharge with date overlapping => should throw exception 
                tolkDbContext.PriceCalculationCharges.Add(new PriceCalculationCharge { StartDate = new DateTime(2018, 01, 01), EndDate = new DateTime(2098, 01, 01), PriceCalculationChargeId = 3, ChargePercentage = (decimal)AdministrativeCharge, ChargeTypeId = ChargeType.AdministrativeCharge });
                tolkDbContext.SaveChanges();
                Action a = () => new PriceCalculationService(tolkDbContext).GetPriceRowAdministrativeCharge(DateTime.Parse(DefaultStartDate), DateTime.Parse(DefaultEndDate), new List<PriceRowBase> { InterpreterCompensationPriceRow });
                a.Should().Throw<InvalidOperationException>();
            }
        }

        [Theory]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, 300, 1)]//1h nwt, complevel 1
        public void TravelCostRow(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, bool useRequestRows, decimal actualPrice, int noOfrows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                //get a requestRow for broker fee
                List<PriceRowBase> requestPriceRows = new List<PriceRowBase>
                {
                    { GetPriceRowBaseForTest(startAt, endAt, listType, competenceLevel, rankingId, PriceRowType.BrokerFee, (decimal)Broker_Fee_Price_Comp1) }
                };
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPricesRequisition(DateTime.Parse(startAt), DateTime.Parse(endAt), competenceLevel, listType, rankingId, out bool useRequestRowsToCompare, null, null, requestPriceRows, actualPrice);
                pi.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.TravelCost).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceRowType == PriceRowType.TravelCost).Should().Be(noOfrows, "number of rows {0}", noOfrows);
                useRequestRowsToCompare.Should().Be(useRequestRows, "cause useRequestRows should be {0}", useRequestRows);
            }
        }

        [Theory]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, Price_LostTime_60M__Court_Comp1, 31, 0, 1, PriceListRowId_LostTime_60M__Court_Comp1, PriceListRowId_IWH_LostTime_30M__Court_Comp1)]//31m nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, Price_LostTime_60M__Court_Comp1, 60, 0, 1, PriceListRowId_LostTime_60M__Court_Comp1, PriceListRowId_IWH_LostTime_30M__Court_Comp1)]//1h nwt
        [InlineData("2018-10-10 10:00:00", "2018-10-10 11:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, Price_LostTime_60M__Court_Comp1 * 2, 90, 0, 1, PriceListRowId_LostTime_60M__Court_Comp1, PriceListRowId_IWH_LostTime_30M__Court_Comp1)]//90m nwt
        [InlineData("2018-10-10 18:00:00", "2018-10-10 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, Price_LostTime_60M__Court_Comp1 + Price_IWH_LostTime_30M__Court_Comp1, 31, 30, 2, PriceListRowId_LostTime_60M__Court_Comp1, PriceListRowId_IWH_LostTime_30M__Court_Comp1)]//30m iwh
        [InlineData("2018-10-10 18:00:00", "2018-10-10 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, (Price_LostTime_60M__Court_Comp1 + (Price_IWH_LostTime_30M__Court_Comp1 * 2)), 60, 60, 2, PriceListRowId_LostTime_60M__Court_Comp1, PriceListRowId_IWH_LostTime_30M__Court_Comp1)]//60m iwh
        [InlineData("2018-10-10 18:00:00", "2018-10-10 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, ((Price_LostTime_60M__Court_Comp1 * 2) + (Price_IWH_LostTime_30M__Court_Comp1 * 3)), 90, 90, 2, PriceListRowId_LostTime_60M__Court_Comp1, PriceListRowId_IWH_LostTime_30M__Court_Comp1)]//90m iwh
        [InlineData("2018-10-10 18:00:00", "2018-10-10 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, Price_LostTime_60M__Court_Comp1 + Price_IWH_LostTime_30M__Court_Comp1, 31, 15, 2, PriceListRowId_LostTime_60M__Court_Comp1, PriceListRowId_IWH_LostTime_30M__Court_Comp1)]//31m nwt 15m iwh
        [InlineData("2018-10-10 18:00:00", "2018-10-10 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, ((Price_LostTime_60M__Court_Comp1 * 2) + Price_IWH_LostTime_30M__Court_Comp1), 90, 20, 2, PriceListRowId_LostTime_60M__Court_Comp1, PriceListRowId_IWH_LostTime_30M__Court_Comp1)]//90m nwt 20m iwh
        [InlineData("2018-10-10 18:00:00", "2018-10-10 19:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false, ((Price_LostTime_60M__Court_Comp1 * 2) + (Price_IWH_LostTime_30M__Court_Comp1 * 2)), 90, 40, 2, PriceListRowId_LostTime_60M__Court_Comp1, PriceListRowId_IWH_LostTime_30M__Court_Comp1)]//90m 40iwh
        public void LostTimeRows(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, bool useRequestRows, decimal actualPrice, int lostTime, int iwhLostTime, int noOfrows, int priceListRowId_NT, int priceListRowId_IWH)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                //get a requestRow for broker fee
                List<PriceRowBase> requestPriceRows = new List<PriceRowBase>
                {
                    { GetPriceRowBaseForTest(startAt, endAt, listType, competenceLevel, rankingId, PriceRowType.BrokerFee, (decimal)Broker_Fee_Price_Comp1) }
                };
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPricesRequisition(DateTime.Parse(startAt), DateTime.Parse(endAt), competenceLevel, listType, rankingId, out bool useRequestRowsToCompare, lostTime, iwhLostTime, requestPriceRows, actualPrice);
                pi.PriceRows.Where(pr => pr.PriceListRowId == priceListRowId_NT || pr.PriceListRowId == priceListRowId_IWH).Sum(pr => pr.TotalPrice).Should().Be(actualPrice, "total price should be {0}", actualPrice);
                pi.PriceRows.Count(pr => pr.PriceListRowId == priceListRowId_NT || pr.PriceListRowId == priceListRowId_IWH).Should().Be(noOfrows, "number of rows {0}", noOfrows);
                useRequestRowsToCompare.Should().Be(useRequestRows, "cause useRequestRows should be {0}", useRequestRows);
            }
        }

        [Theory]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", "2018-10-10 10:00:00", "2018-10-10 12:00:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", "2018-10-10 10:00:00", "2018-10-10 12:20:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false)]
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", "2018-10-10 10:00:00", "2018-10-10 11:45:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, false)]//same tax used
        [InlineData("2018-10-10 10:00:00", "2018-10-10 12:00:00", "2018-10-10 10:00:00", "2018-10-10 11:29:00", PriceListType.Court, CompetenceLevel.OtherInterpreter, DefaultRankingId, true)]//same tax used
        public void UseRequestPricerows(string requestStartAt, string requestEndAt, string requisitionStartAt, string requisitionEndAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, bool useRequestRows)
        {
            using (var tolkdbContext = CreateTolkDbContext(DbNameWithPriceData))
            {
                //get a requestRow for broker fee
                List<PriceRowBase> requestPriceRows = new List<PriceRowBase>
                {
                    { GetPriceRowBaseForTest(requestStartAt, requestEndAt, listType, competenceLevel, rankingId, PriceRowType.BrokerFee, (decimal)Broker_Fee_Price_Comp1) },
                    { GetPriceRowBaseForTest(requestStartAt, requestEndAt, listType, competenceLevel, rankingId, PriceRowType.InterpreterCompensation, (decimal)Price_2H_Court_Comp1) }
                };
                PriceInformation pi = new PriceCalculationService(tolkdbContext).GetPricesRequisition(DateTime.Parse(requisitionStartAt), DateTime.Parse(requisitionEndAt), competenceLevel, listType, rankingId, out bool useRequestRowsToCompare, null, null, requestPriceRows, null);
                useRequestRowsToCompare.Should().Be(useRequestRows, "useRequestRows should be {0}", useRequestRows);
            }
        }

        private PriceRowBase GetPriceRowBaseForTest(string startAt, string endAt, PriceListType listType, CompetenceLevel competenceLevel, int rankingId, PriceRowType priceRowType, decimal price)
        {
            return new PriceRowBase { StartAt = DateTime.Parse(startAt), EndAt = DateTime.Parse(endAt), Quantity = 1, PriceRowType = priceRowType, Price = price };
        }

        private static PriceRowBase InterpreterCompensationPriceRow
        {
            get
            {
                return new PriceRowBase { StartAt = DateTime.Parse(DefaultStartDate), EndAt = DateTime.Parse(DefaultEndDate), Quantity = 1, PriceRowType = PriceRowType.InterpreterCompensation, Price = (decimal)Price_2H_Court_Comp1 };
            }
        }
    }
}
