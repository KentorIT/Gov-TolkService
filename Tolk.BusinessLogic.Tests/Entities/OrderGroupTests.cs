using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Xunit;

namespace Tolk.BusinessLogic.Tests.Entities
{
    public class OrderGroupTests
    {
        private readonly OrderGroup[] MockOrderGroups;

        public OrderGroupTests()
        {
            MockOrderGroups = MockEntities.MockOrderGroups(MockEntities.MockLanguages, MockEntities.MockRankings, MockEntities.MockCustomerUsers(MockEntities.MockCustomers));
        }

        [Theory]
        [InlineData(1, OrderStatus.Requested, RequestStatus.Created)]
        public void CreateRequestGroup(int requestGroups, OrderStatus expectedStatus, RequestStatus expectedRequestStatus)
        {
            var orderGroup = MockOrderGroups.Where(og => og.OrderGroupNumber == "JUSTCREATED").Single();
            orderGroup.CreateRequestGroup(MockEntities.MockRankings.AsQueryable(), null, orderGroup.CreatedAt.AddMinutes(1));
            Assert.Equal(requestGroups, orderGroup.RequestGroups.Count());
            Assert.Equal(orderGroup.RequestGroups.First().Requests.Count, orderGroup.Orders.Count);
            Assert.Equal(expectedStatus, orderGroup.Status);
            Assert.Equal(orderGroup.Status, orderGroup.Orders.First().Status);
            Assert.Equal(expectedRequestStatus, orderGroup.RequestGroups.First().Status);
            Assert.Equal(orderGroup.RequestGroups.First().Requests.First().Status, orderGroup.RequestGroups.First().Status);
        }

        [Theory]
        [InlineData(2, OrderStatus.Requested, RequestStatus.LostDueToQuarantine, RequestStatus.Created)]
        public void CreateRequestGroupWithQuarantine(int requestGroups, OrderStatus expectedStatus, RequestStatus quarantinedStatus, RequestStatus requestedStatus)
        {
            var orderGroup = MockOrderGroups.Where(og => og.OrderGroupNumber == "JUSTCREATED").Single();
            orderGroup.CreateRequestGroup(MockEntities.MockRankingsWithQuarantines.AsQueryable(), null, orderGroup.CreatedAt.AddMinutes(1));
            Assert.Equal(requestGroups, orderGroup.RequestGroups.Count());
            Assert.Equal(orderGroup.RequestGroups.Last().Requests.Count, orderGroup.Orders.Count);
            Assert.Equal(expectedStatus, orderGroup.Status);
            Assert.Equal(quarantinedStatus, orderGroup.RequestGroups.First().Status);
            Assert.Equal(requestedStatus, orderGroup.RequestGroups.Last().Status);
        }

        [Theory]
        [InlineData(new int[] { }, 1, 1, null, 1, 1, false, true)]
        [InlineData(new int[] { }, 1, 1, null, 1, 1, true, true)]
        [InlineData(new int[] { }, 1, 1, null, 1, 2, true, true)]
        [InlineData(new int[] { }, 1, 1, null, 1, 2, false, false)]
        [InlineData(new int[] { }, 1, 1, null, 2, 2, false, false)]
        [InlineData(new int[] { }, 1, 1, null, 2, 2, true, false)]
        [InlineData(new int[] { 1 }, 1, 1, null, 1, 1, false, true)]
        [InlineData(new int[] { 1 }, 1, 1, null, 1, 1, true, true)]
        [InlineData(new int[] { 1 }, 1, 1, null, 1, 2, true, true)]
        [InlineData(new int[] { 1 }, 1, 1, null, 1, 2, false, false)]
        [InlineData(new int[] { 2 }, 1, 1, null, 2, 2, false, false)]
        [InlineData(new int[] { 2 }, 1, 1, null, 2, 2, true, false)]
        [InlineData(new int[] { }, 1, 1, 1, 1, 1, false, false)]
        [InlineData(new int[] { }, 1, 1, 1, 1, 1, true, true)]
        [InlineData(new int[] { }, 1, 1, 1, 1, 2, true, true)]
        [InlineData(new int[] { }, 1, 1, 1, 1, 2, false, false)]
        [InlineData(new int[] { }, 1, 1, 1, 2, 2, false, false)]
        [InlineData(new int[] { }, 1, 1, 1, 2, 2, true, false)]
        [InlineData(new int[] { 1 }, 1, 1, 1, 1, 1, false, true)]
        [InlineData(new int[] { 1 }, 1, 1, 1, 1, 1, true, true)]
        [InlineData(new int[] { 1 }, 1, 1, 1, 1, 2, true, true)]
        [InlineData(new int[] { 1 }, 1, 1, 1, 1, 2, false, true)]
        [InlineData(new int[] { 2 }, 1, 1, 1, 1, 1, false, false)]
        [InlineData(new int[] { 2 }, 1, 1, 1, 1, 1, true, true)]
        [InlineData(new int[] { 2 }, 1, 1, 1, 1, 2, true, true)]
        [InlineData(new int[] { 2 }, 1, 1, 1, 1, 2, false, false)]
        [InlineData(new int[] { 1, 2 }, 1, 1, 1, 1, 1, false, true)]
        [InlineData(new int[] { 1, 2 }, 1, 1, 1, 1, 1, true, true)]
        [InlineData(new int[] { 1, 2 }, 1, 1, 1, 1, 2, true, true)]
        [InlineData(new int[] { 1, 2 }, 1, 1, 1, 1, 2, false, true)]
        public void IsAuthorizedAsCreator(IEnumerable<int> customerUnits, int callingCustomerId, int userId, int? orderUnit, int orderCustomerId, int creatorId, bool hasCorrectAdminRole, bool expected)
        {
            var orderGroup = MockOrderGroups.First();
            orderGroup.CustomerOrganisationId = orderCustomerId;
            orderGroup.CustomerUnitId = orderUnit;
            orderGroup.CreatedBy = creatorId;
            Assert.Equal(expected, orderGroup.IsAuthorizedAsCreator(customerUnits, callingCustomerId, userId, hasCorrectAdminRole));
        }

    }

}
