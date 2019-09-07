using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Tolk.BusinessLogic.Utilities;
using Xunit;

namespace Tolk.BusinessLogic.Tests.Helpers
{
    public class EntityExtensionsTests
    {
        private readonly Order[] MockOrders;
        private readonly AspNetUser[] MockCustomerUsers;

        public EntityExtensionsTests()
        {
            var mockLanguages = MockEntities.MockLanguages;
            var mockRankings = MockEntities.MockRankings;
            MockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers);
            MockOrders = MockEntities.MockOrders(mockLanguages, mockRankings, MockCustomerUsers);
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 4)]
        [InlineData(3, 3)]
        [InlineData(4, 1)]
        public void CustomerOrdersWithUser(int userId, int expectedCount)
        {
            var customer = MockCustomerUsers.Single(c => c.Id == userId);
            MockOrders.AsQueryable().CustomerOrders(customer.CustomerOrganisationId.Value, customer.Id, Enumerable.Empty<int>())
                .Should().HaveCount(expectedCount);
        }

        [Theory]
        [InlineData(1, 3)]
        [InlineData(2, 4)]
        [InlineData(3, 3)]
        [InlineData(4, 3)]
        public void CustomerOrders(int userId, int expectedCount)
        {
            var customer = MockCustomerUsers.Single(c => c.Id == userId);
            MockOrders.AsQueryable().CustomerOrders(customer.CustomerOrganisationId.Value, customer.Id, Enumerable.Empty<int>(), true)
                .Should().HaveCount(expectedCount);
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 4)]
        [InlineData(3, 3)]
        [InlineData(4, 2)]
        public void CustomerOrdersOtherUserWithContacts(int userId, int expectedCount)
        {
            var customer = MockCustomerUsers.Single(c => c.Id == userId);
            MockOrders.AsQueryable().CustomerOrders(customer.CustomerOrganisationId.Value, customer.Id, Enumerable.Empty<int>(), includeContact: true)
                .Should().HaveCount(expectedCount);
        }

        [Theory]
        [InlineData(5, new[] { 1, 2 }, 3)]
        [InlineData(5, new[] { 1 }, 2)]
        [InlineData(5, null, 1)]
        [InlineData(6, new[] { 1, 2 }, 2)]
        [InlineData(6, new[] { 1 }, 1)]
        [InlineData(6, null, 0)]
        public void CustomerOrdersWithUnits(int userId, IEnumerable<int> customerUnits, int expectedCount)
        {
            var customer = MockCustomerUsers.Single(c => c.Id == userId);
            MockOrders.AsQueryable().CustomerOrders(customer.CustomerOrganisationId.Value, customer.Id, customerUnits ?? Enumerable.Empty<int>())
                .Should().HaveCount(expectedCount);
        }
    }
}
