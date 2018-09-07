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

        private List<Order> orders;
        private readonly List<Ranking> rankings;

        private Language[] mockLanguages = MockHelper.MockLanguages();

        public OrderFilterModelTests()
        {
            rankings = new List<Ranking>
            {
                new Ranking { RankingId = 0, BrokerId = 0, Rank = 1 },
                new Ranking { RankingId = 1, BrokerId = 1, Rank = 2 },
            };

            orders = new List<Order>
            {
                new Order {
                    OrderId = 0,
                    OrderNumber = "2018-000000",
                    StartAt = new DateTimeOffset(2018,06,07,13,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,06,07,15,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Stockholm").Single(),
                    Language = mockLanguages.Where(l => l.Name == "English").Single(),
                    Status = BusinessLogic.Enums.OrderStatus.DeliveryAccepted,
                    Requests = new List<Request>
                    {
                        new Request(rankings[0], new DateTimeOffset(2018,05,26,14,56,00, new TimeSpan(02,00,00))),
                        new Request(rankings[1], new DateTimeOffset(2018,06,02,14,11,00, new TimeSpan(02,00,00))),
                    },
                },
                new Order {
                    OrderId = 1,
                    OrderNumber = "2018-000001",
                    StartAt = new DateTimeOffset(2018,07,07,08,30,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,07,07,17,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Stockholm").Single(),
                    Language = mockLanguages.Where(l => l.Name == "German").Single(),
                    Status = BusinessLogic.Enums.OrderStatus.DeliveryAccepted,
                    Requests = new List<Request>
                    {
                        new Request(rankings[0], new DateTimeOffset(2018,06,26,14,56,00, new TimeSpan(02,00,00))),
                    },
                },
                new Order {
                    OrderId = 2,
                    OrderNumber = "2018-000002",
                    StartAt = new DateTimeOffset(2018,08,07,13,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,08,07,13,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Skåne").Single(),
                    Language = mockLanguages.Where(l => l.Name == "French").Single(),
                    Status = BusinessLogic.Enums.OrderStatus.Delivered,
                    Requests = new List<Request>
                    {
                        new Request(rankings[0], new DateTimeOffset(2018,07,29,14,56,00, new TimeSpan(02,00,00))),
                    }
                },
                
                new Order {
                    OrderId = 3,
                    OrderNumber = "2018-000003",
                    StartAt = new DateTimeOffset(2018,09,03,13,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,09,03,19,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Västra Götaland").Single(),
                    Language = mockLanguages.Where(l => l.Name == "Chinese").Single(),
                    Status = BusinessLogic.Enums.OrderStatus.Delivered,
                    Requests = new List<Request>
                    {
                        new Request(rankings[0], new DateTimeOffset(2018,09,01,14,56,00, new TimeSpan(02,00,00))),
                    }
                },
                new Order {
                    OrderId = 4,
                    OrderNumber = "2018-000004",
                    StartAt = new DateTimeOffset(2018,09,18,09,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,09,18,13,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Västra Götaland").Single(),
                    Language = mockLanguages.Where(l => l.Name == "German").Single(),
                    Status = BusinessLogic.Enums.OrderStatus.RequestResponded,
                    Requests = new List<Request>
                    {
                        new Request(rankings[0], new DateTimeOffset(2018,09,15,14,56,00, new TimeSpan(02,00,00))),
                    }
                },
                new Order {
                    OrderId = 5,
                    OrderNumber = "2018-000005",
                    StartAt = new DateTimeOffset(2018,10,09,10,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,10,09,15,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Gotland").Single(),
                    Language = mockLanguages.Where(l => l.Name == "French").Single(),
                    Status = BusinessLogic.Enums.OrderStatus.Requested,
                    Requests = new List<Request>
                    {
                        new Request(rankings[0], new DateTimeOffset(2018,09,15,14,56,00, new TimeSpan(02,00,00))),
                        new Request(rankings[1], new DateTimeOffset(2018,10,02,14,56,00, new TimeSpan(02,00,00))),
                    }
                },
                new Order {
                    OrderId = 6,
                    OrderNumber = "2018-000006",
                    StartAt = new DateTimeOffset(2018,09,03,00,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,09,03,19,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Västra Götaland").Single(),
                    Language = mockLanguages.Where(l => l.Name == "Chinese").Single(),
                    Status = BusinessLogic.Enums.OrderStatus.CancelledByCreatorConfirmed,
                    Requests = new List<Request>
                    {
                        new Request(rankings[0], new DateTimeOffset(2018,08,25,14,56,00, new TimeSpan(02,00,00))),
                    }
                },
            };

            // Modify request statuses
            orders[0].Requests[0].Status = BusinessLogic.Enums.RequestStatus.DeniedByCreator;
            orders[5].Requests[0].Status = BusinessLogic.Enums.RequestStatus.CancelledByBrokerConfirmed;
        }

        [Fact]
        public void OrderFilter_ByOrderNumber()
        {
            var filterFirst = new OrderFilterModel
            {
                OrderNumber = "5"
            };
            var filterSecond = new OrderFilterModel
            {
                OrderNumber = "2018-000006"
            };


            var listFirst = filterFirst.Apply(orders.AsQueryable());
            var listSecond = filterSecond.Apply(orders.AsQueryable());

            listFirst.Should().OnlyContain(item => item == orders.Where(o => o.OrderNumber == "2018-000005").Single());
            listSecond.Should().OnlyContain(item => item == orders.Where(o => o.OrderNumber == "2018-000006").Single());
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

            var listFirst = filterFirst.Apply(orders.AsQueryable());
            var listSecond = filterSecond.Apply(orders.AsQueryable());

            listFirst.Should().HaveCount(3);
            listFirst.Should().Contain(new[] { orders[0], orders[1], orders[2] });

            listSecond.Should().HaveCount(4);
            listSecond.Should().Contain(new[] { orders[3], orders[4], orders[5], orders[6] });
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

            var listFirst = filterFirst.Apply(orders.AsQueryable());
            var listSecond = filterSecond.Apply(orders.AsQueryable());

            listFirst.Should().HaveCount(2);
            listFirst.Should().Contain(new[] { orders[2], orders[3] });

            listSecond.Should().OnlyContain(o => o == orders[6]);
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

            var listFirst = filterFirst.Apply(orders.AsQueryable());
            var listSecond = filterSecond.Apply(orders.AsQueryable());

            listFirst.Should().HaveCount(3);
            listFirst.Should().Contain(new[] { orders[3], orders[4], orders[6] });

            listSecond.Should().OnlyContain(o => o == orders[5]);
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

            var listFirst = filterFirst.Apply(orders.AsQueryable());
            var listSecond = filterSecond.Apply(orders.AsQueryable());

            listFirst.Should().OnlyContain(o => o == orders[0]);

            listSecond.Should().HaveCount(2);
            listSecond.Should().Contain(new[] { orders[1], orders[4] });
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

            var listFirst = filterFirst.Apply(orders.AsQueryable());
            var listSecond = filterSecond.Apply(orders.AsQueryable());

            listFirst.Should().HaveCount(5);
            listFirst.Should().Contain(new[] { orders[1], orders[2], orders[3], orders[4], orders[6] });

            listSecond.Should().HaveCount(2);
            listSecond.Should().Contain(new[] { orders[0], orders[5] });
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

            var listFirst = filterFirst.Apply(orders.AsQueryable());
            var listSecond = filterSecond.Apply(orders.AsQueryable());

            listFirst.Should().HaveCount(2);
            listFirst.Should().Contain(new[] { orders[3], orders[6] });

            listSecond.Should().OnlyContain(o => o == orders[1] );
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

            var listFirst = filterFirst.Apply(orders.AsQueryable());
            var listSecond = filterSecond.Apply(orders.AsQueryable());

            listFirst.Should().HaveCount(3);
            listFirst.Should().Contain(new[] { orders[3], orders[4], orders[6], });

            listSecond.Should().OnlyContain(o => o == orders[0]);
        }

        [Fact]
        public void OrderFilter_DateInclusivity()
        {
            var filter = new OrderFilterModel
            {
                DateRange = new DateRange { Start = new DateTime(2018, 06, 07), End = new DateTime(2018, 08, 07) }
            };

            var list = filter.Apply(orders.AsQueryable());

            list.Should().HaveCount(3);
            list.Should().Contain(new[] { orders[0], orders[1], orders[2] }, because: "these orders fall within these dates");
        }

        [Fact]
        public void OrderFilter_NoSettings()
        {
            var filter = new OrderFilterModel {};

            var list = filter.Apply(orders.AsQueryable());

            list.Should().HaveCount(7);
            list.Should().Contain(orders, because: "no filter parameters are specified");
        }

        [Fact]
        public void OrderFilter_NoResults()
        {
            var filter = new OrderFilterModel
            {
                BrokerId = 3
            };

            var list = filter.Apply(orders.AsQueryable());

            list.Should().BeEmpty("no order is assigned to {0}", filter.BrokerId);
        }
    }
}
