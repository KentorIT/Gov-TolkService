using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Tolk.BusinessLogic.Utilities;
using Xunit;

namespace Tolk.BusinessLogic.Tests.Services
{
    public class StatisticsServiceTests
    {
        private readonly TolkDbContext _tolkDbContext;
        private readonly StubSwedishClock _clock;
        private readonly StatisticsService _statService;
        private readonly DateTimeOffset dateIn_7_DaysRange = DateTimeOffset.Now.AddDays(-2);
        private readonly DateTimeOffset dateIn_14_DaysRange = DateTimeOffset.Now.AddDays(-9);
        private readonly DateTimeOffset breakDate = DateTimeOffset.Now.AddDays(-7);
        private readonly DateTimeOffset resetDate = DateTimeOffset.Now.AddMonths(-18);

        public StatisticsServiceTests()
        {
            _tolkDbContext = CreateTolkDbContext();
            _clock = new StubSwedishClock(DateTimeOffset.Now.ToString());

            var mockLanguages = MockEntities.MockLanguages;
            var mockRankings = MockEntities.MockRankings;
            var mockCustomers = MockEntities.MockCustomers;
            var mockCustomerUsers = MockEntities.MockCustomerUsers(mockCustomers);
            var mockOrders = MockEntities.MockOrders(mockLanguages, mockRankings, mockCustomerUsers);
            var mockRequisitions = MockEntities.MockRequisitions(mockOrders);
            var mockComplaints = MockEntities.MockComplaints(mockOrders);
            var regions = Region.Regions;
            //Initialize data if not already initialized
            if (!_tolkDbContext.CustomerOrganisations.Any())
            {
                _tolkDbContext.AddRange(mockCustomers);
                _tolkDbContext.AddRange(mockCustomerUsers);
                _tolkDbContext.AddRange(mockLanguages);
                _tolkDbContext.AddRange(mockRankings);
                _tolkDbContext.AddRange(mockOrders);
                _tolkDbContext.AddRange(mockRequisitions);
                _tolkDbContext.AddRange(mockComplaints);
                _tolkDbContext.AddRange(regions);
            }
            _tolkDbContext.SaveChanges();
            _statService = new StatisticsService(_tolkDbContext, _clock);
        }

        private TolkDbContext CreateTolkDbContext(string databaseName = "empty")
        {
            var options = new DbContextOptionsBuilder<TolkDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            return new TolkDbContext(options);
        }

        [Theory]
        [InlineData(200, 180, 10.0, StatisticsChangeType.Decreasing)]
        [InlineData(2000, 1992, 0.4, StatisticsChangeType.Decreasing)]
        [InlineData(200, 220, 10.0, StatisticsChangeType.Increasing)]
        [InlineData(2000, 2008, 0.4, StatisticsChangeType.Increasing)]
        [InlineData(200, 200, 0, StatisticsChangeType.Unchanged)]
        [InlineData(0, 0, 0, StatisticsChangeType.Unchanged)]
        [InlineData(200, 0, 0, StatisticsChangeType.NANoDataLastWeek)]
        [InlineData(0, 200, 0, StatisticsChangeType.NANoDataLastWeek)]

        public void GetWeeklyStatistics(int weekBefore, int thisWeek, decimal expectedPercentageDiff, StatisticsChangeType expectedChangeType)
        {
            WeeklyStatisticsModel ws = StatisticsService.GetWeeklyStatistics(weekBefore, thisWeek, string.Empty);
            Assert.Equal(expectedPercentageDiff, ws.DiffPercentage);
            Assert.Equal(expectedChangeType, ws.ChangeType);
        }

        [Theory]
        [InlineData(5, 1, 80, StatisticsChangeType.Decreasing)]
        [InlineData(1, 2, 100, StatisticsChangeType.Increasing)]
        [InlineData(1, 1, 0, StatisticsChangeType.Unchanged)]
        [InlineData(0, 1, 0, StatisticsChangeType.NANoDataLastWeek)]
        [InlineData(1, 0, 0, StatisticsChangeType.NANoDataLastWeek)]
        public void GetWeeklyUserLoginStatistics(int weekBefore, int thisWeek, decimal expectedPercentageDiff, StatisticsChangeType expectedChangeType)
        {
            List<UserLoginLogEntry> usersLogIn = new List<UserLoginLogEntry>();
            for (int i = 1; i <= weekBefore; i++)
            {
                usersLogIn.Add(new UserLoginLogEntry { LoggedInAt = dateIn_14_DaysRange, UserId = i });
            }
            for (int i = weekBefore + 1; i <= (weekBefore + thisWeek); i++)
            {
                usersLogIn.Add(new UserLoginLogEntry { LoggedInAt = dateIn_7_DaysRange, UserId = i });
            }
            _tolkDbContext.UserLoginLogEntries.AddRange(usersLogIn);

            _tolkDbContext.SaveChanges();
            WeeklyStatisticsModel ws = _statService.GetWeeklyUserLogins(breakDate);
            Assert.Equal(expectedPercentageDiff, ws.DiffPercentage);
            Assert.Equal(expectedChangeType, ws.ChangeType);
            _tolkDbContext.UserLoginLogEntries.RemoveRange(usersLogIn);
            _tolkDbContext.SaveChanges();
        }

        [Theory]
        [InlineData(2, 1, 50, StatisticsChangeType.Decreasing)]
        [InlineData(1, 2, 100, StatisticsChangeType.Increasing)]
        [InlineData(1, 1, 0, StatisticsChangeType.Unchanged)]
        [InlineData(0, 1, 0, StatisticsChangeType.NANoDataLastWeek)]
        [InlineData(1, 0, 0, StatisticsChangeType.NANoDataLastWeek)]
        public void GetWeeklyNewUserStatistics(int weekBefore, int thisWeek, decimal expectedPercentageDiff, StatisticsChangeType expectedChangeType)
        {

            List<UserAuditLogEntry> userAudits = new List<UserAuditLogEntry>();
            for (int i = 1; i <= weekBefore; i++)
            {
                userAudits.Add(new UserAuditLogEntry { LoggedAt = dateIn_14_DaysRange, UserId = i, UserChangeType = UserChangeType.Created });
            }
            for (int i = weekBefore + 1; i <= (weekBefore + thisWeek); i++)
            {
                userAudits.Add(new UserAuditLogEntry { LoggedAt = dateIn_7_DaysRange, UserId = i, UserChangeType = UserChangeType.Created });
            }
            _tolkDbContext.UserAuditLogEntries.AddRange(userAudits);

            _tolkDbContext.SaveChanges();
            WeeklyStatisticsModel ws = _statService.GetWeeklyNewUsers(breakDate);
            Assert.Equal(expectedPercentageDiff, ws.DiffPercentage);
            Assert.Equal(expectedChangeType, ws.ChangeType);
            _tolkDbContext.UserAuditLogEntries.RemoveRange(userAudits);
            _tolkDbContext.SaveChanges();
        }


        [Theory]
        [InlineData(2, 1, 50, StatisticsChangeType.Decreasing)]
        [InlineData(1, 2, 100, StatisticsChangeType.Increasing)]
        [InlineData(1, 1, 0, StatisticsChangeType.Unchanged)]
        [InlineData(0, 1, 0, StatisticsChangeType.NANoDataLastWeek)]
        [InlineData(1, 0, 0, StatisticsChangeType.NANoDataLastWeek)]
        public void GetWeeklyOrderStatistics(int weekBefore, int thisWeek, decimal expectedPercentageDiff, StatisticsChangeType expectedChangeType)
        {
            var orders = _tolkDbContext.Orders.ToList();

            //reset the date for all orders 
            foreach (Order o in orders)
            {
                o.CreatedAt = resetDate;
            }
            for (int i = 1; i <= weekBefore; i++)
            {
                orders[i].CreatedAt = dateIn_14_DaysRange;
            }
            for (int i = weekBefore + 1; i <= (weekBefore + thisWeek); i++)
            {
                orders[i].CreatedAt = dateIn_7_DaysRange;
            }
            _tolkDbContext.SaveChanges();
            WeeklyStatisticsModel ws = _statService.GetWeeklyOrderStatistics(breakDate);
            Assert.Equal(expectedPercentageDiff, ws.DiffPercentage);
            Assert.Equal(expectedChangeType, ws.ChangeType);
        }

        [Theory]
        [InlineData(2, 1, 50, StatisticsChangeType.Decreasing)]
        [InlineData(1, 2, 100, StatisticsChangeType.Increasing)]
        [InlineData(1, 1, 0, StatisticsChangeType.Unchanged)]
        [InlineData(0, 1, 0, StatisticsChangeType.NANoDataLastWeek)]
        [InlineData(1, 0, 0, StatisticsChangeType.NANoDataLastWeek)]
        public void GetWeeklyDeliveredOrderStatistics(int weekBefore, int thisWeek, decimal expectedPercentageDiff, StatisticsChangeType expectedChangeType)
        {
            var orders = _tolkDbContext.Orders.ToList();

            //reset the date and set correct status for all orders 
            foreach (Order o in orders)
            {
                o.StartAt = resetDate;
                o.EndAt = resetDate.AddHours(2);
                o.Status = OrderStatus.Delivered;
            }
            for (int i = 1; i <= weekBefore; i++)
            {
                orders[i].StartAt = dateIn_14_DaysRange;
                orders[i].EndAt = dateIn_14_DaysRange.AddHours(2);
            }
            for (int i = weekBefore + 1; i <= (weekBefore + thisWeek); i++)
            {
                orders[i].StartAt = dateIn_7_DaysRange;
                orders[i].EndAt = dateIn_7_DaysRange.AddHours(2);
            }
            _tolkDbContext.SaveChanges();
            WeeklyStatisticsModel ws = _statService.GetWeeklyDeliveredOrderStatistics(breakDate);
            Assert.Equal(expectedPercentageDiff, ws.DiffPercentage);
            Assert.Equal(expectedChangeType, ws.ChangeType);
        }



        [Theory]
        [InlineData(2, 1, 50, StatisticsChangeType.Decreasing)]
        [InlineData(1, 2, 100, StatisticsChangeType.Increasing)]
        [InlineData(1, 1, 0, StatisticsChangeType.Unchanged)]
        [InlineData(0, 1, 0, StatisticsChangeType.NANoDataLastWeek)]
        [InlineData(1, 0, 0, StatisticsChangeType.NANoDataLastWeek)]
        public void GetWeeklyRequisitionStatistics(int weekBefore, int thisWeek, decimal expectedPercentageDiff, StatisticsChangeType expectedChangeType)
        {
            var requisitions = _tolkDbContext.Requisitions.ToList();

            //reset the date for all requisitions 
            foreach (Requisition r in requisitions)
            {
                r.CreatedAt = resetDate;
            }
            for (int i = 1; i <= weekBefore; i++)
            {
                requisitions[i].CreatedAt = dateIn_14_DaysRange;
            }
            for (int i = weekBefore + 1; i <= (weekBefore + thisWeek); i++)
            {
                requisitions[i].CreatedAt = dateIn_7_DaysRange;
            }
            _tolkDbContext.SaveChanges();
            WeeklyStatisticsModel ws = _statService.GetWeeklyRequisitionStatistics(breakDate);
            Assert.Equal(expectedPercentageDiff, ws.DiffPercentage);
            Assert.Equal(expectedChangeType, ws.ChangeType);
        }

        [Theory]
        [InlineData(2, 1, 50, StatisticsChangeType.Decreasing)]
        [InlineData(1, 2, 100, StatisticsChangeType.Increasing)]
        [InlineData(1, 1, 0, StatisticsChangeType.Unchanged)]
        [InlineData(0, 1, 0, StatisticsChangeType.NANoDataLastWeek)]
        [InlineData(1, 0, 0, StatisticsChangeType.NANoDataLastWeek)]
        public void GetWeeklyComplaintStatistics(int weekBefore, int thisWeek, decimal expectedPercentageDiff, StatisticsChangeType expectedChangeType)
        {
            var complaints = _tolkDbContext.Complaints.ToList();

            //reset the date for all complaints 
            foreach (Complaint c in complaints)
            {
                c.CreatedAt = resetDate;
            }
            for (int i = 1; i <= weekBefore; i++)
            {
                complaints[i].CreatedAt = dateIn_14_DaysRange;
            }
            for (int i = weekBefore + 1; i <= (weekBefore + thisWeek); i++)
            {
                complaints[i].CreatedAt = dateIn_7_DaysRange;
            }
            _tolkDbContext.SaveChanges();
            WeeklyStatisticsModel ws = _statService.GetWeeklyComplaintStatistics(breakDate);
            Assert.Equal(expectedPercentageDiff, ws.DiffPercentage);
            Assert.Equal(expectedChangeType, ws.ChangeType);
        }


        [Theory]
        [InlineData(8, 5, 2, 1, 0, 0, 3)]
        [InlineData(8, 5, 1, 1, 1, 0, 4)]
        [InlineData(9, 5, 1, 1, 1, 1, 5)]
        [InlineData(9, 3, 2, 2, 1, 1, 5)]
        [InlineData(9, 2, 2, 2, 2, 1, 5)]
        [InlineData(9, 4, 3, 2, 0, 0, 3)]

        public void GetOrderLanguageStatistics(int noOfTotalOrdersToCheck, int noOfTop1, int noOfTop2, int noOfTop3, int noOfTop4, int noOfTop5, int expectedNoOfListItems)
        {

            if (noOfTotalOrdersToCheck != (noOfTop1 + noOfTop2 + noOfTop3 + noOfTop4 + noOfTop5))
                Assert.True(false, "Incorrect InlineData, noOfTotalOrdersToCheck cant differ from the amount of each no of top value");
            if ((noOfTop1 < noOfTop2) || (noOfTop2 < noOfTop3) || (noOfTop3 < noOfTop4) || (noOfTop4 < noOfTop5))
                Assert.True(false, "Incorrect InlineData, wrong relationship between no of top values");

            int[] listValues = { noOfTop1, noOfTop2, noOfTop3, noOfTop4, noOfTop5 };

            var orders = _tolkDbContext.Orders.ToList();

            if (noOfTotalOrdersToCheck > orders.Count)
                Assert.True(false, "Too many noOfTotalOrdersToCheck in inlinedata, change InlineData or no of mock orders");

            int c = 0;
            for (int i = 0; i < noOfTop1; i++)
            {
                orders[i].LanguageId = MockEntities.MockLanguages[0].LanguageId;
                c = i;
            }
            if (noOfTop2 > 0)
            {
                for (int i = ++c; i < (noOfTop1 + noOfTop2); i++)
                {
                    orders[i].LanguageId = MockEntities.MockLanguages[1].LanguageId;
                    c = i;
                }
            }
            if (noOfTop3 > 0)
            {
                for (int i = ++c; i < (noOfTop1 + noOfTop2 + noOfTop3); i++)
                {
                    orders[i].LanguageId = MockEntities.MockLanguages[2].LanguageId;
                    c = i;
                }
            }
            if (noOfTop4 > 0)
            {
                for (int i = ++c; i < (noOfTop1 + noOfTop2 + noOfTop3 + noOfTop4); i++)
                {
                    orders[i].LanguageId = MockEntities.MockLanguages[3].LanguageId;
                    c = i;
                }
            }
            if (noOfTop5 > 0)
            {
                for (int i = ++c; i < (noOfTop1 + noOfTop2 + noOfTop3 + noOfTop4 + noOfTop5); i++)
                {
                    orders[i].LanguageId = MockEntities.MockLanguages[4].LanguageId;
                    c = i;
                }
            }
            if (noOfTotalOrdersToCheck < orders.Count)
            {
                for (int i = ++c; i < orders.Count; i++)
                {
                    orders[i].LanguageId = null;
                }
            }
            _tolkDbContext.SaveChanges();
            OrderStatisticsModel os = StatisticsService.GetOrderLanguageStatistics(_tolkDbContext.Orders.Where(o => o.LanguageId > 0).Include(o => o.Language));
            Assert.Equal(expectedNoOfListItems, os.TotalListItems.Count());
            int count = 0;
            foreach (var item in os.TotalListItems)
            {
                Assert.Equal(MockEntities.MockLanguages[count].Name, item.Name);
                Assert.Equal(listValues[count], item.NoOfItems);
                Assert.Equal(Math.Round((double)listValues[count] * 100 / noOfTotalOrdersToCheck, 1), item.PercentageValueToDisplay);
                count++;
            }
        }


        [Theory]
        [InlineData(8, 5, 2, 1, 0, 0, 3)]
        [InlineData(8, 5, 1, 1, 1, 0, 4)]
        [InlineData(9, 5, 1, 1, 1, 1, 5)]
        [InlineData(9, 3, 2, 2, 1, 1, 5)]
        [InlineData(9, 2, 2, 2, 2, 1, 5)]
        [InlineData(9, 4, 3, 2, 0, 0, 3)]

        public void GetOrderRegionStatistics(int noOfTotalOrdersToCheck, int noOfTop1, int noOfTop2, int noOfTop3, int noOfTop4, int noOfTop5, int expectedNoOfListItems)
        {

            if (noOfTotalOrdersToCheck != (noOfTop1 + noOfTop2 + noOfTop3 + noOfTop4 + noOfTop5))
                Assert.True(false, "Incorrect InlineData, noOfTotalOrdersToCheck cant differ from the amount of each no of top value");
            if ((noOfTop1 < noOfTop2) || (noOfTop2 < noOfTop3) || (noOfTop3 < noOfTop4) || (noOfTop4 < noOfTop5))
                Assert.True(false, "Incorrect InlineData, wrong relationship between no of top values");

            int[] listValues = { noOfTop1, noOfTop2, noOfTop3, noOfTop4, noOfTop5 };

            var orders = _tolkDbContext.Orders.ToList();

            if (noOfTotalOrdersToCheck > orders.Count)
                Assert.True(false, "Too many noOfTotalOrdersToCheck in inlinedata, change InlineData or no of mock orders");
            var regionNotIncludedInTest = Region.Regions[listValues.Count()].RegionId;

            int c = 0;
            for (int i = 0; i < noOfTop1; i++)
            {
                orders[i].RegionId = Region.Regions[0].RegionId;
                c = i;
            }
            if (noOfTop2 > 0)
            {
                for (int i = ++c; i < (noOfTop1 + noOfTop2); i++)
                {
                    orders[i].RegionId = Region.Regions[1].RegionId;
                    c = i;
                }
            }
            if (noOfTop3 > 0)
            {
                for (int i = ++c; i < (noOfTop1 + noOfTop2 + noOfTop3); i++)
                {
                    orders[i].RegionId = Region.Regions[2].RegionId;
                    c = i;
                }
            }
            if (noOfTop4 > 0)
            {
                for (int i = ++c; i < (noOfTop1 + noOfTop2 + noOfTop3 + noOfTop4); i++)
                {
                    orders[i].RegionId = Region.Regions[3].RegionId;
                    c = i;
                }
            }
            if (noOfTop5 > 0)
            {
                for (int i = ++c; i < (noOfTop1 + noOfTop2 + noOfTop3 + noOfTop4 + noOfTop5); i++)
                {
                    orders[i].RegionId = Region.Regions[4].RegionId;
                    c = i;
                }
            }
            if (noOfTotalOrdersToCheck < orders.Count)
            {
                for (int i = ++c; i < orders.Count; i++)
                {
                    orders[i].RegionId = regionNotIncludedInTest;
                }
            }
            _tolkDbContext.SaveChanges();
            OrderStatisticsModel os = StatisticsService.GetOrderRegionStatistics(_tolkDbContext.Orders.Where(o => o.RegionId != regionNotIncludedInTest).Include(o => o.Region));
            Assert.Equal(expectedNoOfListItems, os.TotalListItems.Count());
            int count = 0;
            foreach (var item in os.TotalListItems)
            {
                Assert.Equal(Region.Regions[count].Name, item.Name);
                Assert.Equal(listValues[count], item.NoOfItems);
                Assert.Equal(Math.Round((double)listValues[count] * 100 / noOfTotalOrdersToCheck, 1), item.PercentageValueToDisplay);
                count++;
            }
        }


        [Theory]
        [InlineData(8, 5, 2, 1, 0, 0, 3)]
        [InlineData(8, 5, 1, 1, 1, 0, 4)]
        [InlineData(9, 5, 1, 1, 1, 1, 5)]
        [InlineData(9, 3, 2, 2, 1, 1, 5)]
        [InlineData(9, 2, 2, 2, 2, 1, 5)]
        [InlineData(9, 4, 3, 2, 0, 0, 3)]
        public void GetOrderCustomerStatistics(int noOfTotalOrdersToCheck, int noOfTop1, int noOfTop2, int noOfTop3, int noOfTop4, int noOfTop5, int expectedNoOfListItems)
        {
            if (noOfTotalOrdersToCheck != (noOfTop1 + noOfTop2 + noOfTop3 + noOfTop4 + noOfTop5))
                Assert.True(false, "Incorrect InlineData, noOfTotalOrdersToCheck cant differ from the amount of each no of top value");
            if ((noOfTop1 < noOfTop2) || (noOfTop2 < noOfTop3) || (noOfTop3 < noOfTop4) || (noOfTop4 < noOfTop5))
                Assert.True(false, "Incorrect InlineData, wrong relationship between no of top values");

            int[] listValues = { noOfTop1, noOfTop2, noOfTop3, noOfTop4, noOfTop5 };

            var orders = _tolkDbContext.Orders.ToList();

            if (noOfTotalOrdersToCheck > orders.Count)
                Assert.True(false, "Too many noOfTotalOrdersToCheck in inlinedata, change InlineData or no of mock orders");
            var customerNotIncludedInTest = MockEntities.MockCustomers[listValues.Count()].CustomerOrganisationId;

            int c = 0;
            for (int i = 0; i < noOfTop1; i++)
            {
                orders[i].CustomerOrganisationId = MockEntities.MockCustomers[0].CustomerOrganisationId;
                c = i;
            }
            if (noOfTop2 > 0)
            {
                for (int i = ++c; i < (noOfTop1 + noOfTop2); i++)
                {
                    orders[i].CustomerOrganisationId = MockEntities.MockCustomers[1].CustomerOrganisationId;
                    c = i;
                }
            }
            if (noOfTop3 > 0)
            {
                for (int i = ++c; i < (noOfTop1 + noOfTop2 + noOfTop3); i++)
                {
                    orders[i].CustomerOrganisationId = MockEntities.MockCustomers[2].CustomerOrganisationId;
                    c = i;
                }
            }
            if (noOfTop4 > 0)
            {
                for (int i = ++c; i < (noOfTop1 + noOfTop2 + noOfTop3 + noOfTop4); i++)
                {
                    orders[i].CustomerOrganisationId = MockEntities.MockCustomers[3].CustomerOrganisationId;
                    c = i;
                }
            }
            if (noOfTop5 > 0)
            {
                for (int i = ++c; i < (noOfTop1 + noOfTop2 + noOfTop3 + noOfTop4 + noOfTop5); i++)
                {
                    orders[i].CustomerOrganisationId = MockEntities.MockCustomers[4].CustomerOrganisationId;
                    c = i;
                }
            }
            if (noOfTotalOrdersToCheck < orders.Count)
            {
                for (int i = ++c; i < orders.Count; i++)
                {
                    orders[i].CustomerOrganisationId = customerNotIncludedInTest;
                }
            }
            _tolkDbContext.SaveChanges();
            OrderStatisticsModel os = StatisticsService.GetOrderCustomerStatistics(_tolkDbContext.Orders.
                Where(o => o.CustomerOrganisationId != customerNotIncludedInTest).Include(o => o.CustomerOrganisation));
            Assert.Equal(expectedNoOfListItems, os.TotalListItems.Count());
            int count = 0;

            foreach (var item in os.TotalListItems)
            {
                Assert.Equal(MockEntities.MockCustomers[count].Name, item.Name);
                Assert.Equal(listValues[count], item.NoOfItems);
                Assert.Equal(Math.Round((double)listValues[count] * 100 / noOfTotalOrdersToCheck, 1), item.PercentageValueToDisplay);
                count++;
            }
        }
    }
}
