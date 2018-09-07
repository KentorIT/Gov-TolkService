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
        private RequestListItemModel[] requests;

        private Language[] mockLanguages = MockEntities.MockLanguages();

        public RequestFilterModelTests()
        {
            var mockRankings = MockEntities.MockRankings();
            var mockOrders = MockEntities.MockOrders(mockLanguages, mockRankings);
            requests = MockEntities.MockRequests(mockOrders);
        }

        [Fact]
        public void RequestFilter_ByOrderNumber()
        {
            var filter = new RequestFilterModel
            {
                OrderNumber = "33"
            };

            var list = filter.Apply(requests.AsQueryable());

            list.Should().HaveCount(2);
            list.Should().Contain(new[] { requests[1], requests[4] }, 
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
            list.Should().Contain(new[] { requests[1], requests[2] }, 
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
                    .Where(l => l.Name == "English")
                    .Single().LanguageId
            };

            var list = filter.Apply(requests.AsQueryable());

            list.Should().OnlyContain(r => r == requests[1], 
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
                OrderDateRange = new DateRange { Start = new DateTime(2018,07,01), End = new DateTime(2018,10,01) }
            };

            var list = filter.Apply(requests.AsQueryable());

            list.Should().HaveCount(4);
            list.Should().Contain(new[] { requests[0], requests[2], requests[3], requests[4], });
        }

        [Fact]
        public void RequestFilter_ByAnswerByDateRange()
        {
            var filter = new RequestFilterModel
            {
                AnswerByDateRange = new DateRange { Start = new DateTime(2018, 09, 14), End = new DateTime(2018, 10, 31) }
            };

            var list = filter.Apply(requests.AsQueryable());

            list.Should().OnlyContain(r => r == requests[5] );
        }

        [Fact]
        public void RequestFilter_ByStatus()
        {
            var filter = new RequestFilterModel
            {
                Status = BusinessLogic.Enums.RequestStatus.InterpreterReplaced
            };

            var list = filter.Apply(requests.AsQueryable());

            list.Should().OnlyContain(r => r == requests[1]);
        }

        [Fact]
        public void RequestFilter_ComboByRegionLanguageStatus()
        {
            var filter = new RequestFilterModel
            {
                RegionId = Region.Regions.Where(r => r.Name == "Stockholm").Single().RegionId,
                LanguageId = mockLanguages.Where(l => l.Name == "German").Single().LanguageId,
                Status = BusinessLogic.Enums.RequestStatus.AcceptedNewInterpreterAppointed
            };

            var list = filter.Apply(requests.AsQueryable());

            list.Should().OnlyContain(r => r == requests[2]);
        }
    }
}
