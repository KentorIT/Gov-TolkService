using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Tolk.BusinessLogic.Utilities;
using Xunit;

namespace Tolk.BusinessLogic.Tests.Entities
{
    public class OrderTests
    {
        private readonly Order[] MockOrders;
        private readonly Order[] MockFlexibleOrders;

        public OrderTests()
        {
            var mockLanguages = MockEntities.MockLanguages;
            var mockRankings = MockEntities.MockRankings;
            var mockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers);
            MockOrders = MockEntities.MockOrders(mockLanguages, mockRankings, mockCustomerUsers);
            MockFlexibleOrders = MockEntities.MockFlexibleOrders(mockLanguages, mockRankings, mockCustomerUsers);
        }

        [Theory]
        //no AllowExceedingTravelCost
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved, InterpreterLocation.OnSite)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved, InterpreterLocation.OffSiteVideo)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved, InterpreterLocation.OffSiteDesignatedLocation)]

        //AllowExceedingTravelCost but no need to approve
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSiteVideo)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSiteDesignatedLocation)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OnSite)]
        //although YesShouldBeApproved has been choosen it should be auto-accepted since the request answer has offsite interpreterlocation
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSiteVideo)]
        //if travelcost is 0 it should be auto-accepted even if YesShouldBeApproved and onsite
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OnSite, 0)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSiteDesignatedLocation, 0)]

        //replacementorder
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.No, 1, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OffSiteVideo)]
        //if replacement order it should be possible to accept also when travelcost > 0
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OffSiteDesignatedLocation, 200)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OffSiteDesignatedLocation, 0)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OnSite, 200)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OnSite, 0)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldNotBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]

        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.No, 1, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.YesShouldBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.YesShouldBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.YesShouldNotBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved, InterpreterLocation.OffSiteVideo)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.No, 1, true, RequestStatus.Approved, InterpreterLocation.OffSiteVideo)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.YesShouldBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSiteVideo)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.YesShouldBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OffSiteVideo)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSiteVideo)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.YesShouldNotBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OffSiteVideo)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved, InterpreterLocation.OnSite)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.No, 1, true, RequestStatus.Approved, InterpreterLocation.OnSite)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.YesShouldBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OnSite)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.YesShouldBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OnSite)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OnSite)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.YesShouldNotBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OnSite)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved, InterpreterLocation.OffSiteDesignatedLocation)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.No, 1, true, RequestStatus.Approved, InterpreterLocation.OffSiteDesignatedLocation)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.YesShouldBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSiteDesignatedLocation)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.YesShouldBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OffSiteDesignatedLocation)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSiteDesignatedLocation)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.YesShouldNotBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OffSiteDesignatedLocation)]

        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.No, 1, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldNotBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved, InterpreterLocation.OffSiteVideo)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.No, 1, true, RequestStatus.Approved, InterpreterLocation.OffSiteVideo)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSiteVideo)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OffSiteVideo)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSiteVideo)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldNotBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OffSiteVideo)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved, InterpreterLocation.OnSite)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.No, 1, true, RequestStatus.Approved, InterpreterLocation.OnSite)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OnSite)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OnSite)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OnSite)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldNotBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OnSite)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved, InterpreterLocation.OffSiteDesignatedLocation)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.No, 1, true, RequestStatus.Approved, InterpreterLocation.OffSiteDesignatedLocation)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSiteDesignatedLocation)]

        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OffSiteDesignatedLocation)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSiteDesignatedLocation)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldNotBeApproved, 1, true, RequestStatus.Approved, InterpreterLocation.OffSiteDesignatedLocation)]
        public void SetResponseAccepted_Valid(OrderStatus status, AllowExceedingTravelCost allowExceedingTravelCost, int? replacingOrderId, bool requestExists, RequestStatus? requestStatus, InterpreterLocation? interpreterLocation, decimal travelcost = 0)
        {
            List<Request> requests = null;
            if (requestExists)
            {
                requests = new List<Request>()
                {
                    new Request() { Status = requestStatus.Value, InterpreterLocation = (int?)interpreterLocation.Value, PriceRows = new List<RequestPriceRow>() }
                };
            }
            if (travelcost > 0)
            {
                requests.First().PriceRows.Add(new RequestPriceRow { Price = travelcost, StartAt = DateTime.Now, EndAt = DateTime.Now, PriceRowType = PriceRowType.TravelCost });
            }

            var order = new Order(MockOrders.First())
            {
                Status = status,
                AllowExceedingTravelCost = allowExceedingTravelCost,
                ReplacingOrderId = replacingOrderId,
                Requests = requests
            };
            order.Requests.First().Order = order;
            order.Status = OrderStatus.ResponseAccepted;
            Assert.Equal(OrderStatus.ResponseAccepted, order.Status);
        }

        [Theory]
        // Illegal status preconditions
        [InlineData(OrderStatus.Delivered, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.CancelledByCreator, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.NoBrokerAcceptedOrder, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.CancelledByBroker, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.ResponseNotAnsweredByCreator, AllowExceedingTravelCost.No, null, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.Delivered, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone, 200)]
        [InlineData(OrderStatus.CancelledByCreator, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone, 200)]
        [InlineData(OrderStatus.NoBrokerAcceptedOrder, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.CancelledByBroker, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        [InlineData(OrderStatus.ResponseNotAnsweredByCreator, AllowExceedingTravelCost.YesShouldNotBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSitePhone)]
        //ExceedingTravelCosts must be accepted if YesShouldBeApproved and OnSite or OffSiteDesignatedLocation and travelcost > 0
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OnSite, 200)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldBeApproved, null, true, RequestStatus.Approved, InterpreterLocation.OffSiteDesignatedLocation, 400)]
        // No requests
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.No, null, false, null, null)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.No, null, false, null, null)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.No, null, false, null, null)]
        [InlineData(OrderStatus.Requested, AllowExceedingTravelCost.YesShouldNotBeApproved, null, false, null, null)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, AllowExceedingTravelCost.YesShouldNotBeApproved, null, false, null, null)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, AllowExceedingTravelCost.YesShouldNotBeApproved, null, false, null, null)]
        public void SetResponseAccepted_Invalid(OrderStatus status, AllowExceedingTravelCost allowExceedingTravelCost, int? replacingOrderId, bool requestExists, RequestStatus? requestStatus, InterpreterLocation? interpreterLocation, decimal travelcost = 0)
        {
            List<Request> requests = null;
            if (requestExists)
            {
                requests = new List<Request>() { new Request() { Status = requestStatus.Value, InterpreterLocation = (int?)interpreterLocation.Value, PriceRows = new List<RequestPriceRow>() } };
                if (travelcost > 0)
                {
                    requests.First().PriceRows.Add(new RequestPriceRow { Price = travelcost, StartAt = DateTime.Now, EndAt = DateTime.Now, PriceRowType = PriceRowType.TravelCost });
                }
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
            if (order.Requests.Any())
            {
                order.Requests.First().Order = order;
            }
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
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, "2019-01-25 10:00:00", 1, null, null)]
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
                ContactPersonId = prevContactPersonId,
                OrderChangeLogEntries = new List<OrderChangeLogEntry>(),
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
            order.Requests.First().Order = order;
            if (conditionalStatus.HasValue)
            {
                order.Status = conditionalStatus.Value;
            }
            order.ChangeContactPerson(changedAtDateTime, userId, impersonatorId, newContactPerson);
            var orderChangeContactHistory = order.OrderChangeLogEntries.Where(oc => oc.OrderChangeLogType == OrderChangeLogType.ContactPerson).OrderBy(ch => ch.LoggedAt).Last();
            Assert.Equal(newContactPerson.Id, order.ContactPersonUser.Id);
            Assert.Equal(changedAtDateTime, orderChangeContactHistory.LoggedAt);
            Assert.Equal(userId, orderChangeContactHistory.UpdatedByUserId);
            Assert.Equal(impersonatorId, orderChangeContactHistory.UpdatedByImpersonatorId);
            Assert.Equal(prevContactPersonId, orderChangeContactHistory.OrderContactPersonHistory.PreviousContactPersonId);
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
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval, 2)]
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

        [Theory]
        [InlineData(null, "2019-01-01", false)]
        [InlineData("2019-01-02", "2019-01-01", true)]
        [InlineData(null, "2019-01-01", true)]
        [InlineData("2019-01-02", "2019-01-01", false)]
        public void CreateRequestValid(string expiry, string now, bool isTerminalRequest)
        {
            RequestExpiryResponse response = new RequestExpiryResponse { ExpiryAt = expiry != null ? (DateTime?)DateTime.Parse(expiry) : null, RequestAnswerRuleType = RequestAnswerRuleType.ResponseSetByCustomer };
            var nowDate = DateTime.Parse(now);
            var order = MockOrders.Last();
            var request = order.CreateRequest(MockEntities.MockRankingsWithQuarantines.AsQueryable(), response, nowDate, isTerminalRequest);
            Assert.Equal(RequestStatus.Created, request.Status);
        }

        [Theory]
        [InlineData("2019-01-01", "2019-01-02", false)]
        [InlineData("2019-01-01", "2019-01-02", true)]
        public void CreateRequestInValid(string expiry, string now, bool isTerminalRequest)
        {
            RequestExpiryResponse response = new RequestExpiryResponse { ExpiryAt = expiry != null ? (DateTime?)DateTime.Parse(expiry) : null, RequestAnswerRuleType = RequestAnswerRuleType.ResponseSetByCustomer };
            var nowDate = DateTime.Parse(now);
            var order = MockOrders.Last();
            Assert.Throws<InvalidOperationException>(() => order.CreateRequest(MockEntities.MockRankingsWithQuarantines.AsQueryable(), response, nowDate, isTerminalRequest));
        }

        [Theory]
        [InlineData(1, 1, 2, OrderStatus.Requested)]
        [InlineData(2, 1, 1, OrderStatus.Requested)]
        [InlineData(1, 2, 1, OrderStatus.Requested)]
        [InlineData(3, 1, 3, OrderStatus.Requested)]
        [InlineData(2, 3, 2, OrderStatus.NoBrokerAcceptedOrder)]
        public void CreateRequestWithQuarantine(int customer, int region, int requests, OrderStatus expectedStatus)
        {
            var order = MockOrders.Last();
            order.CustomerOrganisationId = customer;
            order.RegionId = region;
            order.CreateRequest(MockEntities.MockRankingsWithQuarantines.Where(r => r.RegionId == region).AsQueryable(), new RequestExpiryResponse { RequestAnswerRuleType = RequestAnswerRuleType.ResponseSetByCustomer }, new DateTimeOffset(2018, 09, 07, 0, 0, 0, new TimeSpan(02, 00, 00)), false);
            Assert.Equal(requests, order.Requests.Count);
            Assert.Equal(expectedStatus, order.Status);
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
            var order = MockOrders.Last();
            order.CustomerOrganisationId = orderCustomerId;
            order.CustomerUnitId = orderUnit;
            order.CreatedBy = creatorId;
            Assert.Equal(expected, order.IsAuthorizedAsCreator(customerUnits, callingCustomerId, userId, hasCorrectAdminRole));
        }

        [Theory]
        [InlineData(new int[] { }, 1, 2, null, 1, null, false)]
        [InlineData(new int[] { }, 1, 2, null, 1, 2, true)]
        [InlineData(new int[] { }, 1, 2, 2, 1, null, false)]
        [InlineData(new int[] { }, 1, 2, 2, 1, 2, true)]
        public void IsAuthorizedAsCreatorOrContact(IEnumerable<int> customerUnits, int callingCustomerId, int userId, int? orderUnit, int creatorId, int? contactPersonId, bool expected)
        {
            var order = MockOrders.Last();
            order.CustomerOrganisationId = callingCustomerId;
            order.CustomerUnitId = orderUnit;
            order.CreatedBy = creatorId;
            order.ContactPersonId = contactPersonId;
            Assert.Equal(expected, order.IsAuthorizedAsCreatorOrContact(customerUnits, callingCustomerId, userId, false));
        }

        [Fact]
        public void ConfirmNoAnswer()
        {
            var order = new Order(MockOrders.First())
            {
                Status = OrderStatus.NoBrokerAcceptedOrder,
                Requests = new List<Request>()
                {
                    new Request() { Status = RequestStatus.DeniedByTimeLimit }
                },
                OrderStatusConfirmations = new List<OrderStatusConfirmation>()
            };
            order.ConfirmNoAnswer(DateTimeOffset.Now, 1, null);
            Assert.Equal(1, order.OrderStatusConfirmations.Count(o => o.OrderStatus == OrderStatus.NoBrokerAcceptedOrder));
        }

        // Invalid order status
        [Theory]
        [InlineData(OrderStatus.AwaitingDeadlineFromCustomer)]
        [InlineData(OrderStatus.CancelledByBroker)]
        [InlineData(OrderStatus.CancelledByCreator)]
        [InlineData(OrderStatus.Delivered)]
        [InlineData(OrderStatus.DeliveryAccepted)]
        [InlineData(OrderStatus.GroupAwaitingPartialResponse)]
        [InlineData(OrderStatus.RequestAwaitingPartialAccept)]
        [InlineData(OrderStatus.NoDeadlineFromCustomer)]
        [InlineData(OrderStatus.Requested)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter)]
        [InlineData(OrderStatus.ResponseAccepted)]
        [InlineData(OrderStatus.ResponseNotAnsweredByCreator)]
        public void ConfirmNoAnswer_Invalid(OrderStatus status)
        {
            var order = new Order(MockOrders.First())
            {
                Status = OrderStatus.Requested,
                AllowExceedingTravelCost = AllowExceedingTravelCost.No,
                Requests = new List<Request>()
                {
                    new Request() { Status = status == OrderStatus.ResponseAccepted ? RequestStatus.Approved : RequestStatus.Created }
                },
                OrderStatusConfirmations = new List<OrderStatusConfirmation>()
            };
            order.Requests.First().Order = order;
            order.Status = status;
            Assert.Throws<InvalidOperationException>(() => order.ConfirmNoAnswer(DateTimeOffset.Now, 1, null));
        }

        [Theory]
        [InlineData("2023-03-17 10:00:00", "2023-03-17 17:00:00", "2023-03-17 10:00:00", "02:00", true)]
        [InlineData("2023-03-17 10:00:00", "2023-03-17 17:00:00", "2023-03-17 15:00:00", "02:00", true)]
        [InlineData("2023-03-17 10:00:00", "2023-03-17 17:00:00", "2023-03-17 12:15:00", "02:00", true)]
        [InlineData("2023-03-17 10:00:00", "2023-03-17 17:00:00", "2023-03-17 09:59:00", "02:00", false)]
        [InlineData("2023-03-17 10:00:00", "2023-03-17 14:00:00", "2023-03-17 15:00:00", "02:00", false)]
        [InlineData("2023-03-17 10:00:00", "2023-03-17 14:00:00", "2023-03-17 12:01:00", "02:00", false)]
        [InlineData("2023-03-17 10:00:00", "2023-03-17 14:00:00", "2023-03-18 12:00:00", "02:00", false)]
        [InlineData("2023-03-17 10:00:00", "2023-03-17 12:00:00", "2023-03-17 15:00:00", null, false)]
        [InlineData("2023-03-17 10:00:00", "2023-03-17 17:00:00", null, "02:00", false)]
        [InlineData("2023-03-17 10:00:00", "2023-03-17 17:00:00", null, null, true)]

        public void IsValidRespondedStartAt(string orderStartAt, string orderEndAt, string respondedStartAt, string expectedLength, bool result)
        {
            var orderStartAtOffset = DateTimeOffset.Parse(orderStartAt);
            var orderEndAtOffset = DateTimeOffset.Parse(orderEndAt);
            DateTimeOffset? respondedStartAtOffset = !string.IsNullOrEmpty(respondedStartAt) ? DateTimeOffset.Parse(respondedStartAt) : null;
            TimeSpan? expectedLengthTimeSpan = !string.IsNullOrEmpty(expectedLength) ? TimeSpan.Parse(expectedLength) : null;
            var order = new Order(MockOrders.First())
            {
                StartAt = orderStartAtOffset,
                EndAt = orderEndAtOffset,
                ExpectedLength = expectedLengthTimeSpan,
            };
            Assert.Equal(result, order.IsValidRespondedStartAt(respondedStartAtOffset));
        }

        [Fact]
        public void ConfirmResponseNotAnswered()
        {
            var order = new Order(MockOrders.First())
            {
                Status = OrderStatus.ResponseNotAnsweredByCreator,
                Requests = new List<Request>()
                {
                    new Request() { Status = RequestStatus.DeniedByTimeLimit }
                },
                OrderStatusConfirmations = new List<OrderStatusConfirmation>()
            };
            order.ConfirmResponseNotAnswered(DateTimeOffset.Now, 1, null);
            Assert.Equal(1, order.OrderStatusConfirmations.Count(o => o.OrderStatus == OrderStatus.ResponseNotAnsweredByCreator));
        }

        // Invalid order status
        [Theory]
        [InlineData(OrderStatus.AwaitingDeadlineFromCustomer)]
        [InlineData(OrderStatus.CancelledByBroker)]
        [InlineData(OrderStatus.CancelledByCreator)]
        [InlineData(OrderStatus.Delivered)]
        [InlineData(OrderStatus.DeliveryAccepted)]
        [InlineData(OrderStatus.GroupAwaitingPartialResponse)]
        [InlineData(OrderStatus.RequestAwaitingPartialAccept)]
        [InlineData(OrderStatus.NoDeadlineFromCustomer)]
        [InlineData(OrderStatus.Requested)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter)]
        [InlineData(OrderStatus.ResponseAccepted)]
        [InlineData(OrderStatus.NoBrokerAcceptedOrder)]
        public void ConfirmResponseNotAnswered_Invalid(OrderStatus status)
        {
            var order = new Order(MockOrders.First())
            {
                Status = OrderStatus.Requested,
                AllowExceedingTravelCost = AllowExceedingTravelCost.No,
                Requests = new List<Request>()
                {
                    new Request() { Status = status == OrderStatus.ResponseAccepted ? RequestStatus.Approved : RequestStatus.Created }
                },
                OrderStatusConfirmations = new List<OrderStatusConfirmation>()
            };
            order.Requests.First().Order = order;
            order.Status = status;
            Assert.Throws<InvalidOperationException>(() => order.ConfirmResponseNotAnswered(DateTimeOffset.Now, 1, null));
        }

        [Fact]
        public void ConfirmResponseNotAnswered_AlreadyConfirmed()
        {
            var order = new Order(MockOrders.First())
            {
                Status = OrderStatus.ResponseNotAnsweredByCreator,
                Requests = new List<Request>()
                {
                    new Request() { Status = RequestStatus.DeniedByTimeLimit }
                },
                OrderStatusConfirmations = new List<OrderStatusConfirmation>()
            };
            order.ConfirmResponseNotAnswered(DateTimeOffset.Now, 1, null);
            Assert.Equal(1, order.OrderStatusConfirmations.Count(o => o.OrderStatus == OrderStatus.ResponseNotAnsweredByCreator));
            Assert.Throws<InvalidOperationException>(() => order.ConfirmResponseNotAnswered(DateTimeOffset.Now, 2, null));
        }

        [Theory]
        [InlineData("New Desc", 1, true, true, true)]
        [InlineData("New Desc", 1, true, true, false)]
        [InlineData(null, 1, false, true, true)]
        [InlineData(null, 1, false, true, false)]
        [InlineData("New Desc", null, true, false, false)]
        public void Update_Valid(string description, int? updatedAttachment, bool orderFieldsUpdated, bool attachmentChanged, bool attachmentRemoved)
        {
            var order = new Order(MockOrders.First())
            {
                Status = OrderStatus.Requested,
                AllowExceedingTravelCost = AllowExceedingTravelCost.No,
                Requests = new List<Request>()
                {
                    new Request() { Status = RequestStatus.Approved }
                },
                Attachments = attachmentRemoved ? new List<OrderAttachment>() { new OrderAttachment { AttachmentId = 1 } } : new List<OrderAttachment>(),
                OrderChangeLogEntries = new List<OrderChangeLogEntry>(),
            };
            order.Requests.First().Order = order;
            order.InterpreterLocations.Add(new OrderInterpreterLocation { Rank = 1, InterpreterLocation = InterpreterLocation.OffSitePhone });
            order.Status = OrderStatus.ResponseAccepted;
            var updatedAttachments = updatedAttachment.HasValue ? new[] { updatedAttachment.Value } : null;
            order.Update(new ChangeOrderModel
            {
                UpdatedAt = DateTimeOffset.Now,
                UpdatedBy = 1,
                Description = description,
                OrderChangeLogType = (orderFieldsUpdated && attachmentChanged) ? OrderChangeLogType.AttachmentAndOrderInformationFields : attachmentChanged ? OrderChangeLogType.Attachment : OrderChangeLogType.OrderInformationFields,
                Attachments = updatedAttachments,
                BrokerId = 1,
                SelectedInterpreterLocation = InterpreterLocation.OffSitePhone
            });
            Assert.Equal(orderFieldsUpdated || attachmentChanged, order.OrderChangeLogEntries.Count > 0);
            Assert.Equal(orderFieldsUpdated, order.OrderChangeLogEntries.Single().OrderHistories.Count > 0);
            Assert.Equal(attachmentRemoved, order.OrderChangeLogEntries.Single().OrderAttachmentHistoryEntries.Count > 0);
        }

        [Theory]
        [InlineData(OrderStatus.AwaitingDeadlineFromCustomer)]
        [InlineData(OrderStatus.CancelledByBroker)]
        [InlineData(OrderStatus.CancelledByCreator)]
        [InlineData(OrderStatus.Delivered)]
        [InlineData(OrderStatus.DeliveryAccepted)]
        [InlineData(OrderStatus.GroupAwaitingPartialResponse)]
        [InlineData(OrderStatus.NoBrokerAcceptedOrder)]
        [InlineData(OrderStatus.NoDeadlineFromCustomer)]
        [InlineData(OrderStatus.RequestAwaitingPartialAccept)]
        [InlineData(OrderStatus.Requested)]
        [InlineData(OrderStatus.RequestRespondedAwaitingApproval)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter)]
        [InlineData(OrderStatus.RequestAcceptedAwaitingInterpreter)]
        [InlineData(OrderStatus.ResponseNotAnsweredByCreator)]
        [InlineData(OrderStatus.TerminatedDueToTerminatedFrameworkAgreement)]
        [InlineData(OrderStatus.ToBeProcessedByCustomer)]
        public void Update_InvalidStatus(OrderStatus status)
        {
            var order = new Order(MockOrders.First())
            {
                Status = OrderStatus.Requested,
                AllowExceedingTravelCost = AllowExceedingTravelCost.No,
                Requests = new List<Request>()
                {
                    new Request() { Status = status == OrderStatus.ResponseAccepted ? RequestStatus.Approved : RequestStatus.Created }
                },
                OrderStatusConfirmations = new List<OrderStatusConfirmation>()
            };
            order.Requests.First().Order = order;
            order.Status = status;
            Assert.Throws<InvalidOperationException>(() => order.Update(new ChangeOrderModel()));

        }

        [Fact]
        public void Update_InvalidModel()
        {
            var order = new Order(MockOrders.First())
            {
                Status = OrderStatus.Requested,
                AllowExceedingTravelCost = AllowExceedingTravelCost.No,
                Requests = new List<Request>()
                {
                    new Request() { Status = RequestStatus.Approved }
                },
                OrderStatusConfirmations = new List<OrderStatusConfirmation>()
            };
            order.Requests.First().Order = order;
            order.Status = OrderStatus.ResponseAccepted;
            Assert.Throws<InvalidOperationException>(() => order.Update(null));

        }

        
        [Theory]
        [InlineData("2018-05-06 23:59:00 +02:00",1,1)]
        [InlineData("2018-05-07 00:00:00 +02:00",3,3)]
        [InlineData("2019-05-07 00:00:00 +02:00",3,3)]        
        [InlineData("2019-05-07 00:00:01 +02:00",1,1)]        
        public void Should_Skip_Quarantined_Broker_For_Flexible_Request(string now,int brokerId, int createdRequests)
        {
            RequestExpiryResponse response = new RequestExpiryResponse { RequestAnswerRuleType = RequestAnswerRuleType.ResponseSetByCustomer };
            var nowDate = DateTimeOffset.Parse(now);
            var order = MockFlexibleOrders.Where(o => o.OrderId == 1).First();            
            order.ExpectedLength = new TimeSpan(3, 0, 0);
            var request = order.CreateRequest(MockEntities.MockRankingsWithQuarantines.AsQueryable(), response, nowDate);
            Assert.Equal(RequestStatus.Created, request.Status);
            Assert.Equal(createdRequests, order.Requests.Count);
            Assert.Equal(brokerId, request.Ranking.BrokerId);
        }
    }
}