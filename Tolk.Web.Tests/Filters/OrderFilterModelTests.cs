using System;
using Tolk.BusinessLogic.Entities;
using System.Linq;
using Tolk.Web.Models;
using Xunit;
using FluentAssertions;
using Tolk.Web.Tests.TestHelpers;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Tests.TestHelpers;

namespace Tolk.Web.Tests.Filters
{
    public class OrderFilterModelTests
    {
        private const string DbNameOrderFilterTests = "OrderFilterModel_Tests";

        private Language[] mockLanguages;
        private Order[] mockOrders;

        public OrderFilterModelTests()
        {
            var mockRankings = MockEntities.MockRankings();
            mockLanguages = MockEntities.MockLanguages();
            var mockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers());
            mockOrders = MockEntities.MockOrders(mockLanguages, mockRankings, mockCustomerUsers);

            // Modify request statuses
            mockOrders[0].Requests[0].Status = RequestStatus.DeniedByCreator;
            mockOrders[5].Requests[0].Status = RequestStatus.CancelledByBroker;
        }

        [Fact]
        public void OrderFilter_ByOrderNumber()
        {
            var filterFirst = new OrderFilterModel
            {
                OrderNumber = "337"
            };
            var filterSecond = new OrderFilterModel
            {
                OrderNumber = "2018-000006"
            };

            var listFirst = filterFirst.Apply(mockOrders.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrders.AsQueryable());
            var actualFirst = mockOrders.Where(o => o.OrderNumber.Contains(filterFirst.OrderNumber));
            var actualSecond = mockOrders.Where(o => o.OrderNumber.Contains(filterSecond.OrderNumber));

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
                DateRange = new DateRange { Start = new DateTime(2018,06,01), End = new DateTime(2018,08,31) }
            };
            var filterSecond = new OrderFilterModel
            {
                DateRange = new DateRange { Start = new DateTime(2018, 09, 01), End = new DateTime(2018, 11, 01) }
            };

            var listFirst = filterFirst.Apply(mockOrders.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrders.AsQueryable());

            listFirst.Should().HaveCount(4);
            listFirst.Should().Contain(new[] { mockOrders[0], mockOrders[1], mockOrders[2], mockOrders[7] });

            listSecond.Should().HaveCount(4);
            listSecond.Should().Contain(new[] { mockOrders[3], mockOrders[4], mockOrders[5], mockOrders[6] });
        }

        [Theory]
        [InlineData("x", 0)]
        [InlineData("Number2", 1)]
        [InlineData("Numb", 8)]
        public void OrderFilter_ByCustomerOrderNumber(string input, int count)
        {
            var filter = new OrderFilterModel
            {
                CustomerReferenceNumber = input
            };

            var list = filter.Apply(mockOrders.AsQueryable());

            list.Should().HaveCount(count);
        }

        [Fact]
        public void OrderFilter_ByStatus()
        {
            var filterFirst = new OrderFilterModel
            {
                Status = OrderStatus.Delivered
            };
            var filterSecond = new OrderFilterModel
            {
                Status = OrderStatus.CancelledByCreator
            };

            var listFirst = filterFirst.Apply(mockOrders.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrders.AsQueryable());
            var actualFirst = mockOrders.Where(o => o.Status == filterFirst.Status);
            var actualSecond = mockOrders.Where(o => o.Status == filterSecond.Status);

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
            };
            var filterSecond = new OrderFilterModel
            {
                RegionId = regionSecond.RegionId,
            };

            var listFirst = filterFirst.Apply(mockOrders.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrders.AsQueryable());
            var actualFirst = mockOrders.Where(o => o.Region == regionFirst);
            var actualSecond = mockOrders.Where(o => o.Region == regionSecond);

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
            };
            var filterSecond = new OrderFilterModel
            {
                LanguageId = languageSecond.LanguageId,
            };

            var listFirst = filterFirst.Apply(mockOrders.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrders.AsQueryable());
            var actualFirst = mockOrders.Where(o => o.Language == languageFirst);
            var actualSecond = mockOrders.Where(o => o.Language == languageSecond);

            listFirst.Should().HaveCount(actualFirst.Count());
            listFirst.Should().Contain(actualFirst);

            listSecond.Should().HaveCount(actualSecond.Count());
            listSecond.Should().Contain(actualSecond);
        }

        [Fact]
        public void OrderFilter_ByBroker()
        {
            var brokerFirst = 0;
            var brokerSecond = 1;
            var filterFirst = new OrderFilterModel
            {
                BrokerId = brokerFirst
            };
            var filterSecond = new OrderFilterModel
            {
                BrokerId = brokerSecond
            };

            var listFirst = filterFirst.Apply(mockOrders.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrders.AsQueryable());
            var actualFirst = mockOrders.Where(o => o.Requests.Any(r => r.Ranking.BrokerId == brokerFirst 
                && (r.Status == RequestStatus.Created 
                || r.Status == RequestStatus.Received 
                || r.Status == RequestStatus.Accepted 
                || r.Status == RequestStatus.Approved 
                || r.Status == RequestStatus.AcceptedNewInterpreterAppointed)));
            var actualSecond = mockOrders.Where(o => o.Requests.Any(r => r.Ranking.BrokerId == brokerSecond
                && (r.Status == RequestStatus.Created
                || r.Status == RequestStatus.Received
                || r.Status == RequestStatus.Accepted
                || r.Status == RequestStatus.Approved
                || r.Status == RequestStatus.AcceptedNewInterpreterAppointed)));

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
            };
            var filterSecond = new OrderFilterModel
            {
                LanguageId = languageSecond.LanguageId,
                RegionId = regionSecond.RegionId,
            };

            var listFirst = filterFirst.Apply(mockOrders.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrders.AsQueryable());
            var actualFirst = mockOrders.Where(o => o.Language == languageFirst
                && o.Region == regionFirst);
            var actualSecond = mockOrders.Where(o => o.Language == languageSecond
                && o.Region == regionSecond);

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
                DateRange = new DateRange { Start = new DateTime(2018,09,01), End = new DateTime(2018,10,30) },
                BrokerId = 0
            };
            var filterSecond = new OrderFilterModel
            {
                DateRange = new DateRange { Start = new DateTime(2018, 06, 01), End = new DateTime(2018, 08, 31) },
                BrokerId = 1
            };

            var listFirst = filterFirst.Apply(mockOrders.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrders.AsQueryable());

            listFirst.Should().HaveCount(3);
            listFirst.Should().Contain(new[] { mockOrders[3], mockOrders[4], mockOrders[6], });

            listSecond.Should().OnlyContain(o => o == mockOrders[0]);
        }

        [Fact]
        public void OrderFilter_DateInclusivity()
        {
            var filter = new OrderFilterModel
            {
                DateRange = new DateRange { Start = new DateTime(2018, 06, 07), End = new DateTime(2018, 08, 07) }
            };

            var list = filter.Apply(mockOrders.AsQueryable());

            list.Should().HaveCount(3);
            list.Should().Contain(new[] { mockOrders[0], mockOrders[1], mockOrders[2] }, because: "these orders fall within these dates");
        }

        [Fact]
        public void OrderFilter_NoSettings()
        {
            var filter = new OrderFilterModel {};

            var list = filter.Apply(mockOrders.AsQueryable());

            list.Should().HaveCount(mockOrders.Count());
            list.Should().Contain(mockOrders, because: "no filter parameters are specified");
        }

        [Fact]
        public void OrderFilter_NoResults()
        {
            var filter = new OrderFilterModel
            {
                BrokerId = 3
            };

            var list = filter.Apply(mockOrders.AsQueryable());

            list.Should().BeEmpty("no order is assigned to {0}", filter.BrokerId);
        }
    }
}
