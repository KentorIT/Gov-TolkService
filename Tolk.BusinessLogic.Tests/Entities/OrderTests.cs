using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Xunit;

namespace Tolk.BusinessLogic.Tests.Entities
{
    public class OrderTests
    {
        private readonly Order[] MockOrders;

        public OrderTests()
        {
            var mockLanguages = MockEntities.MockLanguages;
            var mockRankings = MockEntities.MockRankings;
            var mockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers);
            MockOrders = MockEntities.MockOrders(mockLanguages, mockRankings, mockCustomerUsers);
        }

        [Theory]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.No, 1, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldBeApproved, 1, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldNotBeApproved, 1, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestResponded, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestResponded, AllowExceedingTravelCost.No, 1, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestResponded, AllowExceedingTravelCost.YesShouldBeApproved, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestResponded, AllowExceedingTravelCost.YesShouldBeApproved, 1, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestResponded, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestResponded, AllowExceedingTravelCost.YesShouldNotBeApproved, 1, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.No, 1, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldBeApproved, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldBeApproved, 1, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldNotBeApproved, 1, true, RequestStatus.Approved)]
        public void SetResponseAccepted_Valid(OrderStatus status, AllowExceedingTravelCost allowExceedingTravelCost, int? replacingOrderId, bool requestExists, RequestStatus? requestStatus)
        {
            List<Request> requests = null;
            if (requestExists)
            {
                requests = new List<Request>()
                {
                    new Request() { Status = requestStatus.Value }
                };
            }
            var order = new Order(MockOrders.First())
            {
                Status = status,
                AllowExceedingTravelCost = allowExceedingTravelCost,
                ReplacingOrderId = replacingOrderId,
                Requests = requests
            };
            order.Status = OrderStatus.ResponseAccepted;
            Assert.Equal(OrderStatus.ResponseAccepted, order.Status);
        }

        [Theory]
        // Illegal status preconditions
        [InlineData(OrderStatus.Delivered, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.CancelledByCreator, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.NoBrokerAcceptedOrder, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.CancelledByBroker, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.ResponseNotAnsweredByCreator, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.Delivered, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.CancelledByCreator, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.NoBrokerAcceptedOrder, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.CancelledByBroker, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.ResponseNotAnsweredByCreator, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved)]
        //ExceedingTravelCosts must be accepted
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldBeApproved, null, true, RequestStatus.Approved)]
        // No requests
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.No, null, false, null)]
        [InlineData(OrderStatus.RequestResponded, AllowExceedingTravelCost.No, null, false, null)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.No, null, false, null)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldNotBeApproved, null, false, null)]
        [InlineData(OrderStatus.RequestResponded, AllowExceedingTravelCost.YesShouldNotBeApproved, null, false, null)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldNotBeApproved, null, false, null)]
        public void SetResponseAccepted_Invalid(OrderStatus status, AllowExceedingTravelCost allowExceedingTravelCost, int? replacingOrderId, bool requestExists, RequestStatus? requestStatus)
        {
            List<Request> requests = null;
            if (requestExists)
            {
                requests = new List<Request>()
                {
                    new Request() { Status = requestStatus.Value }
                };
            }
            else
            {
                requests = new List<Request>();
            }
            var order = new Order(MockOrders.First())
            {
                Status = status,
                AllowExceedingTravelCost = allowExceedingTravelCost,
                ReplacingOrderId = replacingOrderId,
                Requests = requests
            };
            Assert.Throws<InvalidOperationException>(() => order.Status = OrderStatus.ResponseAccepted);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(6)]
        [InlineData(8)]
        public void DeliverRequisition_Pass(int mockOrderId)
        {
            var order = MockOrders.Where(o => o.OrderId == mockOrderId).Single();
            order.DeliverRequisition();
            Assert.Equal(OrderStatus.Delivered, order.Status);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(7)]
        public void DeliverRequisition_Fail(int mockOrderId)
        {
            Assert.Throws<InvalidOperationException>(() => 
                MockOrders.Where(o => o.OrderId == mockOrderId).Single().DeliverRequisition());
        }

        [Fact]
        public void MakeCopy()
        {

        }

        [Theory]
        [InlineData(OrderStatus.Requested, "2019-01-25 10:00:00", 1, null, null)]
        [InlineData(OrderStatus.RequestResponded, "2019-01-25 10:00:00", 1, null, null)]
        [InlineData(OrderStatus.ResponseAccepted, "2019-01-25 10:00:00", 1, null, null)]
        [InlineData(OrderStatus.Delivered, "2019-01-25 10:00:00", 1, null, null)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, "2019-01-25 10:00:00", 1, null, null)]
        public void ChangeContactPerson_Valid(OrderStatus currentStatus, string changedAt, int userId, int? impersonatorId, int? prevContactPersonId)
        {
            var changedAtDateTime = DateTime.Parse(changedAt);

            var conditionalStatus = currentStatus == OrderStatus.ResponseAccepted ? (OrderStatus?)OrderStatus.ResponseAccepted : null;
            if (conditionalStatus.HasValue)
            {
                currentStatus = OrderStatus.Requested;
            }
            var order = new Order(MockOrders.First())
            {
                Status = currentStatus,
                ContactPersonId = prevContactPersonId ?? null,
                OrderContactPersonHistory = new List<OrderContactPersonHistory>(),
                AllowExceedingTravelCost = AllowExceedingTravelCost.No,
                CustomerOrganisationId = 1,
                Requests = new List<Request>()
                {
                    new Request() { Status = RequestStatus.Approved }
                }
            };
            var newContactPerson = new AspNetUser("", "", "", "")
            {
                CustomerOrganisationId = 1,
            };
            if (conditionalStatus.HasValue)
            {
                order.Status = conditionalStatus.Value;
            }
            order.ChangeContactPerson(changedAtDateTime, userId, impersonatorId, newContactPerson);
            Assert.Equal(newContactPerson.Id, order.ContactPersonId);
            Assert.Equal(changedAtDateTime, order.OrderContactPersonHistory.OrderBy(ch => ch.ChangedAt).Last().ChangedAt);
            Assert.Equal(userId, order.OrderContactPersonHistory.OrderBy(ch => ch.ChangedAt).Last().ChangedBy);
            Assert.Equal(impersonatorId, order.OrderContactPersonHistory.OrderBy(ch => ch.ChangedAt).Last().ImpersonatingChangeUserId);
            Assert.Equal(prevContactPersonId, order.OrderContactPersonHistory.OrderBy(ch => ch.ChangedAt).Last().PreviousContactPersonId);
        }

        [Theory]
        // Invalid status
        [InlineData(OrderStatus.CancelledByCreator, 1)]
        [InlineData(OrderStatus.CancelledByBroker, 1)]
        [InlineData(OrderStatus.NoBrokerAcceptedOrder, 1)]
        [InlineData(OrderStatus.ResponseNotAnsweredByCreator, 1)]
        // Invalid CustomerOrganization
        [InlineData(OrderStatus.AwaitingDeadlineFromCustomer, 2)]
        [InlineData(OrderStatus.Delivered, 2)]
        [InlineData(OrderStatus.DeliveryAccepted, 2)]
        [InlineData(OrderStatus.NoDeadlineFromCustomer, 2)]
        [InlineData(OrderStatus.Requested, 2)]
        [InlineData(OrderStatus.RequestResponded, 2)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, 2)]
        [InlineData(OrderStatus.ToBeProcessedByCustomer, 2)]
        public void ChangeContactPerson_Invalid(OrderStatus invalidStatus, int ContactPersonCustomerOrganizationId)
        {
            var order = new Order(MockOrders.First())
            {
                Status = invalidStatus,
                CustomerOrganisationId = 1,
            };
            var newContactPerson = new AspNetUser("", "", "", "")
            {
                CustomerOrganisationId = ContactPersonCustomerOrganizationId,
            };
            Assert.Throws<InvalidOperationException>(() => order.ChangeContactPerson(DateTimeOffset.Now, 1, null, newContactPerson));
        }
    }
}
