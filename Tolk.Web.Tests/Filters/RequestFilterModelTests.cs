using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Models;
using Tolk.Web.Tests.TestHelpers;
using Xunit;
using Tolk.BusinessLogic.Tests.TestHelpers;

namespace Tolk.Web.Tests.Filters
{
    public class RequestFilterModelTests
    {
        private RequestListRow[] requests;

        private Language[] mockLanguages = MockEntities.MockLanguages;

        public RequestFilterModelTests()
        {
            var mockRankings = MockEntities.MockRankings;
            var mockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers);
            var mockOrders = MockEntities.MockOrders(mockLanguages, mockRankings, mockCustomerUsers);
            requests = MockEntities.MockRequests(mockOrders);
        }

        [Fact]
        public void RequestFilter_ByOrderNumber()
        {
            var orderNum = "66";
            var filter = new RequestFilterModel
            {
                OrderNumber = orderNum
            };

            var list = filter.Apply(requests.AsQueryable());
            var actual = requests.Where(r => r.EntityNumber.Contains(orderNum));

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }

        [Theory]
        [InlineData("x", 0)]
        [InlineData("Number2", 1)]
        [InlineData("Numb", 6)]
        public void RequestFilter_ByCustomerOrderNumber(string input, int count)
        {
            var filter = new RequestFilterModel
            {
                CustomerReferenceNumber = input
            };

            var list = filter.Apply(requests.AsQueryable());

            list.Should().HaveCount(count);
        }

        [Fact]
        public void RequestFilter_ByRegion()
        {
            var region = Region.Regions.Where(r => r.Name == "Skåne").Single();
            var filter = new RequestFilterModel
            {
                RegionId = region.RegionId
            };

            var list = filter.Apply(requests.AsQueryable());
            var actual = requests.Where(r => r.RegionId == region.RegionId);

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }

        [Fact]
        public void RequestFilter_ByCustomer()
        {
            var customerId = 2;
            var filter = new RequestFilterModel
            {
                CustomerOrganizationId = customerId,
            };

            var list = filter.Apply(requests.AsQueryable());
            var actual = requests.Where(r => r.CustomerOrganisationId == customerId);

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }

        [Fact]
        public void RequestFilter_ByLanguage()
        {
            var language = mockLanguages.Where(l => l.Name == "Chinese").Single();
            var filter = new RequestFilterModel
            {
                LanguageId = language.LanguageId
            };

            var list = filter.Apply(requests.AsQueryable());
            var actual = requests.Where(r => r.LanguageId == language.LanguageId);

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }

        [Fact]
        public void RequestFilter_ByOrderDateRange()
        {
            var filter = new RequestFilterModel
            {
                OrderDateRange = new DateRange { Start = new DateTime(2018, 07, 01), End = new DateTime(2018, 10, 01) }
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

            list.Should().OnlyContain(r => r == requests[5]);
        }

        [Fact]
        public void RequestFilter_ByStatus()
        {
            var status = BusinessLogic.Enums.RequestStatus.Accepted;
            var filter = new RequestFilterModel
            {
                Status = status
            };

            var list = filter.Apply(requests.AsQueryable());
            var actual = requests.Where(r => r.Status == status);

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }

        [Fact]
        public void RequestFilter_ComboByRegionLanguageStatus()
        {
            var region = Region.Regions.Where(r => r.Name == "Stockholm").Single();
            var language = mockLanguages.Where(l => l.Name == "German").Single();
            var status = BusinessLogic.Enums.RequestStatus.AcceptedNewInterpreterAppointed;
            var filter = new RequestFilterModel
            {
                RegionId = region.RegionId,
                LanguageId = language.LanguageId,
                Status = status,
            };

            var list = filter.Apply(requests.AsQueryable());
            var actual = requests.Where(r => r.RegionId == region.RegionId
                && r.LanguageId == language.LanguageId
                && r.Status == status);

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }
    }
}
