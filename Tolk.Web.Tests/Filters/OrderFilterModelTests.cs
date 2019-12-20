using FluentAssertions;
using System;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Tolk.Web.Models;
using Xunit;

namespace Tolk.Web.Tests.Filters
{
    public class OrderFilterModelTests
    {
        private const string DbNameOrderFilterTests = "OrderFilterModel_Tests";

        private readonly Language[] mockLanguages;
        private readonly Order[] mockOrders;
        private readonly AspNetUser[] MockCustomerUsers;
        private readonly OrderListRow[] mockOrderListRows;

        public OrderFilterModelTests()
        {
            var mockRankings = MockEntities.MockRankings;
            mockLanguages = MockEntities.MockLanguages;
            MockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers);
            mockOrders = MockEntities.MockOrders(mockLanguages, mockRankings, MockCustomerUsers);

            // Modify request statuses
            mockOrders[0].Requests[0].Status = RequestStatus.DeniedByCreator;
            mockOrders[5].Requests[0].Status = RequestStatus.CancelledByBroker;
            mockOrderListRows = MockEntities.MockOrderListRows(mockOrders.ToList());
        }

        [Fact]
        public void OrderFilter_ByOrderNumber()
        {
            var firstUser = MockCustomerUsers.First();
            var filterFirst = new OrderFilterModel
            {
                OrderNumber = "337",
                IsAdmin = true
            };
            var filterSecond = new OrderFilterModel
            {
                OrderNumber = "2018-001337",
                IsAdmin = true
            };

            var listFirst = filterFirst.Apply(mockOrderListRows.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrderListRows.AsQueryable());
            var actualFirst = mockOrderListRows.Where(o => o.EntityNumber.Contains(filterFirst.OrderNumber));
            var actualSecond = mockOrderListRows.Where(o => o.EntityNumber.Contains(filterSecond.OrderNumber));

            listFirst.Should().HaveCount(actualFirst.Count());
            listFirst.Should().Contain(actualFirst);

            listSecond.Should().HaveCount(actualSecond.Count());
            listSecond.Should().Contain(actualSecond);
        }

        [Fact]
        public void OrderFilter_ByDate()
        {
            var filterFirst = new OrderFilterModel
            {
                DateRange = new DateRange { Start = new DateTime(2018, 06, 01), End = new DateTime(2018, 08, 31) },
                IsAdmin = true
            };
            var filterSecond = new OrderFilterModel
            {
                DateRange = new DateRange { Start = new DateTime(2018, 09, 01), End = new DateTime(2018, 11, 01) },
                IsAdmin = true
            };

            var listFirst = filterFirst.Apply(mockOrderListRows.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrderListRows.AsQueryable());

            listFirst.Should().HaveCount(4);
            listFirst.Should().Contain(new[] { mockOrderListRows[0], mockOrderListRows[1], mockOrderListRows[2], mockOrderListRows[7] });

            listSecond.Should().HaveCount(4);
            listSecond.Should().Contain(new[] { mockOrderListRows[3], mockOrderListRows[4], mockOrderListRows[5], mockOrderListRows[6] });
        }

        [Theory]
        [InlineData("x", 0)]
        [InlineData("Number2", 1)]
        [InlineData("Numb", 8)]
        public void OrderFilter_ByCustomerOrderNumber(string input, int count)
        {
            var filter = new OrderFilterModel
            {
                CustomerReferenceNumber = input,
                IsAdmin = true
            };

            var list = filter.Apply(mockOrderListRows.AsQueryable());

            list.Should().HaveCount(count);
        }

        [Fact]
        public void OrderFilter_ByStatus()
        {
            var filterFirst = new OrderFilterModel
            {
                Status = OrderStatus.Delivered,
                IsAdmin = true
            };
            var filterSecond = new OrderFilterModel
            {
                Status = OrderStatus.CancelledByCreator,
                IsAdmin = true
            };

            var listFirst = filterFirst.Apply(mockOrderListRows.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrderListRows.AsQueryable());
            var actualFirst = mockOrderListRows.Where(o => o.Status == filterFirst.Status);
            var actualSecond = mockOrderListRows.Where(o => o.Status == filterSecond.Status);

            listFirst.Should().HaveCount(actualFirst.Count());
            listFirst.Should().Contain(actualFirst);

            listSecond.Should().HaveCount(actualSecond.Count());
            listSecond.Should().Contain(actualSecond);
        }

        [Fact]
        public void OrderFilter_ByRegion()
        {
            var regionFirst = Region.Regions.Where(r => r.Name == "Västra Götaland").Single();
            var regionSecond = Region.Regions.Where(r => r.Name == "Gotland").Single();
            var filterFirst = new OrderFilterModel
            {
                RegionId = regionFirst.RegionId,
                IsAdmin = true
            };
            var filterSecond = new OrderFilterModel
            {
                RegionId = regionSecond.RegionId,
                IsAdmin = true
            };

            var listFirst = filterFirst.Apply(mockOrderListRows.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrderListRows.AsQueryable());
            var actualFirst = mockOrderListRows.Where(o => o.RegionId == regionFirst.RegionId);
            var actualSecond = mockOrderListRows.Where(o => o.RegionId == regionSecond.RegionId);

            listFirst.Should().HaveCount(actualFirst.Count());
            listFirst.Should().Contain(actualFirst);

            listSecond.Should().HaveCount(actualSecond.Count());
            listSecond.Should().Contain(actualSecond);
        }

        [Fact]
        public void OrderFilter_ByLanguage()
        {
            var languageFirst = mockLanguages.Where(l => l.Name == "English").Single();
            var languageSecond = mockLanguages.Where(l => l.Name == "German").Single();
            var filterFirst = new OrderFilterModel
            {
                LanguageId = languageFirst.LanguageId,
                IsAdmin = true
            };
            var filterSecond = new OrderFilterModel
            {
                LanguageId = languageSecond.LanguageId,
                IsAdmin = true
            };

            var listFirst = filterFirst.Apply(mockOrderListRows.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrderListRows.AsQueryable());
            var actualFirst = mockOrderListRows.Where(o => o.LanguageId == languageFirst.LanguageId);
            var actualSecond = mockOrderListRows.Where(o => o.LanguageId == languageSecond.LanguageId);

            listFirst.Should().HaveCount(actualFirst.Count());
            listFirst.Should().Contain(actualFirst);

            listSecond.Should().HaveCount(actualSecond.Count());
            listSecond.Should().Contain(actualSecond);
        }

        [Fact]
        public void OrderFilter_ByBroker()
        {
            var brokerFirst = 1;
            var brokerSecond = 2;
            var filterFirst = new OrderFilterModel
            {
                BrokerId = brokerFirst,
                IsAdmin = true
            };
            var filterSecond = new OrderFilterModel
            {
                BrokerId = brokerSecond,
                IsAdmin = true
            };

            var listFirst = filterFirst.Apply(mockOrderListRows.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrderListRows.AsQueryable());
            var actualFirst = mockOrderListRows.Where(o => o.BrokerId == brokerFirst);
            var actualSecond = mockOrderListRows.Where(o => o.BrokerId == brokerSecond);

            listFirst.Should().HaveCount(actualFirst.Count());
            listFirst.Should().Contain(actualFirst);

            listSecond.Should().HaveCount(actualSecond.Count());
            listSecond.Should().Contain(actualSecond);
        }

        [Fact]
        public void OrderFilter_ComboByRegionLanguage()
        {
            var languageFirst = mockLanguages.Where(l => l.Name == "Chinese").Single();
            var languageSecond = mockLanguages.Where(l => l.Name == "German").Single();
            var regionFirst = Region.Regions.Where(r => r.Name == "Västra Götaland").Single();
            var regionSecond = Region.Regions.Where(r => r.Name == "Stockholm").Single();
            var filterFirst = new OrderFilterModel
            {
                LanguageId = languageFirst.LanguageId,
                RegionId = regionFirst.RegionId,
                IsAdmin = true
            };
            var filterSecond = new OrderFilterModel
            {
                LanguageId = languageSecond.LanguageId,
                RegionId = regionSecond.RegionId,
                IsAdmin = true
            };

            var listFirst = filterFirst.Apply(mockOrderListRows.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrderListRows.AsQueryable());
            var actualFirst = mockOrderListRows.Where(o => o.LanguageId == languageFirst.LanguageId
                && o.RegionId == regionFirst.RegionId);
            var actualSecond = mockOrderListRows.Where(o => o.LanguageId == languageSecond.LanguageId
                && o.RegionId == regionSecond.RegionId);

            listFirst.Should().HaveCount(actualFirst.Count());
            listFirst.Should().Contain(actualFirst);

            listSecond.Should().HaveCount(actualSecond.Count());
            listSecond.Should().Contain(actualSecond);
        }

        [Fact]
        public void OrderFilter_ComboByTimeBroker()
        {
            var filterFirst = new OrderFilterModel
            {
                DateRange = new DateRange { Start = new DateTime(2018, 09, 01), End = new DateTime(2018, 10, 30) },
                BrokerId = 1,
                IsAdmin = true
            };
            var filterSecond = new OrderFilterModel
            {
                DateRange = new DateRange { Start = new DateTime(2018, 06, 01), End = new DateTime(2018, 08, 31) },
                BrokerId = 2,
                IsAdmin = true
            };

            var listFirst = filterFirst.Apply(mockOrderListRows.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrderListRows.AsQueryable());

            listFirst.Should().HaveCount(3);
            listFirst.Should().Contain(new[] { mockOrderListRows[3], mockOrderListRows[4], mockOrderListRows[6], });

            listSecond.Should().OnlyContain(o => o == mockOrderListRows[0]);
        }

        [Fact]
        public void OrderFilter_DateInclusivity()
        {
            var filter = new OrderFilterModel
            {
                DateRange = new DateRange { Start = new DateTime(2018, 06, 07), End = new DateTime(2018, 08, 07) },
                IsAdmin = true
            };

            var list = filter.Apply(mockOrderListRows.AsQueryable());

            list.Should().HaveCount(3);
            list.Should().Contain(new[] { mockOrderListRows[0], mockOrderListRows[1], mockOrderListRows[2] }, because: "these orders fall within these dates");
        }

        [Fact]
        public void OrderFilter_NoSettings()
        {
            var filter = new OrderFilterModel { IsAdmin = true };

            var list = filter.Apply(mockOrderListRows.AsQueryable());

            list.Should().HaveCount(mockOrderListRows.Count());
            list.Should().Contain(mockOrderListRows, because: "no filter parameters are specified");
        }

        [Fact]
        public void OrderFilter_NoResults()
        {
            var filter = new OrderFilterModel
            {
                BrokerId = 3,
                IsAdmin = true
            };

            var list = filter.Apply(mockOrderListRows.AsQueryable());

            list.Should().BeEmpty("no order is assigned to {0}", filter.BrokerId);
        }
    }
}
