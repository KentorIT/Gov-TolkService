using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Models;
using Tolk.Web.Tests.TestHelpers;
using Xunit;

namespace Tolk.Web.Tests.Filters
{
    public class AssignmentFilterModelTests
    {
        private Language[] mockLanguages;
        private Request[] mockRequests;
        private StubSwedishClock _clock;

        public AssignmentFilterModelTests()
        {
            _clock = new StubSwedishClock("2018-09-03 12:57:14");

            mockLanguages = MockEntities.MockLanguages();
            var mockRankings = MockEntities.MockRankings();
            var mockOrders = MockEntities.MockOrders(mockLanguages, mockRankings);
            var mockRequisitions = MockEntities.MockRequisitions(mockOrders);
            mockOrders = MockEntities.LinkRequisitionsInOrdersRequests(mockOrders, mockRequisitions);
            mockRequests = MockEntities.GetRequestsFromOrders(mockOrders);
        }

        [Fact]
        private void AssignmentFilter_ByOrderNumber()
        {
            var filter = new AssignmentFilterModel
            {
                OrderNumber = "654"
            };

            var list = filter.Apply(mockRequests.AsQueryable(), _clock);

            list.Should().OnlyContain(r => r == mockRequests.Where(req => req.Order.OrderNumber == "2018-000654").Single());
        }

        [Fact]
        private void AssignmentFilter_ByRegion()
        {
            var region = Region.Regions.Where(r => r.Name == "Gotland").Single();
            var filter = new AssignmentFilterModel
            {
                RegionId = region.RegionId
            };

            var list = filter.Apply(mockRequests.AsQueryable(), _clock);
            var actual = mockRequests.Where(r => r.Order.Region == region);

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }

        [Fact]
        private void AssignmentFilter_ByCustomer()
        {
            int customerId = 1;
            var filter = new AssignmentFilterModel
            {
                CustomerOrganizationId = customerId
            };

            var list = filter.Apply(mockRequests.AsQueryable(), _clock);
            var actual = mockRequests.Where(r => r.Order.CustomerOrganisationId == customerId);

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }

        [Fact]
        private void AssignmentFilter_ByLanguage()
        {
            var language = mockLanguages.Where(l => l.Name == "Chinese").Single();
            var filter = new AssignmentFilterModel
            {
                LanguageId = language.LanguageId
            };

            var list = filter.Apply(mockRequests.AsQueryable(), _clock);
            var actual = mockRequests.Where(r => r.Order.LanguageId == language.LanguageId);

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }

        [Fact]
        private void AssignmentFilter_ByDateRange()
        {
            var filter = new AssignmentFilterModel
            {
                DateRange = new DateRange { Start = new DateTime(2018,07,01), End = new DateTime(2018,09,30) }
            };

            var list = filter.Apply(mockRequests.AsQueryable(), _clock);
            var actual = new[] { mockRequests[2], mockRequests[3], mockRequests[4], mockRequests[5], mockRequests[8] };

            list.Should().HaveCount(actual.Length);
            list.Should().Contain(actual);
        }

        [Fact]
        private void AssignmentFilter_ByStatusDefault()
        {
            var filter = new AssignmentFilterModel
            {
                Status = BusinessLogic.Enums.AssignmentStatus.Executed
            };

            var list = filter.Apply(mockRequests.AsQueryable(), _clock);
            var actual = mockRequests.Where(r => r.Requisitions.Any() 
                && r.Order.Status == BusinessLogic.Enums.OrderStatus.Delivered
                || r.Order.Status == BusinessLogic.Enums.OrderStatus.DeliveryAccepted);

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }

        [Fact]
        private void AssignmentFilter_ByStatusToBeExecuted()
        {
            var filter = new AssignmentFilterModel
            {
                Status = BusinessLogic.Enums.AssignmentStatus.ToBeExecuted
            };

            var list = filter.Apply(mockRequests.AsQueryable(), _clock);
            var actual = mockRequests.Where(r => !r.Requisitions.Any()
                && r.Order.StartAt > _clock.SwedenNow);

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }

        [Fact]
        private void AssignmentFilter_ByStatusToBeReported()
        {
            var filter = new AssignmentFilterModel
            {
                Status = BusinessLogic.Enums.AssignmentStatus.ToBeReported
            };

            var list = filter.Apply(mockRequests.AsQueryable(), _clock);
            var actual = mockRequests.Where(r => !r.Requisitions.Any()
                && r.Order.StartAt < _clock.SwedenNow);

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }

        [Fact]
        private void AssignmentFilter_ComboByLanguageStatusRegion()
        {
            var region = Region.Regions.Where(r => r.Name == "Uppsala").Single();
            var language = mockLanguages.Where(l => l.Name == "French").Single();
            var filter = new AssignmentFilterModel
            {
                Status = BusinessLogic.Enums.AssignmentStatus.ToBeExecuted,
                RegionId = region.RegionId,
                LanguageId = language.LanguageId,
            };

            var list = filter.Apply(mockRequests.AsQueryable(), _clock);
            var actual = mockRequests.Where(r => r.Order.Region == region
                && r.Order.Language == language
                && (!r.Requisitions.Any()
                    && r.Order.StartAt > _clock.SwedenNow));

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }
    }
}
