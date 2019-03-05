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
            MockOrders = MockEntities.MockOrders(mockLanguages, mockRankings);
        }

        [Theory]
        [InlineData(OrderStatus.Requested, false, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.Requested, false, 1, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.Requested, true, 1, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestResponded, false, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestResponded, false, 1, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestResponded, true, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestResponded, true, 1, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, false, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, false, 1, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, true, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, true, 1, true, RequestStatus.Approved)]
        public void SetResponseAccepted_Valid(OrderStatus status, bool allowMoreThanTwoHourTravelTime, int? replacingOrderId, bool requestExists, RequestStatus? requestStatus)
        {
            List<Request> requests = null;
            if (requestExists)
            {
                requests = new List<Request>()
                {
                    new Request() { Status = requestStatus.Value }
                };
            }
            var order = new Order()
            {
                Status = status,
                AllowMoreThanTwoHoursTravelTime = allowMoreThanTwoHourTravelTime,
                ReplacingOrderId = replacingOrderId,
                Requests = requests
            };
            order.Status = OrderStatus.ResponseAccepted;
            Assert.Equal(OrderStatus.ResponseAccepted, order.Status);
        }

        [Theory]
        // Illegal status preconditions
        [InlineData(OrderStatus.Delivered, false, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.CancelledByCreator, false, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.NoBrokerAcceptedOrder, false, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.CancelledByBroker, false, null, true, RequestStatus.Approved)]
        [InlineData(OrderStatus.ResponseNotAnsweredByCreator, false, null, true, RequestStatus.Approved)]
        // Over two hours travel time allowed
        [InlineData(OrderStatus.Requested, true, null, true, RequestStatus.Approved)]
        // No requests
        [InlineData(OrderStatus.Requested, false, null, false, null)]
        [InlineData(OrderStatus.RequestResponded, false, null, false, null)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, false, null, false, null)]
        public void SetResponseAccepted_Invalid(OrderStatus status, bool allowMoreThanTwoHourTravelTime, int? replacingOrderId, bool requestExists, RequestStatus? requestStatus)
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
            var order = new Order()
            {
                Status = status,
                AllowMoreThanTwoHoursTravelTime = allowMoreThanTwoHourTravelTime,
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
            var order = new Order
            {
                Status = currentStatus,
                ContactPersonId = prevContactPersonId ?? null,
                OrderContactPersonHistory = new List<OrderContactPersonHistory>(),
                AllowMoreThanTwoHoursTravelTime = false,
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
            var order = new Order
            {
                Status = invalidStatus
            };
            Assert.Throws<InvalidOperationException>(() => order.ChangeContactPerson(DateTimeOffset.Now, 1, null, 1));
        }
    }
}
