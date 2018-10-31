using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Models;
using Tolk.Web.Tests.TestHelpers;
using Tolk.BusinessLogic.Enums;
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
            var orderNum = "654";
            var filter = new AssignmentFilterModel
            {
                OrderNumber = orderNum
            };

            var list = filter.Apply(mockRequests.AsQueryable(), _clock);
            var actual = mockRequests.Where(req => req.Order.OrderNumber.Contains(orderNum));

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
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
                DateRange = new DateRange { Start = new DateTime(2018, 07, 01), End = new DateTime(2018, 09, 30) }
            };

            var list = filter.Apply(mockRequests.AsQueryable(), _clock);
            var actual = new[] { mockRequests[2], mockRequests[3], mockRequests[4], mockRequests[5], mockRequests[8], mockRequests[9] };

            list.Should().HaveCount(actual.Length);
            list.Should().Contain(actual);
        }

        [Fact]
        private void AssignmentFilter_ByStatusExecuted()
        {
            var filter = new AssignmentFilterModel
            {
                Status = AssignmentStatus.Executed
            };

            var list = filter.Apply(mockRequests.AsQueryable(), _clock);
            var actual = mockRequests.Where(r => r.Order.Status == OrderStatus.Delivered
                || r.Order.Status == OrderStatus.DeliveryAccepted);
            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }

        [Fact]
        private void AssignmentFilter_ByStatusCancelled()
        {
            var filter = new AssignmentFilterModel
            {
                Status = AssignmentStatus.Cancelled
            };

            var list = filter.Apply(mockRequests.AsQueryable(), _clock);
            var actual = mockRequests.Where(r => r.Order.Status == OrderStatus.CancelledByBroker
                || r.Order.Status == OrderStatus.CancelledByBrokerConfirmed
                || r.Order.Status == OrderStatus.CancelledByCreator
                || r.Order.Status == OrderStatus.CancelledByCreatorConfirmed

                );

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }

        [Fact]
        private void AssignmentFilter_ByStatusToBeExecuted()
        {
            var filter = new AssignmentFilterModel
            {
                Status = AssignmentStatus.ToBeExecuted
            };

            var list = filter.Apply(mockRequests.AsQueryable(), _clock);
            var actual = mockRequests.Where(r => !r.Requisitions.Any()
                && r.Order.StartAt > _clock.SwedenNow
                && r.Status == RequestStatus.Approved
                && !r.Order.ReplacingOrderId.HasValue);
            if (list.ToList().Count == 0)
            {
                actual.ToList().Count().Should().Be(0);
            }
            else
            {
                list.Should().HaveCount(actual.Count());
                list.Should().Contain(actual);
            }
        }

        [Fact]
        private void AssignmentFilter_ByStatusToBeReported()
        {
            var filter = new AssignmentFilterModel
            {
                Status = AssignmentStatus.ToBeReported
            };

            var list = filter.Apply(mockRequests.AsQueryable(), _clock);
            var actual = mockRequests.Where(r => !r.Requisitions.Any()
                && r.Order.StartAt < _clock.SwedenNow
                && r.Status == RequestStatus.Approved);

            if (list.ToList().Count == 0)
            {
                actual.ToList().Count().Should().Be(0);
            }
            else
            {
                list.Should().HaveCount(actual.Count());
                list.Should().Contain(actual);
            }
        }

        [Fact]
        private void AssignmentFilter_ComboByLanguageStatusRegion()
        {
            var region = Region.Regions.Where(r => r.Name == "Uppsala").Single();
            var language = mockLanguages.Where(l => l.Name == "French").Single();
            var filter = new AssignmentFilterModel
            {
                Status = AssignmentStatus.Executed,
                RegionId = region.RegionId,
                LanguageId = language.LanguageId,
            };

            var list = filter.Apply(mockRequests.AsQueryable(), _clock);
            var actual = mockRequests.Where(r => r.Order.Region == region
                && r.Order.Language == language
                && r.Order.StartAt < _clock.SwedenNow
                && (r.Order.Status == OrderStatus.Delivered
                || r.Order.Status == OrderStatus.DeliveryAccepted));

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }
    }
}
