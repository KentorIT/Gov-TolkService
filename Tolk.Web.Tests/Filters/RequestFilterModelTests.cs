using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Models;
using Tolk.Web.Tests.Helpers;
using Xunit;

namespace Tolk.Web.Tests.Filters
{
    public class RequestFilterModelTests
    {
        private List<RequestListItemModel> requests;

        private Language[] mockLanguages = MockHelper.MockLanguages();

        public RequestFilterModelTests()
        {
            requests = new List<RequestListItemModel>
            {
                new RequestListItemModel
                {
                    OrderNumber = "2018-000103",
                    RegionId = Region.Regions
                        .Where(r => r.Name == "Stockholm")
                        .Single().RegionId,
                    RegionName = "Stockholm",
                    CustomerId = 34,
                    LanguageId = mockLanguages
                        .Where(r => r.Name == "English")
                        .Single().LanguageId,
                    Start = new DateTimeOffset(2018, 06, 07, 13, 00, 00, new TimeSpan(02,00,00)),
                    End = new DateTimeOffset(2018, 06, 07, 16, 00, 00, new TimeSpan(02,00,00)),
                    ExpiresAt = new DateTimeOffset(2018, 05, 28, 16, 00, 00, new TimeSpan(02,00,00)),
                    Status = BusinessLogic.Enums.RequestStatus.Accepted
                },
                new RequestListItemModel
                {
                    OrderNumber = "2018-000104",
                    RegionId = Region.Regions
                        .Where(r => r.Name == "Dalarna")
                        .Single().RegionId,
                    RegionName = "Dalarna",
                    CustomerId = 2,
                    LanguageId = mockLanguages
                        .Where(r => r.Name == "German")
                        .Single().LanguageId,
                    Start = new DateTimeOffset(2018, 08, 07, 13, 00, 00, new TimeSpan(02,00,00)),
                    End = new DateTimeOffset(2018, 08, 07, 16, 00, 00, new TimeSpan(02,00,00)),
                    ExpiresAt = new DateTimeOffset(2018, 07, 28, 16, 00, 00, new TimeSpan(02,00,00)),
                    Status = BusinessLogic.Enums.RequestStatus.InterpreterReplaced
                },
                new RequestListItemModel
                {
                    OrderNumber = "2018-000105",
                    RegionId = Region.Regions
                        .Where(r => r.Name == "Dalarna")
                        .Single().RegionId,
                    RegionName = "Dalarna",
                    CustomerId = 2,
                    LanguageId = mockLanguages
                        .Where(r => r.Name == "German")
                        .Single().LanguageId,
                    Start = new DateTimeOffset(2018, 08, 07, 13, 00, 00, new TimeSpan(02,00,00)),
                    End = new DateTimeOffset(2018, 08, 07, 16, 00, 00, new TimeSpan(02,00,00)),
                    ExpiresAt = new DateTimeOffset(2018, 08, 05, 16, 00, 00, new TimeSpan(02,00,00)),
                    Status = BusinessLogic.Enums.RequestStatus.AcceptedNewInterpreterAppointed
                },
                new RequestListItemModel
                {
                    OrderNumber = "2018-000066", // execute order 66...
                    RegionId = Region.Regions
                        .Where(r => r.Name == "Stockholm")
                        .Single().RegionId,
                    RegionName = "Stockholm",
                    CustomerId = 0,
                    LanguageId = mockLanguages
                        .Where(r => r.Name == "English")
                        .Single().LanguageId,
                    Start = new DateTimeOffset(2018, 09, 21, 08, 30, 00, new TimeSpan(02,00,00)),
                    End = new DateTimeOffset(2018, 09, 21, 17, 00, 00, new TimeSpan(02,00,00)),
                    ExpiresAt = new DateTimeOffset(2018, 09, 14, 16, 00, 00, new TimeSpan(02,00,00)),
                    Status = BusinessLogic.Enums.RequestStatus.ToBeProcessedByBroker
                },
                new RequestListItemModel
                {
                    OrderNumber = "2018-001337",
                    RegionId = Region.Regions
                        .Where(r => r.Name == "Örebro")
                        .Single().RegionId,
                    RegionName = "Örebro",
                    CustomerId = 1,
                    LanguageId = mockLanguages
                        .Where(r => r.Name == "Chinese")
                        .Single().LanguageId,
                    Start = new DateTimeOffset(2018, 10, 17, 13, 00, 00, new TimeSpan(02,00,00)),
                    End = new DateTimeOffset(2018, 10, 17, 16, 00, 00, new TimeSpan(02,00,00)),
                    ExpiresAt = new DateTimeOffset(2018, 10, 07, 16, 00, 00, new TimeSpan(02,00,00)),
                    Status = BusinessLogic.Enums.RequestStatus.Accepted
                },
            };
        }

        [Fact]
        public void RequestFilter_ByOrderNumber()
        {
            var filter = new RequestFilterModel
            {
                OrderNumber = "3"
            };

            var list = filter.Apply(requests.AsQueryable());

            list.Should().HaveCount(2);
            list.Should().Contain(new[] { requests[0], requests[4] }, 
                because: "Both ordernumbers contain the number {0}", 
                becauseArgs: filter.OrderNumber);
        }

        [Fact]
        public void RequestFilter_ByRegion()
        {
            var filter = new RequestFilterModel
            {
                RegionId = Region.Regions
                    .Where(r => r.Name == "Stockholm")
                    .Single().RegionId
            };

            var list = filter.Apply(requests.AsQueryable());

            list.Should().HaveCount(2);
            list.Should().Contain(new[] { requests[0], requests[3] }, 
                because: "Both requests regard RegionId {0}", 
                becauseArgs: filter.RegionId);
        }

        [Fact]
        public void RequestFilter_ByCustomer()
        {
            var filter = new RequestFilterModel
            {
                CustomerOrganizationId = 2
            };

            var list = filter.Apply(requests.AsQueryable());

            list.Should().HaveCount(2);
            list.Should().Contain(new[] { requests[1], requests[2] }, 
                because: "Both requests regard CustomerId {0}", 
                becauseArgs: filter.CustomerOrganizationId);
        }

        [Fact]
        public void RequestFilter_ByLanguage()
        {
            var filter = new RequestFilterModel
            {
                LanguageId = mockLanguages
                    .Where(l => l.Name == "Chinese")
                    .Single().LanguageId
            };

            var list = filter.Apply(requests.AsQueryable());

            list.Should().OnlyContain(r => r == requests[4], 
                because: "Only {0} has {1} as language", 
                becauseArgs: new[] 
                {
                    requests[4].OrderNumber,
                    mockLanguages.Where(l => l.LanguageId == filter.LanguageId).Single().Name
                });
        }

        [Fact]
        public void RequestFilter_ByOrderDateRange()
        {
            var filter = new RequestFilterModel
            {
                OrderDateRange = new DateRange { Start = new DateTime(2018,06,01), End = new DateTime(2018,10,01) }
            };

            var list = filter.Apply(requests.AsQueryable());

            list.Should().HaveCount(4);
            list.Should().Contain(new[] { requests[0], requests[1], requests[2], requests[3] });
        }

        [Fact]
        public void RequestFilter_ByAnswerByDateRange()
        {
            var filter = new RequestFilterModel
            {
                AnswerByDateRange = new DateRange { Start = new DateTime(2018, 09, 14), End = new DateTime(2018, 10, 31) }
            };

            var list = filter.Apply(requests.AsQueryable());

            list.Should().HaveCount(2);
            list.Should().Contain(new[] { requests[3], requests[4] });
        }

        [Fact]
        public void RequestFilter_ByStatus()
        {
            var filter = new RequestFilterModel
            {
                Status = BusinessLogic.Enums.RequestStatus.AcceptedNewInterpreterAppointed
            };

            var list = filter.Apply(requests.AsQueryable());

            list.Should().OnlyContain(r => r == requests[2]);
        }
    }
}
