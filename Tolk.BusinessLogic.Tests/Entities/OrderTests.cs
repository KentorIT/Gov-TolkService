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
            var mockLanguages = MockEntities.MockLanguages();
            var mockRankings = MockEntities.MockRankings();
            var mockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers());
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
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(7)]
        public void DeliverRequisition_Pass(int mockOrderId)
        {
            var order = MockOrders.Where(o => o.OrderId == mockOrderId).Single();
            order.DeliverRequisition();
            Assert.Equal(OrderStatus.Delivered, order.Status);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(6)]
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
        [InlineData(OrderStatus.Requested, "2019-01-25 10:00:00", 1, null, 101, null)]
        [InlineData(OrderStatus.RequestResponded, "2019-01-25 10:00:00", 1, null, 101, null)]
        [InlineData(OrderStatus.ResponseAccepted, "2019-01-25 10:00:00", 1, null, 101, null)]
        [InlineData(OrderStatus.Delivered, "2019-01-25 10:00:00", 1, null, 101, null)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, "2019-01-25 10:00:00", 1, null, 101, null)]
        public void ChangeContactPerson_Valid(OrderStatus currentStatus, string changedAt, int userId, int? impersonatorId, int contactPersonId, int? prevContactPersonId)
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
                Requests = new List<Request>()
                {
                    new Request() { Status = RequestStatus.Approved }
                }
            };
            if (conditionalStatus.HasValue)
            {
                order.Status = conditionalStatus.Value;
            }
            order.ChangeContactPerson(changedAtDateTime, userId, impersonatorId, contactPersonId);
            Assert.Equal(contactPersonId, order.ContactPersonId);
            Assert.Equal(changedAtDateTime, order.OrderContactPersonHistory.OrderBy(ch => ch.ChangedAt).Last().ChangedAt);
            Assert.Equal(userId, order.OrderContactPersonHistory.OrderBy(ch => ch.ChangedAt).Last().ChangedBy);
            Assert.Equal(impersonatorId, order.OrderContactPersonHistory.OrderBy(ch => ch.ChangedAt).Last().ImpersonatingChangeUserId);
            Assert.Equal(prevContactPersonId, order.OrderContactPersonHistory.OrderBy(ch => ch.ChangedAt).Last().PreviousContactPersonId);
        }

        [Theory]
        [InlineData(OrderStatus.CancelledByCreator)]
        [InlineData(OrderStatus.CancelledByBroker)]
        [InlineData(OrderStatus.NoBrokerAcceptedOrder)]
        [InlineData(OrderStatus.ResponseNotAnsweredByCreator)]
        public void ChangeContactPerson_Invalid(OrderStatus invalidStatus)
        {
            var order = new Order(MockOrders.First())
            {
                Status = invalidStatus
            };
            Assert.Throws<InvalidOperationException>(() => order.ChangeContactPerson(DateTimeOffset.Now, 1, null, 1));
        }
    }
}
