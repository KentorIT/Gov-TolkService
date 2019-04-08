﻿using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Tolk.BusinessLogic.Utilities;
using Xunit;

namespace Tolk.BusinessLogic.Tests.Entities
{
    public class RequestTests
    {
        private readonly Order MockOrder;
        private readonly InterpreterBroker MockInterpreter;

        public RequestTests()
        {
            var mockLanguages = MockEntities.MockLanguages();
            var mockRankings = MockEntities.MockRankings();
            var mockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers());
            MockOrder = MockEntities.MockOrders(mockLanguages, mockRankings, mockCustomerUsers).Single(o => o.OrderId == 8);
            MockInterpreter = new InterpreterBroker("first", "last", 15, "a@a.at", "12345", "ID-335");
        }

        [Fact]
        public void Recieved_Valid()
        {
            var recievedAt = DateTime.Parse("2019-01-28 12:31:00");
            var recievedBy = 10;
            var request = new Request()
            {
                Status = RequestStatus.Created
            };
            request.Received(recievedAt, recievedBy);
            Assert.Equal(RequestStatus.Received, request.Status);
            Assert.Equal(recievedAt, request.RecievedAt);
            Assert.Equal(recievedBy, request.ReceivedBy);
        }

        [Theory]
        [InlineData(RequestStatus.Accepted)]
        [InlineData(RequestStatus.AcceptedNewInterpreterAppointed)]
        [InlineData(RequestStatus.Approved)]
        [InlineData(RequestStatus.CancelledByBroker)]
        [InlineData(RequestStatus.CancelledByCreator)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved)]
        [InlineData(RequestStatus.DeclinedByBroker)]
        [InlineData(RequestStatus.DeniedByCreator)]
        [InlineData(RequestStatus.DeniedByTimeLimit)]
        [InlineData(RequestStatus.InterpreterReplaced)]
        [InlineData(RequestStatus.Received)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator)]
        [InlineData(RequestStatus.ToBeProcessedByBroker)]
        public void Recieved_Invalid(RequestStatus status)
        {
            var request = new Request()
            {
                Status = status
            };
            Assert.Throws<InvalidOperationException>(() => request.Received(DateTime.Now, 10));
        }

        [Theory]
        [InlineData(RequestStatus.Accepted)]
        [InlineData(RequestStatus.AcceptedNewInterpreterAppointed)]
        public void Approve_Valid(RequestStatus status)
        {
            var request = new Request()
            {
                Status = status,
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.RequestResponded,
                }
            };
            request.Order.Requests.Add(request);
            var time = DateTime.Now;
            var user = 10;
            request.Approve(time, user, null);

            Assert.Equal(RequestStatus.Approved, request.Status);
            Assert.Equal(OrderStatus.ResponseAccepted, request.Order.Status);
            Assert.Equal(time, request.AnswerProcessedAt);
            Assert.Equal(user, request.AnswerProcessedBy);
        }

        [Theory]
        // Already approved request
        [InlineData(RequestStatus.Accepted, true)]
        [InlineData(RequestStatus.AcceptedNewInterpreterAppointed, true)]
        [InlineData(RequestStatus.Approved, true)]
        [InlineData(RequestStatus.CancelledByBroker, true)]
        [InlineData(RequestStatus.CancelledByCreator, true)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved, true)]
        [InlineData(RequestStatus.Created, true)]
        [InlineData(RequestStatus.DeclinedByBroker, true)]
        [InlineData(RequestStatus.DeniedByCreator, true)]
        [InlineData(RequestStatus.DeniedByTimeLimit, true)]
        [InlineData(RequestStatus.InterpreterReplaced, true)]
        [InlineData(RequestStatus.Received, true)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator, true)]
        [InlineData(RequestStatus.ToBeProcessedByBroker, true)]
        // Invalid status, no approved request
        [InlineData(RequestStatus.Approved, false)]
        [InlineData(RequestStatus.CancelledByBroker, false)]
        [InlineData(RequestStatus.CancelledByCreator, false)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved, false)]
        [InlineData(RequestStatus.Created, false)]
        [InlineData(RequestStatus.DeclinedByBroker, false)]
        [InlineData(RequestStatus.DeniedByCreator, false)]
        [InlineData(RequestStatus.DeniedByTimeLimit, false)]
        [InlineData(RequestStatus.InterpreterReplaced, false)]
        [InlineData(RequestStatus.Received, false)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator, false)]
        [InlineData(RequestStatus.ToBeProcessedByBroker, false)]
        public void Approve_Invalid(RequestStatus status, bool hasApprovedRequest)
        {
            List<Request> requests = new List<Request>();
            if (hasApprovedRequest)
            {
                requests.Add(new Request() { Status = RequestStatus.Approved });
            }
            var request = new Request()
            {
                Status = status,
                Order = new Order(MockOrder)
                {
                    Requests = requests
                }
            };
            Assert.Throws<InvalidOperationException>(() => request.Approve(DateTime.Now, 1, null));
        }

        [Theory]
        [InlineData(AllowExceedingTravelCost.YesShouldBeApproved)]
        [InlineData(AllowExceedingTravelCost.YesShouldNotBeApproved)]
        [InlineData(AllowExceedingTravelCost.No)]
        public void Accept_Valid(AllowExceedingTravelCost allowExceedingTravelCost)
        {
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                    AllowExceedingTravelCost = allowExceedingTravelCost,
                    InterpreterLocations = new List<OrderInterpreterLocation>() { new OrderInterpreterLocation { InterpreterLocation = InterpreterLocation.OnSite } },
                },
            };
            request.Order.Requests.Add(request);

            var expectedRequestStatus = allowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved ? RequestStatus.Accepted : RequestStatus.Approved;
            var expectedOrderStatus = allowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved ? OrderStatus.RequestResponded : OrderStatus.ResponseAccepted;
            var acceptTime = DateTime.Now;
            var answeredBy = 10;
            var impersonatingAnsweredBy = (int?)null;
            var interpreter = new InterpreterBroker("first", "last", 15, "a@a.at", "12345", "ID-335");
            var interpreterLocation = InterpreterLocation.OnSite;
            var competenceLevel = CompetenceAndSpecialistLevel.AuthorizedInterpreter;
            var requirementAnswers = new List<OrderRequirementRequestAnswer>();
            var attachments = new List<RequestAttachment>();
            var priceInfo = new PriceInformation()
            {
                PriceRows = new List<PriceRowBase>()
            };

            request.Accept(acceptTime, answeredBy, impersonatingAnsweredBy, interpreter, interpreterLocation, competenceLevel,
                requirementAnswers, attachments, priceInfo);

            Assert.Equal(expectedRequestStatus, request.Status);
            Assert.Equal(expectedOrderStatus, request.Order.Status);
            Assert.Equal(acceptTime, request.AnswerDate);
            Assert.Equal(answeredBy, request.AnsweredBy);
            Assert.Equal(impersonatingAnsweredBy, request.ImpersonatingAnsweredBy);
            Assert.Equal(interpreter, request.Interpreter);
            Assert.Equal((int)interpreterLocation, request.InterpreterLocation);
            Assert.Equal((int)competenceLevel, request.CompetenceLevel);
            Assert.Equal(requirementAnswers, request.RequirementAnswers);
            Assert.Equal(attachments, request.Attachments);
            Assert.Equal(priceInfo.PriceRows, request.PriceRows);
        }

        [Theory]
        //Invalid status
        [InlineData(RequestStatus.Accepted, false, true)]
        [InlineData(RequestStatus.AcceptedNewInterpreterAppointed, false, true)]
        [InlineData(RequestStatus.Approved, false, true)]
        [InlineData(RequestStatus.AwaitingDeadlineFromCustomer, false, true)]
        [InlineData(RequestStatus.CancelledByBroker, false, true)]
        [InlineData(RequestStatus.CancelledByCreator, false, true)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved, false, true)]
        [InlineData(RequestStatus.DeclinedByBroker, false, true)]
        [InlineData(RequestStatus.DeniedByCreator, false, true)]
        [InlineData(RequestStatus.DeniedByTimeLimit, false, true)]
        [InlineData(RequestStatus.InterpreterReplaced, false, true)]
        [InlineData(RequestStatus.NoDeadlineFromCustomer, false, true)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator, false, true)]
        [InlineData(RequestStatus.ToBeProcessedByBroker, false, true)]
        //// Replacing order has value
        [InlineData(RequestStatus.Accepted, true, true)]
        [InlineData(RequestStatus.AcceptedNewInterpreterAppointed, true, true)]
        [InlineData(RequestStatus.Approved, true, true)]
        [InlineData(RequestStatus.AwaitingDeadlineFromCustomer, true, true)]
        [InlineData(RequestStatus.CancelledByBroker, true, true)]
        [InlineData(RequestStatus.CancelledByCreator, true, true)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved, true, true)]
        [InlineData(RequestStatus.Created, true, true)]
        [InlineData(RequestStatus.DeclinedByBroker, true, true)]
        [InlineData(RequestStatus.DeniedByCreator, true, true)]
        [InlineData(RequestStatus.DeniedByTimeLimit, true, true)]
        [InlineData(RequestStatus.InterpreterReplaced, true, true)]
        [InlineData(RequestStatus.NoDeadlineFromCustomer, true, true)]
        [InlineData(RequestStatus.Received, true, true)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator, true, true)]
        [InlineData(RequestStatus.ToBeProcessedByBroker, true, true)]
        // Interpreter isn't set
        [InlineData(RequestStatus.Accepted, true, false)]
        [InlineData(RequestStatus.AcceptedNewInterpreterAppointed, true, false)]
        [InlineData(RequestStatus.Approved, true, false)]
        [InlineData(RequestStatus.AwaitingDeadlineFromCustomer, true, false)]
        [InlineData(RequestStatus.CancelledByBroker, true, false)]
        [InlineData(RequestStatus.CancelledByCreator, true, false)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved, true, false)]
        [InlineData(RequestStatus.Created, true, false)]
        [InlineData(RequestStatus.DeclinedByBroker, true, false)]
        [InlineData(RequestStatus.DeniedByCreator, true, false)]
        [InlineData(RequestStatus.DeniedByTimeLimit, true, false)]
        [InlineData(RequestStatus.InterpreterReplaced, true, false)]
        [InlineData(RequestStatus.NoDeadlineFromCustomer, true, false)]
        [InlineData(RequestStatus.Received, true, false)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator, true, false)]
        [InlineData(RequestStatus.ToBeProcessedByBroker, true, false)]
        public void Accept_Invalid(RequestStatus status, bool replacingOrderIdHasValue, bool isInterpreterSet)
        {
            var replacingOrderId = replacingOrderIdHasValue ? (int?)42 : null;
            var interpreter = isInterpreterSet ? new InterpreterBroker("first", "last", 15, "a@a.at", "12345", "ID-335") : null;
            var request = new Request()
            {
                Status = status,
                Order = new Order(MockOrder)
                {
                    ReplacingOrderId = replacingOrderId
                }
            };
            Assert.Throws<InvalidOperationException>(() => request.Accept(DateTime.Now, 10, null,
                interpreter, InterpreterLocation.OnSite, CompetenceAndSpecialistLevel.AuthorizedInterpreter,
                new List<OrderRequirementRequestAnswer>(), new List<RequestAttachment>(),
                new PriceInformation()));
        }

        [Theory]
        [InlineData(RequestStatus.Created, false)]
        [InlineData(RequestStatus.Received, false)]
        [InlineData(RequestStatus.Created, true)]
        [InlineData(RequestStatus.Received, true)]
        public void Decline(RequestStatus status, bool hasReplacingOrder)
        {
            var replacingOrderId = hasReplacingOrder ? (int?)10 : null;
            var request = new Request()
            {
                Status = status,
                Order = new Order(MockOrder)
                {
                    ReplacingOrderId = replacingOrderId
                }
            };
            var expectedRequestStatus = RequestStatus.DeclinedByBroker;
            var expectedOrderStatus = hasReplacingOrder ? OrderStatus.NoBrokerAcceptedOrder : OrderStatus.Requested;
            var declinedAt = DateTime.Now;
            var userId = 10;
            var impersonatorId = (int?)null;
            var message = "Declined because of reasons.";

            request.Decline(declinedAt, userId, impersonatorId, message);

            Assert.Equal(expectedRequestStatus, request.Status);
            Assert.Equal(expectedOrderStatus, request.Order.Status);
            Assert.Equal(declinedAt, request.AnswerDate);
            Assert.Equal(userId, request.AnsweredBy);
            Assert.Equal(impersonatorId, request.ImpersonatingAnsweredBy);
            Assert.Equal(message, request.DenyMessage);
        }

        [Theory]
        // Invalid status
        [InlineData(RequestStatus.Accepted, false)]
        [InlineData(RequestStatus.AcceptedNewInterpreterAppointed, false)]
        [InlineData(RequestStatus.Approved, false)]
        [InlineData(RequestStatus.CancelledByBroker, false)]
        [InlineData(RequestStatus.CancelledByCreator, false)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved, false)]
        [InlineData(RequestStatus.DeclinedByBroker, false)]
        [InlineData(RequestStatus.DeniedByCreator, false)]
        [InlineData(RequestStatus.DeniedByTimeLimit, false)]
        [InlineData(RequestStatus.InterpreterReplaced, false)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator, false)]
        [InlineData(RequestStatus.ToBeProcessedByBroker, false)]
        // Invalid status, Replacing order
        [InlineData(RequestStatus.Accepted, true)]
        [InlineData(RequestStatus.AcceptedNewInterpreterAppointed, true)]
        [InlineData(RequestStatus.Approved, true)]
        [InlineData(RequestStatus.CancelledByBroker, true)]
        [InlineData(RequestStatus.CancelledByCreator, true)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved, true)]
        [InlineData(RequestStatus.DeclinedByBroker, true)]
        [InlineData(RequestStatus.DeniedByCreator, true)]
        [InlineData(RequestStatus.DeniedByTimeLimit, true)]
        [InlineData(RequestStatus.InterpreterReplaced, true)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator, true)]
        [InlineData(RequestStatus.ToBeProcessedByBroker, true)]
        public void Decline_InvalidStatus(RequestStatus status, bool hasReplacingOrder)
        {
            var replacingOrderId = hasReplacingOrder ? (int?)10 : null;
            var request = new Request()
            {
                Status = status,
                Order = new Order(MockOrder)
                {
                    ReplacingOrderId = replacingOrderId
                }
            };
            Assert.Throws<InvalidOperationException>(() =>
                request.Decline(DateTime.Now, 10, null, "Fel"));
        }

        [Theory]
        [InlineData(AllowExceedingTravelCost.YesShouldBeApproved)]
        [InlineData(AllowExceedingTravelCost.YesShouldNotBeApproved)]
        [InlineData(AllowExceedingTravelCost.No)]
        public void AcceptReplacementOrder_Valid(AllowExceedingTravelCost allowExceedingTravelCost)
        {
            var request = new Request()
            {
                Status = RequestStatus.Received,
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                    AllowExceedingTravelCost = allowExceedingTravelCost,
                    ReplacingOrderId = 14,
                }
            };
            request.Order.Requests.Add(request);
            var expectedRequestStatus = allowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved ? RequestStatus.Accepted : RequestStatus.Approved;
            var expectedOrderStatus = allowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved ? OrderStatus.RequestResponded : OrderStatus.ResponseAccepted;
            var acceptTime = DateTime.Now;
            var userId = 10;
            var impersonatorId = (int?)null;
            var priceInfo = new PriceInformation()
            {
                PriceRows = new List<PriceRowBase>()
            };

            request.AcceptReplacementOrder(acceptTime, userId, impersonatorId, null, InterpreterLocation.OnSite, priceInfo);

            Assert.Equal(expectedRequestStatus, request.Status);
            Assert.Equal(expectedOrderStatus, request.Order.Status);
            Assert.Equal(acceptTime, request.AnswerDate);
            Assert.Equal(userId, request.AnsweredBy);
            Assert.Equal(impersonatorId, request.ImpersonatingAnsweredBy);
            Assert.Equal(priceInfo.PriceRows, request.PriceRows);
        }

        [Theory]
        // Invalid status
        [InlineData(RequestStatus.Accepted, false)]
        [InlineData(RequestStatus.AcceptedNewInterpreterAppointed, false)]
        [InlineData(RequestStatus.Approved, false)]
        [InlineData(RequestStatus.CancelledByBroker, false)]
        [InlineData(RequestStatus.CancelledByCreator, false)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved, false)]
        [InlineData(RequestStatus.Created, false)]
        [InlineData(RequestStatus.DeclinedByBroker, false)]
        [InlineData(RequestStatus.DeniedByCreator, false)]
        [InlineData(RequestStatus.DeniedByTimeLimit, false)]
        [InlineData(RequestStatus.InterpreterReplaced, false)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator, false)]
        [InlineData(RequestStatus.ToBeProcessedByBroker, false)]
        // Replacing order has value
        [InlineData(RequestStatus.Accepted, true)]
        [InlineData(RequestStatus.AcceptedNewInterpreterAppointed, true)]
        [InlineData(RequestStatus.Approved, true)]
        [InlineData(RequestStatus.CancelledByBroker, true)]
        [InlineData(RequestStatus.CancelledByCreator, true)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved, true)]
        [InlineData(RequestStatus.Created, true)]
        [InlineData(RequestStatus.DeclinedByBroker, true)]
        [InlineData(RequestStatus.DeniedByCreator, true)]
        [InlineData(RequestStatus.DeniedByTimeLimit, true)]
        [InlineData(RequestStatus.InterpreterReplaced, true)]
        [InlineData(RequestStatus.Received, true)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator, true)]
        [InlineData(RequestStatus.ToBeProcessedByBroker, true)]
        public void AcceptReplacementOrder_Invalid(RequestStatus status, bool hasReplacingOrder)
        {
            var replacingOrderId = hasReplacingOrder ? (int?)10 : null;
            var request = new Request()
            {
                Status = status,
                Order = new Order(MockOrder)
                {
                    ReplacingOrderId = replacingOrderId
                }
            };
            Assert.Throws<InvalidOperationException>(() =>
                request.AcceptReplacementOrder(DateTime.Now, 10, null, null, InterpreterLocation.OnSite, new PriceInformation()));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void ReplaceInterpreter_Valid(bool isAutoAccepted, bool isOldRequestApproved)
        {
            var interpreterLocation = InterpreterLocation.OnSite;
            var request = new Request()
            {
                Status = RequestStatus.AcceptedNewInterpreterAppointed,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                    InterpreterLocations = new List<OrderInterpreterLocation>() { new OrderInterpreterLocation { InterpreterLocation = interpreterLocation } },

                
                },
            };
            var oldRequestRecievedBy = 66;
            var oldRequestRecievedAt = DateTime.Parse("2019-01-29 15:32");
            var oldRequestImpersonatingRecievedBy = (int?)null;
            var oldRequestStatus = isOldRequestApproved ? RequestStatus.Approved : RequestStatus.Received;
            var oldRequestAnswerProcessedAt = DateTime.Parse("2019-01-29 15:32");
            var oldRequestAnswerProcessedBy = 20;
            var oldRequestImpersonatingAnswerProcessedBy = 100;
            var oldRequestId = 34;
            var oldRequest = new Request()
            {
                RequestId = oldRequestId,
                Status = oldRequestStatus,
                ReceivedBy = oldRequestRecievedBy,
                RecievedAt = oldRequestRecievedAt,
                ImpersonatingReceivedBy = oldRequestImpersonatingRecievedBy,
                AnswerProcessedAt = oldRequestAnswerProcessedAt,
                AnswerProcessedBy = oldRequestAnswerProcessedBy,
                ImpersonatingAnsweredBy = oldRequestImpersonatingAnswerProcessedBy,
            };
            request.Order.Requests.Add(request);
            request.Order.Status = OrderStatus.RequestResponded;

            var expectedRequestStatus = isOldRequestApproved && isAutoAccepted ? RequestStatus.Approved : request.Status;
            var expectedOrderStatus = isOldRequestApproved && !isAutoAccepted ? OrderStatus.RequestRespondedNewInterpreter : request.Order.Status;
            var acceptTime = DateTime.Now;
            var answeredBy = 10;
            var impersonatingAnsweredBy = (int?)null;
            var interpreter = new InterpreterBroker("first", "last", 15, "a@a.at", "12345", "ID-335");
            var competenceLevel = CompetenceAndSpecialistLevel.AuthorizedInterpreter;
            var requirementAnswers = new List<OrderRequirementRequestAnswer>();
            var attachments = new List<RequestAttachment>();
            var priceInfo = new PriceInformation()
            {
                PriceRows = new List<PriceRowBase>()
            };

            request.ReplaceInterpreter(acceptTime, answeredBy, impersonatingAnsweredBy, interpreter, interpreterLocation, competenceLevel,
                requirementAnswers, attachments, priceInfo, isAutoAccepted, oldRequest);

            Assert.Equal(expectedRequestStatus, request.Status);
            Assert.Equal(expectedOrderStatus, request.Order.Status);
            Assert.Equal(acceptTime, request.AnswerDate);
            Assert.Equal(answeredBy, request.AnsweredBy);
            Assert.Equal(impersonatingAnsweredBy, request.ImpersonatingAnsweredBy);
            Assert.Equal(interpreter, request.Interpreter);
            Assert.Equal((int)interpreterLocation, request.InterpreterLocation);
            Assert.Equal((int)competenceLevel, request.CompetenceLevel);
            Assert.Equal(oldRequestRecievedAt, request.RecievedAt);
            Assert.Equal(oldRequestRecievedBy, request.ReceivedBy);
            Assert.Equal(oldRequestImpersonatingRecievedBy, request.ImpersonatingReceivedBy);
            Assert.Equal(oldRequestId, request.ReplacingRequestId);
            Assert.Equal(requirementAnswers, request.RequirementAnswers);
            Assert.Equal(attachments, request.Attachments);
            Assert.Equal(priceInfo.PriceRows, request.PriceRows);
        }

        [Theory]
        [InlineData(RequestStatus.Accepted)]
        [InlineData(RequestStatus.Approved)]
        [InlineData(RequestStatus.CancelledByBroker)]
        [InlineData(RequestStatus.CancelledByCreator)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved)]
        [InlineData(RequestStatus.Created)]
        [InlineData(RequestStatus.DeclinedByBroker)]
        [InlineData(RequestStatus.DeniedByCreator)]
        [InlineData(RequestStatus.DeniedByTimeLimit)]
        [InlineData(RequestStatus.InterpreterReplaced)]
        [InlineData(RequestStatus.Received)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator)]
        [InlineData(RequestStatus.ToBeProcessedByBroker)]
        public void ReplaceInterpreter_Invalid(RequestStatus status)
        {
            var request = new Request
            {
                Order = new Order(MockOrder),
                Status = status,
            };
            Assert.Throws<InvalidOperationException>(() =>
                request.ReplaceInterpreter(DateTime.Now, 10, null, null, InterpreterLocation.OffSitePhone, CompetenceAndSpecialistLevel.OtherInterpreter, null, null, null, false, null));
        }

        [Theory]
        [InlineData(RequestStatus.Accepted)]
        [InlineData(RequestStatus.AcceptedNewInterpreterAppointed)]
        public void Deny_Valid(RequestStatus status)
        {
            var request = new Request()
            {
                Status = status,
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                },
            };

            var denyTime = DateTime.Now;
            var userId = 10;
            var impersonatorId = (int?)null;
            var message = "Nope!";
            request.Deny(denyTime, userId, impersonatorId, message);

            Assert.Equal(RequestStatus.DeniedByCreator, request.Status);
            Assert.Equal(OrderStatus.Requested, request.Order.Status);
            Assert.Equal(denyTime, request.AnswerProcessedAt);
            Assert.Equal(userId, request.AnswerProcessedBy);
            Assert.Equal(impersonatorId, request.ImpersonatingAnsweredBy);
            Assert.Equal(message, request.DenyMessage);
        }

        [Theory]
        [InlineData(RequestStatus.Approved)]
        [InlineData(RequestStatus.CancelledByBroker)]
        [InlineData(RequestStatus.CancelledByCreator)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved)]
        [InlineData(RequestStatus.Created)]
        [InlineData(RequestStatus.DeclinedByBroker)]
        [InlineData(RequestStatus.DeniedByCreator)]
        [InlineData(RequestStatus.DeniedByTimeLimit)]
        [InlineData(RequestStatus.InterpreterReplaced)]
        [InlineData(RequestStatus.Received)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator)]
        [InlineData(RequestStatus.ToBeProcessedByBroker)]
        public void Deny_Invalid(RequestStatus status)
        {
            var request = new Request()
            {
                Status = status,
            };
            Assert.Throws<InvalidOperationException>(() => request.Deny(DateTime.Now, 10, null, "Boom!"));
        }

        [Theory]
        // OrderStatus.Requested
        [InlineData(OrderStatus.Requested, RequestStatus.Approved)]
        [InlineData(OrderStatus.Requested, RequestStatus.Accepted)]
        [InlineData(OrderStatus.Requested, RequestStatus.AcceptedNewInterpreterAppointed)]
        [InlineData(OrderStatus.Requested, RequestStatus.Created)]
        [InlineData(OrderStatus.Requested, RequestStatus.Received)]
        // OrderStatus.RequestResponded
        [InlineData(OrderStatus.RequestResponded, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestResponded, RequestStatus.Accepted)]
        [InlineData(OrderStatus.RequestResponded, RequestStatus.AcceptedNewInterpreterAppointed)]
        [InlineData(OrderStatus.RequestResponded, RequestStatus.Created)]
        [InlineData(OrderStatus.RequestResponded, RequestStatus.Received)]
        // OrderStatus.RequestRespondedNewInterpreter
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, RequestStatus.Approved)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, RequestStatus.Accepted)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, RequestStatus.AcceptedNewInterpreterAppointed)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, RequestStatus.Created)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, RequestStatus.Received)]
        // OrderStatus.ResponseAccepted
        [InlineData(OrderStatus.ResponseAccepted, RequestStatus.Approved)]
        [InlineData(OrderStatus.ResponseAccepted, RequestStatus.Accepted)]
        [InlineData(OrderStatus.ResponseAccepted, RequestStatus.AcceptedNewInterpreterAppointed)]
        [InlineData(OrderStatus.ResponseAccepted, RequestStatus.Created)]
        [InlineData(OrderStatus.ResponseAccepted, RequestStatus.Received)]
        // Is Approved, not replaced and not full compensation
        [InlineData(OrderStatus.Requested, RequestStatus.Approved, false)]
        [InlineData(OrderStatus.RequestResponded, RequestStatus.Approved, false)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, RequestStatus.Approved, false)]
        [InlineData(OrderStatus.ResponseAccepted, RequestStatus.Approved, false)]
        // Is Approved, not replaced and full compensation
        [InlineData(OrderStatus.Requested, RequestStatus.Approved, false, true)]
        [InlineData(OrderStatus.RequestResponded, RequestStatus.Approved, false, true)]
        [InlineData(OrderStatus.RequestRespondedNewInterpreter, RequestStatus.Approved, false, true)]
        [InlineData(OrderStatus.ResponseAccepted, RequestStatus.Approved, false, true)]
        public void Cancel_Valid(OrderStatus orderStatus, RequestStatus requestStatus, bool isReplaced = true, bool createFullCompensationRequisition = false)
        {
            var cancelledAt = DateTime.Parse("2019-01-29 15:32");
            var startAt = DateTime.Parse("2019-02-03 12:00");
            var endAt = startAt.AddHours(1);
            var request = new Request()
            {
                Status = RequestStatus.Approved,
                Order = new Order(MockOrder)
                {
                    StartAt = startAt,
                    EndAt = endAt,
                    ReplacingOrderId = (isReplaced ? (int?)66 : null)
                },
                Requisitions = new List<Requisition>(),
                PriceRows = new List<RequestPriceRow>(),
            };
            request.Order.Requests.Add(request);
            request.Order.Status = orderStatus;
            request.Status = requestStatus;
            request.Order.Requests.First().Status = requestStatus;

            var expectNewRequisition = request.Status == RequestStatus.Approved && !isReplaced;
            var userId = 10;
            var impersonatorId = (int?)null;
            var cancelMessage = "Neh";
            var expectedRequestStatus = request.Status == RequestStatus.Approved && !isReplaced ? RequestStatus.CancelledByCreatorWhenApproved : RequestStatus.CancelledByCreator;
            request.Cancel(cancelledAt, userId, impersonatorId, cancelMessage, createFullCompensationRequisition, isReplaced);

            Assert.Equal(expectedRequestStatus, request.Status);
            Assert.Equal(OrderStatus.CancelledByCreator, request.Order.Status);
            Assert.Equal(cancelledAt, request.CancelledAt);
            Assert.Equal(userId, request.CancelledBy);
            Assert.Equal(impersonatorId, request.ImpersonatingCanceller);
            Assert.Equal(cancelMessage, request.CancelMessage);

            if (expectNewRequisition)
            {
                var requisition = request.Requisitions.Single();
                var expectedMessage = createFullCompensationRequisition ? "Genererat av systemet, eftersom tillfället avbokades för tätt inpå" : "Genererat av systemet vid avbokning, endast förmedlingsavgift utgår";
                Assert.Equal(cancelledAt, requisition.CreatedAt);
                Assert.Equal(userId, requisition.CreatedBy);
                Assert.Equal(impersonatorId, requisition.ImpersonatingCreatedBy);
                Assert.Equal(expectedMessage, requisition.Message);
                Assert.Equal(RequisitionStatus.AutomaticGeneratedFromCancelledOrder, requisition.Status);
                Assert.Equal(request.Order.StartAt, requisition.SessionStartedAt);
                Assert.Equal(request.Order.EndAt, requisition.SessionEndedAt);
            }
        }

        [Theory]
        // Invalid OrderStatus
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.CancelledByBroker, RequestStatus.Approved)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.CancelledByCreator, RequestStatus.Approved)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.Delivered, RequestStatus.Approved)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.NoBrokerAcceptedOrder, RequestStatus.Approved)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseNotAnsweredByCreator, RequestStatus.Approved)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ToBeProcessedByCustomer, RequestStatus.Approved)]
        // Invalid RequestStatus
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.CancelledByBroker)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.CancelledByCreator)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.CancelledByCreatorWhenApproved)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.DeclinedByBroker)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.DeniedByCreator)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.DeniedByTimeLimit)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.InterpreterReplaced)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.ResponseNotAnsweredByCreator)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.ToBeProcessedByBroker)]
        // Order start time already passed
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.CancelledByBroker, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.CancelledByCreator, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.Delivered, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.NoBrokerAcceptedOrder, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.Requested, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.RequestResponded, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.RequestRespondedNewInterpreter, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseNotAnsweredByCreator, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ToBeProcessedByCustomer, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.Accepted)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.AcceptedNewInterpreterAppointed)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.CancelledByBroker)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.CancelledByCreator)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.CancelledByCreatorWhenApproved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.Created)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.DeclinedByBroker)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.DeniedByCreator)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.DeniedByTimeLimit)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.InterpreterReplaced)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.Received)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.ResponseNotAnsweredByCreator)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.ToBeProcessedByBroker)]
        public void Cancel_Invalid(string cancelTime, string startTime, OrderStatus orderStatus, RequestStatus requestStatus)
        {
            var cancelledAt = DateTime.Parse(cancelTime);
            var startAt = DateTime.Parse(startTime);
            var endAt = startAt.AddHours(1);
            var request = new Request()
            {
                Status = RequestStatus.Approved,
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.RequestResponded,
                    StartAt = startAt,
                    EndAt = endAt,
                },
                Requisitions = new List<Requisition>(),
                PriceRows = new List<RequestPriceRow>(),
            };
            request.Order.Requests.Add(request);
            request.Order.Status = orderStatus;
            request.Status = requestStatus;
            request.Order.Requests.First().Status = requestStatus;

            Assert.Throws<InvalidOperationException>(() => request.Cancel(cancelledAt, 10, null, "Neh"));
        }

        [Fact]
        public void CancelByBroker_Valid()
        {
            var cancelledAt = DateTime.Parse("2019-01-29 15:32");
            var startAt = DateTime.Parse("2019-02-03 12:00");
            var request = new Request()
            {
                Status = RequestStatus.Approved,
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.RequestResponded,
                    StartAt = startAt,
                },
            };
            request.Order.Requests.Add(request);
            request.Order.Status = OrderStatus.ResponseAccepted;
            var userId = 10;
            var impersonatorId = (int?)null;
            var cancelMessage = "Neh";

            request.CancelByBroker(cancelledAt, userId, impersonatorId, cancelMessage);

            Assert.Equal(RequestStatus.CancelledByBroker, request.Status);
            Assert.Equal(OrderStatus.CancelledByBroker, request.Order.Status);
            Assert.Equal(cancelledAt, request.CancelledAt);
            Assert.Equal(userId, request.CancelledBy);
            Assert.Equal(impersonatorId, request.ImpersonatingCanceller);
            Assert.Equal(cancelMessage, request.CancelMessage);
        }

        [Theory]
        // Invalid OrderStatus
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.CancelledByBroker, RequestStatus.Approved)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.CancelledByCreator, RequestStatus.Approved)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.Delivered, RequestStatus.Approved)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.NoBrokerAcceptedOrder, RequestStatus.Approved)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.Requested, RequestStatus.Approved)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.RequestResponded, RequestStatus.Approved)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.RequestRespondedNewInterpreter, RequestStatus.Approved)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseNotAnsweredByCreator, RequestStatus.Approved)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ToBeProcessedByCustomer, RequestStatus.Approved)]
        // Invalid RequestStatus
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.Accepted)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.AcceptedNewInterpreterAppointed)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.CancelledByBroker)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.CancelledByCreator)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.CancelledByCreatorWhenApproved)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.Created)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.DeclinedByBroker)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.DeniedByCreator)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.DeniedByTimeLimit)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.InterpreterReplaced)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.Received)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.ResponseNotAnsweredByCreator)]
        [InlineData("2019-01-29 15:32", "2019-02-03 12:00", OrderStatus.ResponseAccepted, RequestStatus.ToBeProcessedByBroker)]
        // Order start time already passed
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.CancelledByBroker, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.CancelledByCreator, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.Delivered, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.NoBrokerAcceptedOrder, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.Requested, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.RequestResponded, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.RequestRespondedNewInterpreter, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseNotAnsweredByCreator, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ToBeProcessedByCustomer, RequestStatus.Approved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.Accepted)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.AcceptedNewInterpreterAppointed)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.CancelledByBroker)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.CancelledByCreator)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.CancelledByCreatorWhenApproved)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.Created)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.DeclinedByBroker)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.DeniedByCreator)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.DeniedByTimeLimit)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.InterpreterReplaced)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.Received)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.ResponseNotAnsweredByCreator)]
        [InlineData("2019-02-03 12:00", "2019-01-29 15:32", OrderStatus.ResponseAccepted, RequestStatus.ToBeProcessedByBroker)]
        public void CancelByBroker_Invalid(string cancelTime, string startTime, OrderStatus orderStatus, RequestStatus requestStatus)
        {
            var cancelledAt = DateTime.Parse(cancelTime);
            var startAt = DateTime.Parse(startTime);
            var request = new Request()
            {
                Status = RequestStatus.Approved,
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.RequestResponded,
                    StartAt = startAt,
                },
            };
            request.Order.Requests.Add(request);
            request.Order.Status = orderStatus;
            request.Status = requestStatus;
            request.Order.Requests.First().Status = requestStatus;

            Assert.Throws<InvalidOperationException>(() => request.CancelByBroker(cancelledAt, 10, null, "Neh"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(RequisitionStatus.Commented)]
        [InlineData(RequisitionStatus.AutomaticGeneratedFromCancelledOrder)]
        public void CreateRequisition_Valid(RequisitionStatus? preexistingRequisition = null)
        {
            Requisition existingRequisition = null;
            if (preexistingRequisition != null)
            {
                existingRequisition = new Requisition
                {
                    Status = preexistingRequisition.Value,
                };
            }
            var request = new Request
            {
                Status = RequestStatus.Approved,
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.RequestResponded,
                },
                Requisitions = new List<Requisition>(),
            };
            if (existingRequisition != null)
            {
                request.Requisitions.Add(existingRequisition);
            }
            request.Order.Requests.Add(request);
            request.Order.Status = OrderStatus.ResponseAccepted;

            var requisition = new Requisition()
            {
                Status = RequisitionStatus.Created
            };
            request.CreateRequisition(requisition);

            Assert.Equal(OrderStatus.Delivered, request.Order.Status);
            Assert.Equal(requisition, request.Requisitions.Single(r => r.Status == RequisitionStatus.Created));
        }

        [Theory]
        // Invalid request status
        [InlineData(RequestStatus.Accepted)]
        [InlineData(RequestStatus.AcceptedNewInterpreterAppointed)]
        [InlineData(RequestStatus.CancelledByBroker)]
        [InlineData(RequestStatus.CancelledByCreator)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved)]
        [InlineData(RequestStatus.Created)]
        [InlineData(RequestStatus.DeclinedByBroker)]
        [InlineData(RequestStatus.DeniedByCreator)]
        [InlineData(RequestStatus.DeniedByTimeLimit)]
        [InlineData(RequestStatus.InterpreterReplaced)]
        [InlineData(RequestStatus.Received)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator)]
        [InlineData(RequestStatus.ToBeProcessedByBroker)]
        // Invalid request status, pre-existing valid requisition (DeniedByCustomer)
        [InlineData(RequestStatus.Accepted, RequisitionStatus.Commented)]
        [InlineData(RequestStatus.AcceptedNewInterpreterAppointed, RequisitionStatus.Commented)]
        [InlineData(RequestStatus.CancelledByBroker, RequisitionStatus.Commented)]
        [InlineData(RequestStatus.CancelledByCreator, RequisitionStatus.Commented)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved, RequisitionStatus.Commented)]
        [InlineData(RequestStatus.Created, RequisitionStatus.Commented)]
        [InlineData(RequestStatus.DeclinedByBroker, RequisitionStatus.Commented)]
        [InlineData(RequestStatus.DeniedByCreator, RequisitionStatus.Commented)]
        [InlineData(RequestStatus.DeniedByTimeLimit, RequisitionStatus.Commented)]
        [InlineData(RequestStatus.InterpreterReplaced, RequisitionStatus.Commented)]
        [InlineData(RequestStatus.Received, RequisitionStatus.Commented)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator, RequisitionStatus.Commented)]
        [InlineData(RequestStatus.ToBeProcessedByBroker, RequisitionStatus.Commented)]
        // Invalid request status, pre-existing valid requisition (AutomaticApprovalFromCancelledOrder)
        [InlineData(RequestStatus.Accepted, RequisitionStatus.AutomaticGeneratedFromCancelledOrder)]
        [InlineData(RequestStatus.AcceptedNewInterpreterAppointed, RequisitionStatus.AutomaticGeneratedFromCancelledOrder)]
        [InlineData(RequestStatus.CancelledByBroker, RequisitionStatus.AutomaticGeneratedFromCancelledOrder)]
        [InlineData(RequestStatus.CancelledByCreator, RequisitionStatus.AutomaticGeneratedFromCancelledOrder)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved, RequisitionStatus.AutomaticGeneratedFromCancelledOrder)]
        [InlineData(RequestStatus.Created, RequisitionStatus.AutomaticGeneratedFromCancelledOrder)]
        [InlineData(RequestStatus.DeclinedByBroker, RequisitionStatus.AutomaticGeneratedFromCancelledOrder)]
        [InlineData(RequestStatus.DeniedByCreator, RequisitionStatus.AutomaticGeneratedFromCancelledOrder)]
        [InlineData(RequestStatus.DeniedByTimeLimit, RequisitionStatus.AutomaticGeneratedFromCancelledOrder)]
        [InlineData(RequestStatus.InterpreterReplaced, RequisitionStatus.AutomaticGeneratedFromCancelledOrder)]
        [InlineData(RequestStatus.Received, RequisitionStatus.AutomaticGeneratedFromCancelledOrder)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator, RequisitionStatus.AutomaticGeneratedFromCancelledOrder)]
        [InlineData(RequestStatus.ToBeProcessedByBroker, RequisitionStatus.AutomaticGeneratedFromCancelledOrder)]
        // Valid request status, invalid requisition status
        [InlineData(RequestStatus.Approved, RequisitionStatus.Reviewed)]
        [InlineData(RequestStatus.Approved, RequisitionStatus.Created)]
        public void CreateRequisition_Invalid(RequestStatus status, RequisitionStatus? existingRequisition = null)
        {
            Requisition requisition = null;
            if (existingRequisition != null)
            {
                requisition = new Requisition
                {
                    Status = existingRequisition.Value,
                };
            }
            var request = new Request
            {
                Status = status,
                Requisitions = new List<Requisition>(),
            };
            if (requisition != null)
            {
                request.Requisitions.Add(requisition);
            }

            Assert.Throws<InvalidOperationException>(() => request.CreateRequisition(new Requisition()));
        }

        [Fact]
        public void CreateComplaint_Valid()
        {
            var complaint = new Complaint
            {
                Status = ComplaintStatus.Created,
                ComplaintType = ComplaintType.NoDelivery,
                ComplaintMessage = "Vafalls!",
            };
            var request = new Request
            {
                Status = RequestStatus.Approved,
                Complaints = new List<Complaint>()
            };
            request.CreateComplaint(complaint);

            Assert.Single(request.Complaints);
            Assert.Equal(complaint, request.Complaints[0]);
        }

        [Fact]
        public void CreateComplaint_Invalid()
        {
            var request = new Request()
            {
                Complaints = new List<Complaint>()
                {
                    new Complaint()
                }
            };
            Assert.Throws<InvalidOperationException>(() => request.CreateComplaint(new Complaint()));
        }

        //REQUIREMENTS

        [Fact]
        public void AcceptWithOneRequiredRequirement_Valid()
        {
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                    Requirements = new List<OrderRequirement>() { new OrderRequirement { OrderRequirementId = 1, RequirementType = RequirementType.SpecifiedInterpreter, IsRequired = true } }
                },
            };
            request.Order.Requests.Add(request);
            var requirementAnswers = new List<OrderRequirementRequestAnswer>() { new OrderRequirementRequestAnswer { OrderRequirementId = 1, CanSatisfyRequirement = true } };
            request.Accept(
                DateTimeOffset.Now,
                1,
                null,
                MockInterpreter,
                InterpreterLocation.OffSitePhone,
                CompetenceAndSpecialistLevel.OtherInterpreter,
                new List<OrderRequirementRequestAnswer>() { new OrderRequirementRequestAnswer { OrderRequirementId = 1, CanSatisfyRequirement = true } },
                new List<RequestAttachment>(),
                new PriceInformation() { PriceRows = new List<PriceRowBase>() });
        }

        [Fact]
        public void AcceptWithOneRequiredRequirement_NegativeAnswer()
        {
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                    Requirements = new List<OrderRequirement>() { new OrderRequirement { OrderRequirementId = 1, RequirementType = RequirementType.SpecifiedInterpreter, IsRequired = true } }
                },
            };
            request.Order.Requests.Add(request);
            Assert.Throws<InvalidOperationException>(() =>
                request.Accept(
                    DateTimeOffset.Now,
                    1,
                    null,
                    MockInterpreter,
                    InterpreterLocation.OffSitePhone,
                    CompetenceAndSpecialistLevel.OtherInterpreter,
                    new List<OrderRequirementRequestAnswer>() { new OrderRequirementRequestAnswer { OrderRequirementId = 1, CanSatisfyRequirement = false } },
                    new List<RequestAttachment>(),
                    new PriceInformation() { PriceRows = new List<PriceRowBase>() })
            );
        }

        [Fact]
        public void AcceptWithOneRequiredRequirement_NoAnswer()
        {
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                    Requirements = new List<OrderRequirement>() { new OrderRequirement { OrderRequirementId = 1, RequirementType = RequirementType.SpecifiedInterpreter, IsRequired = true } }
                },
            };
            request.Order.Requests.Add(request);
            Assert.Throws<InvalidOperationException>(() =>
                request.Accept(
                    DateTimeOffset.Now,
                    1,
                    null,
                    MockInterpreter,
                    InterpreterLocation.OffSitePhone,
                    CompetenceAndSpecialistLevel.OtherInterpreter,
                    new List<OrderRequirementRequestAnswer>(),
                    new List<RequestAttachment>(),
                    new PriceInformation() { PriceRows = new List<PriceRowBase>() })
            );
        }

        [Fact]
        public void AcceptWithOneRequiredRequirement_WrongId()
        {
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                    Requirements = new List<OrderRequirement>() { new OrderRequirement { OrderRequirementId = 1, RequirementType = RequirementType.SpecifiedInterpreter, IsRequired = true } }
                },
            };
            request.Order.Requests.Add(request);
            Assert.Throws<InvalidOperationException>(() =>
                request.Accept(
                    DateTimeOffset.Now,
                    1,
                    null,
                    MockInterpreter,
                    InterpreterLocation.OffSitePhone,
                    CompetenceAndSpecialistLevel.OtherInterpreter,
                    new List<OrderRequirementRequestAnswer>() { new OrderRequirementRequestAnswer { OrderRequirementId = 2, CanSatisfyRequirement = true } },
                    new List<RequestAttachment>(),
                    new PriceInformation() { PriceRows = new List<PriceRowBase>() })
            );
        }
        // no on not required
        [Fact]
        public void AcceptWithOneNOTRequiredRequirement_NegativeAnswer()
        {
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                    Requirements = new List<OrderRequirement>() { new OrderRequirement { OrderRequirementId = 1, RequirementType = RequirementType.SpecifiedInterpreter, IsRequired = false } }
                },
            };
            request.Order.Requests.Add(request);
            request.Accept(
                    DateTimeOffset.Now,
                    1,
                    null,
                    MockInterpreter,
                    InterpreterLocation.OffSitePhone,
                    CompetenceAndSpecialistLevel.OtherInterpreter,
                    new List<OrderRequirementRequestAnswer>() { new OrderRequirementRequestAnswer { OrderRequirementId = 1, CanSatisfyRequirement = false } },
                    new List<RequestAttachment>(),
                    new PriceInformation() { PriceRows = new List<PriceRowBase>() });
        }

        [Fact]
        public void AcceptWithOneNOTRequiredRequirement_PositiveAnswer()
        {
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                    Requirements = new List<OrderRequirement>() { new OrderRequirement { OrderRequirementId = 1, RequirementType = RequirementType.SpecifiedInterpreter, IsRequired = false } }
                },
            };
            request.Order.Requests.Add(request);
            request.Accept(
                    DateTimeOffset.Now,
                    1,
                    null,
                    MockInterpreter,
                    InterpreterLocation.OffSitePhone,
                    CompetenceAndSpecialistLevel.OtherInterpreter,
                    new List<OrderRequirementRequestAnswer>() { new OrderRequirementRequestAnswer { OrderRequirementId = 1, CanSatisfyRequirement = true } },
                    new List<RequestAttachment>(),
                    new PriceInformation() { PriceRows = new List<PriceRowBase>() });
        }
        // no answer on not required
        [Fact]
        public void AcceptWithOneNOTRequiredRequirement_NoAnswer()
        {
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                    Requirements = new List<OrderRequirement>() { new OrderRequirement { OrderRequirementId = 1, RequirementType = RequirementType.SpecifiedInterpreter, IsRequired = false } }
                },
            };
            request.Order.Requests.Add(request);
            Assert.Throws<InvalidOperationException>(() =>
                request.Accept(
                    DateTimeOffset.Now,
                    1,
                    null,
                    MockInterpreter,
                    InterpreterLocation.OffSitePhone,
                    CompetenceAndSpecialistLevel.OtherInterpreter,
                    new List<OrderRequirementRequestAnswer>(),
                    new List<RequestAttachment>(),
                    new PriceInformation() { PriceRows = new List<PriceRowBase>() })
            );
        }

        [Fact]
        public void AcceptWithSeveralRequirementsMixedRequired_CanSatisfyAll()
        {
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                    Requirements = new List<OrderRequirement>() {
                        new OrderRequirement { OrderRequirementId = 1, RequirementType = RequirementType.SpecifiedInterpreter, IsRequired = false },
                        new OrderRequirement { OrderRequirementId = 2, RequirementType = RequirementType.DeniedInterpreter, IsRequired = true },
                        new OrderRequirement { OrderRequirementId = 3, RequirementType = RequirementType.HasSecurityClearence, IsRequired = true },
                    }
                },
            };
            request.Order.Requests.Add(request);
            request.Accept(
                DateTimeOffset.Now,
                1,
                null,
                MockInterpreter,
                InterpreterLocation.OffSitePhone,
                CompetenceAndSpecialistLevel.OtherInterpreter,
                new List<OrderRequirementRequestAnswer>()
                {
                    new OrderRequirementRequestAnswer {OrderRequirementId = 1, CanSatisfyRequirement = true },
                    new OrderRequirementRequestAnswer {OrderRequirementId = 2, CanSatisfyRequirement = true },
                    new OrderRequirementRequestAnswer {OrderRequirementId = 3, CanSatisfyRequirement = true }
                },
                new List<RequestAttachment>(),
                new PriceInformation() { PriceRows = new List<PriceRowBase>() });
        }

        // yes and no on required
        [Fact]
        public void AcceptWithSeveralRequirementsMixedRequired_CanSatisfyAllRequired()
        {
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                    Requirements = new List<OrderRequirement>() {
                        new OrderRequirement { OrderRequirementId = 1, RequirementType = RequirementType.SpecifiedInterpreter, IsRequired = false },
                        new OrderRequirement { OrderRequirementId = 2, RequirementType = RequirementType.DeniedInterpreter, IsRequired = true },
                        new OrderRequirement { OrderRequirementId = 3, RequirementType = RequirementType.HasSecurityClearence, IsRequired = true },
                    }
                },
            };
            request.Order.Requests.Add(request);
            request.Accept(
                DateTimeOffset.Now,
                1,
                null,
                MockInterpreter,
                InterpreterLocation.OffSitePhone,
                CompetenceAndSpecialistLevel.OtherInterpreter,
                new List<OrderRequirementRequestAnswer>()
                {
                    new OrderRequirementRequestAnswer {OrderRequirementId = 1, CanSatisfyRequirement = false },
                    new OrderRequirementRequestAnswer {OrderRequirementId = 2, CanSatisfyRequirement = true },
                    new OrderRequirementRequestAnswer {OrderRequirementId = 3, CanSatisfyRequirement = true }
                },
                new List<RequestAttachment>(),
                new PriceInformation() { PriceRows = new List<PriceRowBase>() });
        }

        [Fact]
        public void AcceptWithSeveralRequirementsMixedRequired_CanNOTSatisfyAllRequired()
        {
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                    Requirements = new List<OrderRequirement>() {
                        new OrderRequirement { OrderRequirementId = 1, RequirementType = RequirementType.SpecifiedInterpreter, IsRequired = false },
                        new OrderRequirement { OrderRequirementId = 2, RequirementType = RequirementType.DeniedInterpreter, IsRequired = true },
                        new OrderRequirement { OrderRequirementId = 3, RequirementType = RequirementType.HasSecurityClearence, IsRequired = true },
                    }
                },
            };
            request.Order.Requests.Add(request);
            Assert.Throws<InvalidOperationException>(() =>
                request.Accept(
                    DateTimeOffset.Now,
                    1,
                    null,
                    MockInterpreter,
                    InterpreterLocation.OffSitePhone,
                    CompetenceAndSpecialistLevel.OtherInterpreter,
                    new List<OrderRequirementRequestAnswer>()
                    {
                        new OrderRequirementRequestAnswer {OrderRequirementId = 1, CanSatisfyRequirement = true },
                        new OrderRequirementRequestAnswer {OrderRequirementId = 2, CanSatisfyRequirement = false },
                        new OrderRequirementRequestAnswer {OrderRequirementId = 3, CanSatisfyRequirement = true }
                    },
                    new List<RequestAttachment>(),
                    new PriceInformation() { PriceRows = new List<PriceRowBase>() })
            );
        }

        //COMPETENCE LEVELS
        [Theory]
        [InlineData(CompetenceAndSpecialistLevel.OtherInterpreter)]
        [InlineData(CompetenceAndSpecialistLevel.EducatedInterpreter)]
        [InlineData(CompetenceAndSpecialistLevel.AuthorizedInterpreter)]
        [InlineData(CompetenceAndSpecialistLevel.HealthCareSpecialist)]
        [InlineData(CompetenceAndSpecialistLevel.CourtSpecialist)]
        public void AcceptWithNoRequestedCompetenceLevels(CompetenceAndSpecialistLevel level)
        {
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                },
            };
            request.Order.Requests.Add(request);
            request.Accept(
                DateTimeOffset.Now,
                1,
                null,
                MockInterpreter,
                InterpreterLocation.OffSitePhone,
                level,
                new List<OrderRequirementRequestAnswer>(),
                new List<RequestAttachment>(),
                new PriceInformation() { PriceRows = new List<PriceRowBase>() });

        }

        [Theory]
        [InlineData(CompetenceAndSpecialistLevel.OtherInterpreter)]
        [InlineData(CompetenceAndSpecialistLevel.EducatedInterpreter)]
        [InlineData(CompetenceAndSpecialistLevel.AuthorizedInterpreter)]
        [InlineData(CompetenceAndSpecialistLevel.HealthCareSpecialist)]
        [InlineData(CompetenceAndSpecialistLevel.CourtSpecialist)]
        [InlineData(CompetenceAndSpecialistLevel.OtherInterpreter, true)]
        [InlineData(CompetenceAndSpecialistLevel.EducatedInterpreter, true)]
        [InlineData(CompetenceAndSpecialistLevel.AuthorizedInterpreter, true)]
        [InlineData(CompetenceAndSpecialistLevel.HealthCareSpecialist, true)]
        [InlineData(CompetenceAndSpecialistLevel.CourtSpecialist, true)]
        public void AcceptWithRequestedCompetenceLevelsNOTRequired(CompetenceAndSpecialistLevel level, bool multiple = false)
        {
            var competenceLevels = new List<OrderCompetenceRequirement> {
                new OrderCompetenceRequirement { CompetenceLevel = CompetenceAndSpecialistLevel.OtherInterpreter}
            };
            if (multiple)
            {
                competenceLevels.Add(new OrderCompetenceRequirement { CompetenceLevel = CompetenceAndSpecialistLevel.HealthCareSpecialist });
            }
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                    CompetenceRequirements = competenceLevels,
                    SpecificCompetenceLevelRequired = false
                },
            };
            request.Order.Requests.Add(request);
            request.Accept(
                DateTimeOffset.Now,
                1,
                null,
                MockInterpreter,
                InterpreterLocation.OffSitePhone,
                level,
                new List<OrderRequirementRequestAnswer>(),
                new List<RequestAttachment>(),
                new PriceInformation() { PriceRows = new List<PriceRowBase>() });

        }

        [Fact]
        public void AcceptWithOneRequiredCompetenceLevel_Valid()
        {
            var competenceLevels = new List<OrderCompetenceRequirement> {
                new OrderCompetenceRequirement { CompetenceLevel = CompetenceAndSpecialistLevel.OtherInterpreter}
            };
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                    CompetenceRequirements = new List<OrderCompetenceRequirement> {
                        new OrderCompetenceRequirement { CompetenceLevel = CompetenceAndSpecialistLevel.OtherInterpreter}
                    },
                    SpecificCompetenceLevelRequired = true
                },
            };
            request.Order.Requests.Add(request);
            request.Accept(
                DateTimeOffset.Now,
                1,
                null,
                MockInterpreter,
                InterpreterLocation.OffSitePhone,
                CompetenceAndSpecialistLevel.OtherInterpreter,
                new List<OrderRequirementRequestAnswer>(),
                new List<RequestAttachment>(),
                new PriceInformation() { PriceRows = new List<PriceRowBase>() });
        }

        [Theory]
        [InlineData(CompetenceAndSpecialistLevel.EducatedInterpreter)]
        [InlineData(CompetenceAndSpecialistLevel.AuthorizedInterpreter)]
        [InlineData(CompetenceAndSpecialistLevel.HealthCareSpecialist)]
        [InlineData(CompetenceAndSpecialistLevel.CourtSpecialist)]
        public void AcceptWithOneRequiredCompetenceLevel_Invalid(CompetenceAndSpecialistLevel level)
        {
            var competenceLevels = new List<OrderCompetenceRequirement> {
                new OrderCompetenceRequirement { CompetenceLevel = CompetenceAndSpecialistLevel.OtherInterpreter}
            };
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                    CompetenceRequirements = competenceLevels,
                    SpecificCompetenceLevelRequired = true
                },
            };
            request.Order.Requests.Add(request);
            Assert.Throws<InvalidOperationException>(() =>
                request.Accept(
                    DateTimeOffset.Now,
                    1,
                    null,
                    MockInterpreter,
                    InterpreterLocation.OffSitePhone,
                    level,
                    new List<OrderRequirementRequestAnswer>(),
                    new List<RequestAttachment>(),
                    new PriceInformation() { PriceRows = new List<PriceRowBase>() })
            );
        }

        [Theory]
        [InlineData(CompetenceAndSpecialistLevel.EducatedInterpreter)]
        [InlineData(CompetenceAndSpecialistLevel.AuthorizedInterpreter)]
        public void AcceptWithTwoRequiredCompetenceLevel_Valid(CompetenceAndSpecialistLevel level)
        {
            var competenceLevels = new List<OrderCompetenceRequirement> {
                new OrderCompetenceRequirement { CompetenceLevel = CompetenceAndSpecialistLevel.EducatedInterpreter},
                new OrderCompetenceRequirement { CompetenceLevel = CompetenceAndSpecialistLevel.AuthorizedInterpreter}
            };
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                    CompetenceRequirements = competenceLevels,
                    SpecificCompetenceLevelRequired = true
                },
            };
            request.Order.Requests.Add(request);
            request.Accept(
                DateTimeOffset.Now,
                1,
                null,
                MockInterpreter,
                InterpreterLocation.OffSitePhone,
                level,
                new List<OrderRequirementRequestAnswer>(),
                new List<RequestAttachment>(),
                new PriceInformation() { PriceRows = new List<PriceRowBase>() });
        }

        [Theory]
        [InlineData(CompetenceAndSpecialistLevel.HealthCareSpecialist)]
        [InlineData(CompetenceAndSpecialistLevel.CourtSpecialist)]
        [InlineData(CompetenceAndSpecialistLevel.OtherInterpreter)]
        public void AcceptWithTwoRequiredCompetenceLevel_Invalid(CompetenceAndSpecialistLevel level)
        {
            var competenceLevels = new List<OrderCompetenceRequirement> {
                new OrderCompetenceRequirement { CompetenceLevel = CompetenceAndSpecialistLevel.EducatedInterpreter},
                new OrderCompetenceRequirement { CompetenceLevel = CompetenceAndSpecialistLevel.AuthorizedInterpreter}
            };
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.Requested,
                    CompetenceRequirements = competenceLevels,
                    SpecificCompetenceLevelRequired = true
                },
            };
            request.Order.Requests.Add(request);
            Assert.Throws<InvalidOperationException>(() =>
                request.Accept(
                    DateTimeOffset.Now,
                    1,
                    null,
                    MockInterpreter,
                    InterpreterLocation.OffSitePhone,
                    level,
                    new List<OrderRequirementRequestAnswer>(),
                    new List<RequestAttachment>(),
                    new PriceInformation() { PriceRows = new List<PriceRowBase>() })
            );
        }

        //INTERPRETER LOCATIONS
        [Theory]
        [InlineData(InterpreterLocation.OffSiteDesignatedLocation)]
        [InlineData(InterpreterLocation.OffSitePhone)]
        [InlineData(InterpreterLocation.OffSiteVideo)]
        public void AcceptWithOneLocation_Invalid(InterpreterLocation location)
        {
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    InterpreterLocations = new List<OrderInterpreterLocation>() { new OrderInterpreterLocation {InterpreterLocation= InterpreterLocation.OnSite } },
                    Status = OrderStatus.Requested,
                },
            };
            request.Order.Requests.Add(request);
            Assert.Throws<InvalidOperationException>(() =>
                request.Accept(
                    DateTimeOffset.Now,
                    1,
                    null,
                    MockInterpreter,
                    location,
                    CompetenceAndSpecialistLevel.OtherInterpreter,
                    new List<OrderRequirementRequestAnswer>(),
                    new List<RequestAttachment>(),
                    new PriceInformation() { PriceRows = new List<PriceRowBase>() })
            );
        }

        [Theory]
        [InlineData(InterpreterLocation.OnSite)]
        [InlineData(InterpreterLocation.OffSiteDesignatedLocation)]
        public void AcceptWithTwoLocations_Valid(InterpreterLocation location)
        {
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    InterpreterLocations = new List<OrderInterpreterLocation>() {
                        new OrderInterpreterLocation { InterpreterLocation = InterpreterLocation.OnSite },
                        new OrderInterpreterLocation { InterpreterLocation = InterpreterLocation.OffSiteDesignatedLocation }
                },
                    Status = OrderStatus.Requested,
                },
            };
            request.Order.Requests.Add(request);
            request.Accept(
                DateTimeOffset.Now,
                1,
                null,
                MockInterpreter,
                location,
                CompetenceAndSpecialistLevel.OtherInterpreter,
                new List<OrderRequirementRequestAnswer>(),
                new List<RequestAttachment>(),
                new PriceInformation() { PriceRows = new List<PriceRowBase>() });
        }

        [Theory]
        [InlineData(InterpreterLocation.OffSitePhone)]
        [InlineData(InterpreterLocation.OffSiteVideo)]
        public void AcceptWithTwoLocations_Invalid(InterpreterLocation location)
        {
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    InterpreterLocations = new List<OrderInterpreterLocation>() {
                        new OrderInterpreterLocation { InterpreterLocation = InterpreterLocation.OnSite },
                        new OrderInterpreterLocation { InterpreterLocation = InterpreterLocation.OffSiteDesignatedLocation }
                },
                    Status = OrderStatus.Requested,
                },
            };
            request.Order.Requests.Add(request);
            Assert.Throws<InvalidOperationException>(() =>
                request.Accept(
                    DateTimeOffset.Now,
                    1,
                    null,
                    MockInterpreter,
                    location,
                    CompetenceAndSpecialistLevel.OtherInterpreter,
                    new List<OrderRequirementRequestAnswer>(),
                    new List<RequestAttachment>(),
                    new PriceInformation() { PriceRows = new List<PriceRowBase>() })
            );
        }

        [Theory]
        [InlineData(InterpreterLocation.OnSite)]
        [InlineData(InterpreterLocation.OffSiteDesignatedLocation)]
        [InlineData(InterpreterLocation.OffSitePhone)]
        public void AcceptWithThreeLocations_Valid(InterpreterLocation location)
        {
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    InterpreterLocations = new List<OrderInterpreterLocation>() {
                        new OrderInterpreterLocation { InterpreterLocation = InterpreterLocation.OnSite },
                        new OrderInterpreterLocation { InterpreterLocation = InterpreterLocation.OffSiteDesignatedLocation },
                        new OrderInterpreterLocation { InterpreterLocation = InterpreterLocation.OffSitePhone }
                },
                    Status = OrderStatus.Requested,
                },
            };
            request.Order.Requests.Add(request);
            request.Accept(
                DateTimeOffset.Now,
                1,
                null,
                MockInterpreter,
                location,
                CompetenceAndSpecialistLevel.OtherInterpreter,
                new List<OrderRequirementRequestAnswer>(),
                new List<RequestAttachment>(),
                new PriceInformation() { PriceRows = new List<PriceRowBase>() });
        }

        [Theory]
        [InlineData(InterpreterLocation.OffSitePhone)]
        public void AcceptWithThreeLocations_Invalid(InterpreterLocation location)
        {
            var request = new Request()
            {
                Status = RequestStatus.Received,
                RequirementAnswers = new List<OrderRequirementRequestAnswer>(),
                PriceRows = new List<RequestPriceRow>(),
                Order = new Order(MockOrder)
                {
                    InterpreterLocations = new List<OrderInterpreterLocation>() {
                        new OrderInterpreterLocation { InterpreterLocation = InterpreterLocation.OnSite },
                        new OrderInterpreterLocation { InterpreterLocation = InterpreterLocation.OffSiteDesignatedLocation },
                        new OrderInterpreterLocation { InterpreterLocation = InterpreterLocation.OffSiteVideo }

                },
                    Status = OrderStatus.Requested,
                },
            };
            request.Order.Requests.Add(request);
            Assert.Throws<InvalidOperationException>(() =>
                request.Accept(
                    DateTimeOffset.Now,
                    1,
                    null,
                    MockInterpreter,
                    location,
                    CompetenceAndSpecialistLevel.OtherInterpreter,
                    new List<OrderRequirementRequestAnswer>(),
                    new List<RequestAttachment>(),
                    new PriceInformation() { PriceRows = new List<PriceRowBase>() })
            );
        }
    }
}
