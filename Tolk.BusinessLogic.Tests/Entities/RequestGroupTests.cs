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
    public class RequestGroupTests
    {
        private readonly InterpreterBroker MockInterpreter;
        private readonly OrderGroup[] MockOrderGroups;

        public RequestGroupTests()
        {
            MockOrderGroups = MockEntities.MockOrderGroups(MockEntities.MockLanguages, MockEntities.MockRankings, MockEntities.MockCustomerUsers(MockEntities.MockCustomers));
            MockInterpreter = new InterpreterBroker("first", "last", 15, "a@a.at", "12345", "ID-335");
        }

        [Fact]
        public void Recieved_Valid()
        {
            var recievedAt = DateTime.Parse("2019-01-28 12:31:00");
            var recievedBy = 10;
            var orderGroup = MockOrderGroups.Single(og => og.OrderGroupNumber == "REQUESTSJUSTCREATED");
            var requestGroup = orderGroup.RequestGroups.First();
            requestGroup.Received(recievedAt, recievedBy);
            Assert.Equal(RequestStatus.Received, requestGroup.Status);
            Assert.Equal(recievedAt, requestGroup.RecievedAt);
            Assert.Equal(recievedBy, requestGroup.ReceivedBy);
            foreach (var request in requestGroup.Requests)
            {
                Assert.Equal(RequestStatus.Received, request.Status);
                Assert.Equal(recievedAt, request.RecievedAt);
                Assert.Equal(recievedBy, request.ReceivedBy);
            }
        }

        [Theory]
        [InlineData(RequestStatus.Accepted)]
        [InlineData(RequestStatus.Approved)]
        [InlineData(RequestStatus.CancelledByCreator)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved)]
        [InlineData(RequestStatus.DeclinedByBroker)]
        [InlineData(RequestStatus.DeniedByCreator)]
        [InlineData(RequestStatus.DeniedByTimeLimit)]
        [InlineData(RequestStatus.Received)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator)]
        [InlineData(RequestStatus.ToBeProcessedByBroker)]
        [InlineData(RequestStatus.LostDueToQuarantine)]
        [InlineData(RequestStatus.AwaitingDeadlineFromCustomer)]
        [InlineData(RequestStatus.NoDeadlineFromCustomer)]
        public void Recieved_Invalid(RequestStatus status)
        {
            var request = new RequestGroup()
            {
                Status = status
            };
            Assert.Throws<InvalidOperationException>(() => request.Received(DateTime.Now, 10));
        }

        [Theory]
        [InlineData(RequestStatus.CancelledByBroker)]
        [InlineData(RequestStatus.AcceptedNewInterpreterAppointed)]
        [InlineData(RequestStatus.InterpreterReplaced)]
        public void Status_Invalid(RequestStatus status)
        {
            Assert.Throws<InvalidOperationException>(() => new RequestGroup()
            {
                Status = status
            });
        }

        [Fact]
        public void Approve_Valid()
        {
            var approvedAt = DateTime.Parse("2019-01-28 12:31:00");
            var approvedBy = 10;
            var orderGroup = MockOrderGroups.Single(og => og.OrderGroupNumber == "REQUESTGROUPAWAITINGAPPROVAL");
            var requestGroup = orderGroup.RequestGroups.First();
            requestGroup.Approve(approvedAt, approvedBy, null);
            Assert.Equal(RequestStatus.Approved, requestGroup.Status);
            Assert.Equal(approvedAt, requestGroup.AnswerProcessedAt);
            Assert.Equal(approvedBy, requestGroup.AnswerProcessedBy);
            foreach (var request in requestGroup.Requests)
            {
                Assert.Equal(RequestStatus.Approved, request.Status);
                Assert.Equal(approvedAt, request.AnswerProcessedAt);
                Assert.Equal(approvedBy, request.AnswerProcessedBy);
            }
        }

        [Theory]
        [InlineData(RequestStatus.Created)]
        [InlineData(RequestStatus.Approved)]
        [InlineData(RequestStatus.CancelledByCreator)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved)]
        [InlineData(RequestStatus.DeclinedByBroker)]
        [InlineData(RequestStatus.DeniedByCreator)]
        [InlineData(RequestStatus.DeniedByTimeLimit)]
        [InlineData(RequestStatus.Received)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator)]
        [InlineData(RequestStatus.ToBeProcessedByBroker)]
        [InlineData(RequestStatus.LostDueToQuarantine)]
        [InlineData(RequestStatus.AwaitingDeadlineFromCustomer)]
        [InlineData(RequestStatus.NoDeadlineFromCustomer)]
        public void Approve_Invalid(RequestStatus status)
        {
            var orderGroup = MockOrderGroups.Single(og => og.OrderGroupNumber == "REQUESTSJUSTCREATED");
            var requestGroup = orderGroup.RequestGroups.First();
            requestGroup.SetStatus(status);
            Assert.Throws<InvalidOperationException>(() => requestGroup.Approve(DateTime.Now, 10, null));
        }

        [Fact]
        public void Deny_Valid()
        {
            var approvedAt = DateTime.Parse("2019-01-28 12:31:00");
            var approvedBy = 10;
            var orderGroup = MockOrderGroups.Single(og => og.OrderGroupNumber == "REQUESTGROUPAWAITINGAPPROVAL");
            var requestGroup = orderGroup.RequestGroups.First();
            var message = "Declined because of reasons.";
            var expectedOrderStatus = OrderStatus.Requested;
            requestGroup.Deny(approvedAt, approvedBy, null, message);
            Assert.Equal(RequestStatus.DeniedByCreator, requestGroup.Status);
            Assert.Equal(expectedOrderStatus, requestGroup.OrderGroup.Status);
            Assert.Equal(approvedAt, requestGroup.AnswerProcessedAt);
            Assert.Equal(approvedBy, requestGroup.AnswerProcessedBy);
            foreach (var request in requestGroup.Requests)
            {
                Assert.Equal(RequestStatus.DeniedByCreator, request.Status);
                Assert.Equal(expectedOrderStatus, request.Order.Status);
                Assert.Equal(approvedAt, request.AnswerProcessedAt);
                Assert.Equal(approvedBy, request.AnswerProcessedBy);
            }
        }

        [Theory]
        [InlineData(RequestStatus.Created)]
        [InlineData(RequestStatus.Approved)]
        [InlineData(RequestStatus.CancelledByCreator)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved)]
        [InlineData(RequestStatus.DeclinedByBroker)]
        [InlineData(RequestStatus.DeniedByCreator)]
        [InlineData(RequestStatus.DeniedByTimeLimit)]
        [InlineData(RequestStatus.Received)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator)]
        [InlineData(RequestStatus.ToBeProcessedByBroker)]
        [InlineData(RequestStatus.LostDueToQuarantine)]
        [InlineData(RequestStatus.AwaitingDeadlineFromCustomer)]
        [InlineData(RequestStatus.NoDeadlineFromCustomer)]
        public void Deny_Invalid(RequestStatus status)
        {
            var orderGroup = MockOrderGroups.Single(og => og.OrderGroupNumber == "REQUESTSJUSTCREATED");
            var requestGroup = orderGroup.RequestGroups.First();
            requestGroup.SetStatus(status);
            Assert.Throws<InvalidOperationException>(() => requestGroup.Deny(DateTime.Now, 10, null, "xx"));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Decline_Valid(bool receive)
        {
            var orderGroup = MockOrderGroups.Single(og => og.OrderGroupNumber == "REQUESTSJUSTCREATED");
            var requestGroup = orderGroup.RequestGroups.First();
            var expectedRequestStatus = RequestStatus.DeclinedByBroker;
            var expectedOrderStatus = OrderStatus.Requested;
            var declinedAt = DateTime.Now;
            var userId = 10;
            var impersonatorId = (int?)null;
            var message = "Declined because of reasons.";

            if (receive)
            {
                requestGroup.Received(declinedAt, userId, impersonatorId);
            }
            requestGroup.Decline(declinedAt, userId, impersonatorId, message);

            Assert.Equal(expectedRequestStatus, requestGroup.Status);
            Assert.Equal(expectedOrderStatus, requestGroup.OrderGroup.Status);
            Assert.Equal(declinedAt, requestGroup.AnswerDate);
            Assert.Equal(userId, requestGroup.AnsweredBy);
            Assert.Equal(impersonatorId, requestGroup.ImpersonatingAnsweredBy);
            Assert.Equal(message, requestGroup.DenyMessage);
            foreach (var request in requestGroup.Requests)
            {
                Assert.Equal(expectedRequestStatus, request.Status);
                Assert.Equal(expectedOrderStatus, request.Order.Status);
                Assert.Equal(declinedAt, request.AnswerDate);
                Assert.Equal(userId, request.AnsweredBy);
                Assert.Equal(impersonatorId, request.ImpersonatingAnsweredBy);
                Assert.Equal(message, request.DenyMessage);
            }
        }

        [Theory]
        [InlineData(RequestStatus.Accepted)]
        [InlineData(RequestStatus.Approved)]
        [InlineData(RequestStatus.AwaitingDeadlineFromCustomer)]
        [InlineData(RequestStatus.CancelledByCreator)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved)]
        [InlineData(RequestStatus.DeclinedByBroker)]
        [InlineData(RequestStatus.DeniedByCreator)]
        [InlineData(RequestStatus.DeniedByTimeLimit)]
        [InlineData(RequestStatus.LostDueToQuarantine)]
        [InlineData(RequestStatus.NoDeadlineFromCustomer)]
        [InlineData(RequestStatus.PartiallyAccepted)]
        [InlineData(RequestStatus.PartiallyApproved)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator)]
        [InlineData(RequestStatus.ToBeProcessedByBroker)]
        public void Decline_Invalid(RequestStatus status)
        {
            var orderGroup = MockOrderGroups.Single(og => og.OrderGroupNumber == "REQUESTSJUSTCREATED");
            var requestGroup = orderGroup.RequestGroups.First();
            requestGroup.SetStatus(status, false);
            Assert.Throws<InvalidOperationException>(() => requestGroup.Decline(DateTime.Now, 10, null, "apa"));
        }

        [Theory]
        [InlineData(false, true, false, InterpreterLocation.OnSite)]
        [InlineData(true, true, false, InterpreterLocation.OnSite)]
        [InlineData(false, false, false, InterpreterLocation.OnSite)]
        [InlineData(true, false, false, InterpreterLocation.OnSite)]
        [InlineData(false, true, true, InterpreterLocation.OnSite)]
        [InlineData(true, true, true, InterpreterLocation.OnSite)]
        [InlineData(false, false, true, InterpreterLocation.OnSite)]
        [InlineData(true, false, true, InterpreterLocation.OnSite)]
        [InlineData(false, true, false, InterpreterLocation.OffSitePhone)]
        [InlineData(true, true, false, InterpreterLocation.OffSitePhone)]
        [InlineData(false, false, false, InterpreterLocation.OffSitePhone)]
        [InlineData(true, false, false, InterpreterLocation.OffSitePhone)]
        [InlineData(false, true, true, InterpreterLocation.OffSitePhone)]
        [InlineData(true, true, true, InterpreterLocation.OffSitePhone)]
        [InlineData(false, false, true, InterpreterLocation.OffSitePhone)]
        [InlineData(true, false, true, InterpreterLocation.OffSitePhone)]
        public void Accept_Valid(bool receive, bool hasTravelCosts, bool partialAnswer, InterpreterLocation actualLocation)
        {
            var orderGroup = MockOrderGroups.Single(og => og.OrderGroupNumber == "REQUESTGROUPALLOWEXCEEDINGJUSTCREATED");
            var requestGroup = orderGroup.RequestGroups.First();
            var expectedOrderStatus = hasTravelCosts && actualLocation == InterpreterLocation.OnSite ?
                partialAnswer ? OrderStatus.RequestAwaitingPartialAccept : OrderStatus.RequestResponded :
                partialAnswer ? OrderStatus.GroupAwaitingPartialResponse : OrderStatus.ResponseAccepted;
            var expectedRequestGroupStatus = hasTravelCosts && actualLocation == InterpreterLocation.OnSite ?
                partialAnswer ? RequestStatus.PartiallyAccepted : RequestStatus.Accepted :
                partialAnswer ? RequestStatus.PartiallyApproved : RequestStatus.Approved;

            var acceptAt = DateTime.Now;
            var userId = 10;
            var impersonatorId = (int?)null;

            if (receive)
            {
                requestGroup.Received(acceptAt, userId, impersonatorId);
            }
            requestGroup.Requests.ForEach(r => r.InterpreterLocation = (int?)actualLocation);
            requestGroup.Accept(acceptAt, userId, impersonatorId, Enumerable.Empty<RequestGroupAttachment>().ToList(), hasTravelCosts, partialAnswer);

            Assert.Equal(expectedOrderStatus, requestGroup.OrderGroup.Status);
            Assert.Equal(expectedRequestGroupStatus, requestGroup.Status);
            Assert.Equal(acceptAt, requestGroup.AnswerDate);
            Assert.Equal(userId, requestGroup.AnsweredBy);
            if (hasTravelCosts && actualLocation == InterpreterLocation.OnSite)
            {
                Assert.Null(requestGroup.AnswerProcessedAt);
            }
            else
            {
                Assert.Equal(acceptAt, requestGroup.AnswerProcessedAt);
            }
            Assert.Equal(impersonatorId, requestGroup.ImpersonatingAnsweredBy);
        }

        [Theory]
        [InlineData(RequestStatus.Accepted)]
        [InlineData(RequestStatus.Approved)]
        [InlineData(RequestStatus.AwaitingDeadlineFromCustomer)]
        [InlineData(RequestStatus.CancelledByCreator)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved)]
        [InlineData(RequestStatus.DeclinedByBroker)]
        [InlineData(RequestStatus.DeniedByCreator)]
        [InlineData(RequestStatus.DeniedByTimeLimit)]
        [InlineData(RequestStatus.LostDueToQuarantine)]
        [InlineData(RequestStatus.NoDeadlineFromCustomer)]
        [InlineData(RequestStatus.PartiallyAccepted)]
        [InlineData(RequestStatus.PartiallyApproved)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator)]
        [InlineData(RequestStatus.ToBeProcessedByBroker)]
        public void Accept_Invalid(RequestStatus status)
        {
            var orderGroup = MockOrderGroups.Single(og => og.OrderGroupNumber == "REQUESTSJUSTCREATED");
            var requestGroup = orderGroup.RequestGroups.First();
            requestGroup.SetStatus(status, false);
            Assert.Throws<InvalidOperationException>(() => requestGroup.Accept(DateTime.Now, 10, null, Enumerable.Empty<RequestGroupAttachment>().ToList(), false, false));
        }

        [Fact]
        public void ConfirmDenial()
        {
            var orderGroup = MockOrderGroups.Single(og => og.OrderGroupNumber == "REQUESTGROUPDENIED");
            var requestGroup = orderGroup.RequestGroups.First();
            requestGroup.ConfirmDenial(DateTimeOffset.Now, 1, null);
            Assert.Equal(1, requestGroup.StatusConfirmations.Count(r => r.RequestStatus == RequestStatus.DeniedByCreator));
        }

        // Invalid request status
        [Theory]
        [InlineData(RequestStatus.Accepted)]
        [InlineData(RequestStatus.Approved)]
        [InlineData(RequestStatus.AwaitingDeadlineFromCustomer)]
        [InlineData(RequestStatus.CancelledByCreator)]
        [InlineData(RequestStatus.CancelledByCreatorWhenApproved)]
        [InlineData(RequestStatus.Created)]
        [InlineData(RequestStatus.DeclinedByBroker)]
        [InlineData(RequestStatus.DeniedByTimeLimit)]
        [InlineData(RequestStatus.LostDueToQuarantine)]
        [InlineData(RequestStatus.NoDeadlineFromCustomer)]
        [InlineData(RequestStatus.PartiallyAccepted)]
        [InlineData(RequestStatus.PartiallyApproved)]
        [InlineData(RequestStatus.Received)]
        [InlineData(RequestStatus.ResponseNotAnsweredByCreator)]
        [InlineData(RequestStatus.ToBeProcessedByBroker)]
        public void ConfirmDenial_Invalid(RequestStatus status)
        {
            var orderGroup = MockOrderGroups.Single(og => og.OrderGroupNumber == "REQUESTSJUSTCREATED");
            var requestGroup = orderGroup.RequestGroups.First();
            requestGroup.SetStatus(status, false);
            Assert.Throws<InvalidOperationException>(() => requestGroup.ConfirmDenial(DateTimeOffset.Now, 1, null));
        }

        [Fact]
        public void AddRequestView()
        {
            var orderGroup = MockOrderGroups.Single(og => og.OrderGroupNumber == "REQUESTSJUSTCREATED");
            var requestGroup = orderGroup.RequestGroups.First();
            requestGroup.AddView(1, null, DateTimeOffset.Now);
            Assert.Equal(1, requestGroup.Views.Count(r => r.ViewedBy == 1));
        }

        [Fact]
        public void AddRequestView_Twice()
        {
            var orderGroup = MockOrderGroups.Single(og => og.OrderGroupNumber == "REQUESTSJUSTCREATED");
            var requestGroup = orderGroup.RequestGroups.First();
            requestGroup.AddView(1, null, DateTimeOffset.Now);
            requestGroup.AddView(1, null, DateTimeOffset.Now);
            Assert.Equal(1, requestGroup.Views.Count(r => r.ViewedBy == 1));
        }

    }
}