using System;
using System.Collections.Generic;
using System.Text;
using Tolk.BusinessLogic.Entities;
using System.Linq;
using Tolk.Web.Models;
using Xunit;
using FluentAssertions;
using Tolk.Web.Tests.Helpers;

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
            mockOrders = MockEntities.MockOrders(mockLanguages, mockRankings);

            // Modify request statuses
            mockOrders[0].Requests[0].Status = BusinessLogic.Enums.RequestStatus.DeniedByCreator;
            mockOrders[5].Requests[0].Status = BusinessLogic.Enums.RequestStatus.CancelledByBrokerConfirmed;
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

            listFirst.Should().OnlyContain(item => item == mockOrders.Where(o => o.OrderNumber == "2018-001337").Single());

            listSecond.Should().OnlyContain(item => item == mockOrders.Where(o => o.OrderNumber == "2018-000006").Single());
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

            listFirst.Should().HaveCount(3);
            listFirst.Should().Contain(new[] { mockOrders[0], mockOrders[1], mockOrders[2] });

            listSecond.Should().HaveCount(4);
            listSecond.Should().Contain(new[] { mockOrders[3], mockOrders[4], mockOrders[5], mockOrders[6] });
        }

        [Fact]
        public void OrderFilter_ByStatus()
        {
            var filterFirst = new OrderFilterModel
            {
                Status = BusinessLogic.Enums.OrderStatus.Delivered
            };
            var filterSecond = new OrderFilterModel
            {
                Status = BusinessLogic.Enums.OrderStatus.CancelledByCreatorConfirmed
            };

            var listFirst = filterFirst.Apply(mockOrders.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrders.AsQueryable());

            listFirst.Should().HaveCount(2);
            listFirst.Should().Contain(new[] { mockOrders[3], mockOrders[5] });

            listSecond.Should().OnlyContain(o => o == mockOrders[6]);
        }

        [Fact]
        public void OrderFilter_ByRegion()
        {
            var filterFirst = new OrderFilterModel
            {
                RegionId = Region.Regions
                    .Where(r => r.Name == "Västra Götaland")
                    .Select(r => r.RegionId)
                    .Single()
            };
            var filterSecond = new OrderFilterModel
            {
                RegionId = Region.Regions
                    .Where(r => r.Name == "Gotland")
                    .Select(r => r.RegionId)
                    .Single()
            };

            var listFirst = filterFirst.Apply(mockOrders.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrders.AsQueryable());

            listFirst.Should().HaveCount(3);
            listFirst.Should().Contain(new[] { mockOrders[3], mockOrders[4], mockOrders[6] });

            listSecond.Should().OnlyContain(o => o == mockOrders[5]);
        }

        [Fact]
        public void OrderFilter_ByLanguage()
        {
            var filterFirst = new OrderFilterModel
            {
                LanguageId = mockLanguages
                    .Where(l => l.Name == "English")
                    .Select(l => l.LanguageId)
                    .Single()
            };
            var filterSecond = new OrderFilterModel
            {
                LanguageId = mockLanguages
                    .Where(l => l.Name == "German")
                    .Select(l => l.LanguageId)
                    .Single()
            };

            var listFirst = filterFirst.Apply(mockOrders.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrders.AsQueryable());

            listFirst.Should().OnlyContain(o => o == mockOrders[0]);

            listSecond.Should().HaveCount(2);
            listSecond.Should().Contain(new[] { mockOrders[1], mockOrders[4] });
        }

        [Fact]
        public void OrderFilter_ByBroker()
        {
            var filterFirst = new OrderFilterModel
            {
                BrokerId = 0
            };
            var filterSecond = new OrderFilterModel
            {
                BrokerId = 1
            };

            var listFirst = filterFirst.Apply(mockOrders.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrders.AsQueryable());

            listFirst.Should().HaveCount(5);
            listFirst.Should().Contain(new[] { mockOrders[1], mockOrders[2], mockOrders[3], mockOrders[4], mockOrders[6] });

            listSecond.Should().HaveCount(2);
            listSecond.Should().Contain(new[] { mockOrders[0], mockOrders[5] });
        }

        [Fact]
        public void OrderFilter_ComboByRegionLanguage()
        {
            var filterFirst = new OrderFilterModel
            {
                LanguageId = mockLanguages
                    .Where(l => l.Name == "Chinese")
                    .Select(l => l.LanguageId)
                    .Single(),
                RegionId = Region.Regions
                    .Where(r => r.Name == "Västra Götaland")
                    .Select(r => r.RegionId)
                    .Single()
            };
            var filterSecond = new OrderFilterModel
            {
                LanguageId = mockLanguages
                    .Where(l => l.Name == "German")
                    .Select(l => l.LanguageId)
                    .Single(),
                RegionId = Region.Regions
                    .Where(r => r.Name == "Stockholm")
                    .Select(r => r.RegionId)
                    .Single()
            };

            var listFirst = filterFirst.Apply(mockOrders.AsQueryable());
            var listSecond = filterSecond.Apply(mockOrders.AsQueryable());

            listFirst.Should().HaveCount(2);
            listFirst.Should().Contain(new[] { mockOrders[3], mockOrders[6] });

            listSecond.Should().OnlyContain(o => o == mockOrders[1] );
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

            list.Should().HaveCount(7);
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
