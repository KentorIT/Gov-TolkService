using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Xunit;

namespace Tolk.BusinessLogic.Tests.Entities
{
    public class RequisitionTests
    {
        private readonly Order MockOrder;
        private readonly Requisition MockRequisition;

        public RequisitionTests()
        {
            var mockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers);

            MockOrder = new Order(mockCustomerUsers[2], null, mockCustomerUsers[2].CustomerOrganisation, new DateTimeOffset(2018, 05, 07, 13, 00, 00, new TimeSpan(02, 00, 00)))
            {
                OrderId = 8,
                CustomerReferenceNumber = "EmptyOrder",
                OrderNumber = "2018-000008",
                Status = OrderStatus.Requested,
                Requests = new List<Request>()
            };

            MockRequisition = new Requisition
            {
                Status = RequisitionStatus.Created,
                Message = string.Empty,
                Request = new Request
                {
                    Status = RequestStatus.Approved,
                    Order = new Order(MockOrder)
                    {
                        Status = OrderStatus.RequestResponded,
                    },
                },
            };
            MockRequisition.Request.Order.Requests.Add(MockRequisition.Request);
            //when a requisition is created order get status Delivered
            MockRequisition.Request.Order.Status = OrderStatus.Delivered;
            MockRequisition.RequisitionStatusConfirmations = new List<RequisitionStatusConfirmation>();
        }

        [Theory]
        [InlineData(31, 15)]
        [InlineData(null, 15)]
        [InlineData(31, null)]
        [InlineData(null, null)]
        public void TimeWasteTotalTime(int? timeWasteNormalTime, int? timeWasteIWHTime)
        {
            var expectedTimeWasteTotalTime = (timeWasteNormalTime ?? 0) + (timeWasteIWHTime ?? 0);
            var requisition = new Requisition
            {
                TimeWasteNormalTime = timeWasteNormalTime,
                TimeWasteIWHTime = timeWasteIWHTime,
                Message = string.Empty
            };
            Assert.Equal(expectedTimeWasteTotalTime, requisition.TimeWasteTotalTime);
        }

        [Fact]
        public void SessionEndedAtSetterValidation()
        {
            var requisition = new Requisition
            {
                SessionStartedAt = DateTime.Parse("2019-01-31 12:23"),
                Message = string.Empty
            };
            Assert.Throws<ValidationException>(() => requisition.SessionEndedAt = DateTime.Parse("2019-01-30 12:00"));
        }

        [Fact]
        public void Review_Valid()
        {
            var requisition = MockRequisition;
            var approveTime = DateTime.Parse("2019-01-31 12:31");
            var userId = 10;
            var impersonatorId = (int?)null;

            requisition.Review(approveTime, userId, impersonatorId);

            Assert.Equal(RequisitionStatus.Reviewed, requisition.Status);
            Assert.Equal(OrderStatus.Delivered, requisition.Request.Order.Status);
            Assert.Equal(approveTime, requisition.ProcessedAt);
            Assert.Equal(userId, requisition.ProcessedBy);
            Assert.Equal(impersonatorId, requisition.ImpersonatingProcessedBy);
        }

        [Theory]
        [InlineData(RequisitionStatus.Reviewed)]
        [InlineData(RequisitionStatus.AutomaticGeneratedFromCancelledOrder)]
        [InlineData(RequisitionStatus.Commented)]
        public void Review_Invalid(RequisitionStatus status)
        {
            var requisition = new Requisition { Status = status, Message = string.Empty };
            Assert.Throws<InvalidOperationException>(() => requisition.Review(DateTime.Now, 10, null));
        }

        [Fact]
        public void Comment_Valid()
        {
            var requisition = new Requisition { Status = RequisitionStatus.Created, Message = string.Empty };
            var commentTime = DateTime.Parse("2019-01-31 12:31");
            var userId = 10;
            var impersonatorId = (int?)null;
            var comment = "Commented!";

            requisition.Comment(commentTime, userId, impersonatorId, comment);

            Assert.Equal(RequisitionStatus.Commented, requisition.Status);
            Assert.Equal(commentTime, requisition.ProcessedAt);
            Assert.Equal(userId, requisition.ProcessedBy);
            Assert.Equal(impersonatorId, requisition.ImpersonatingProcessedBy);
            Assert.Equal(comment, requisition.CustomerComment);
        }

        [Theory]
        [InlineData(RequisitionStatus.Reviewed)]
        [InlineData(RequisitionStatus.AutomaticGeneratedFromCancelledOrder)]
        [InlineData(RequisitionStatus.Commented)]
        public void Comment_Invalid(RequisitionStatus status)
        {
            var requisition = new Requisition { Status = status, Message = string.Empty };
            Assert.Throws<InvalidOperationException>(() => requisition.Comment(DateTime.Now, 10, null, "Test"));
        }

        [Fact]
        public void ConfirmNoReview_Valid()
        {
            var requisition = MockRequisition;
            var confirmTime = DateTime.Parse("2019-01-31 12:31");
            var userId = 10;
            var impersonatorId = (int?)null;

            requisition.CofirmNoReview(confirmTime, userId, impersonatorId);

            Assert.Equal(RequisitionStatus.Created, requisition.Status);
            Assert.Equal(OrderStatus.Delivered, requisition.Request.Order.Status);
            Assert.Single(requisition.RequisitionStatusConfirmations.Where(r => r.RequisitionStatus == RequisitionStatus.Created));
            Assert.Equal(requisition.RequisitionStatusConfirmations.Single(r => r.RequisitionStatus == RequisitionStatus.Created).ConfirmedBy, userId);
            Assert.Equal(requisition.RequisitionStatusConfirmations.Single(r => r.RequisitionStatus == RequisitionStatus.Created).ImpersonatingConfirmedBy, impersonatorId);
            Assert.Equal(requisition.RequisitionStatusConfirmations.Single(r => r.RequisitionStatus == RequisitionStatus.Created).ConfirmedAt, confirmTime);
        }


        [Theory]
        [InlineData(RequisitionStatus.Reviewed)]
        [InlineData(RequisitionStatus.AutomaticGeneratedFromCancelledOrder)]
        [InlineData(RequisitionStatus.Commented)]
        public void ConfirmNoReview_Invalid(RequisitionStatus status)
        {
            var requisition = new Requisition { Status = status, Message = string.Empty };
            Assert.Throws<InvalidOperationException>(() => requisition.CofirmNoReview(DateTime.Now, 10, null));
        }

        [Fact]
        public void ConfirmNoReview_AlreadyConfirmed_Invalid()
        {
            var requisition = MockRequisition;
            var confirmTime = DateTime.Parse("2019-01-31 12:31");
            var userId = 10;
            var impersonatorId = (int?)null;

            requisition.CofirmNoReview(confirmTime, userId, impersonatorId);

            Assert.Equal(RequisitionStatus.Created, requisition.Status);
            Assert.Equal(OrderStatus.Delivered, requisition.Request.Order.Status);
            Assert.Single(requisition.RequisitionStatusConfirmations.Where(r => r.RequisitionStatus == RequisitionStatus.Created));
            Assert.Equal(requisition.RequisitionStatusConfirmations.Single(r => r.RequisitionStatus == RequisitionStatus.Created).ConfirmedBy, userId);
            Assert.Equal(requisition.RequisitionStatusConfirmations.Single(r => r.RequisitionStatus == RequisitionStatus.Created).ImpersonatingConfirmedBy, impersonatorId);
            Assert.Equal(requisition.RequisitionStatusConfirmations.Single(r => r.RequisitionStatus == RequisitionStatus.Created).ConfirmedAt, confirmTime);
            //assert that exception is thrown if already confirmed
            Assert.Throws<InvalidOperationException>(() => requisition.CofirmNoReview(confirmTime.AddHours(1), userId, impersonatorId));
        }
    }
}
