using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Xunit;

namespace Tolk.BusinessLogic.Tests.Entities
{
    public class RequisitionTests
    {
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
            };
            Assert.Equal(expectedTimeWasteTotalTime, requisition.TimeWasteTotalTime);
        }

        [Fact]
        public void SessionEndedAtSetterValidation()
        {
            var requisition = new Requisition
            {
                SessionStartedAt = DateTime.Parse("2019-01-31 12:23"),
            };
            Assert.Throws<ValidationException>(() => requisition.SessionEndedAt = DateTime.Parse("2019-01-30 12:00"));
        }

        [Fact]
        public void Review_Valid()
        {
            var requisition = new Requisition
            {
                Status = RequisitionStatus.Created,
                Request = new Request
                {
                    Status = RequestStatus.Approved,
                    Order = new Order
                    {
                        Status = OrderStatus.RequestResponded,
                        Requests = new List<Request>(),
                    },
                },
            };
            requisition.Request.Order.Requests.Add(requisition.Request);
            //when a requisition is created order get status Delivered
            requisition.Request.Order.Status = OrderStatus.Delivered;
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
            var requisition = new Requisition { Status = status };
            Assert.Throws<InvalidOperationException>(() => requisition.Review(DateTime.Now, 10, null));
        }

        [Fact]
        public void Comment_Valid()
        {
            var requisition = new Requisition { Status = RequisitionStatus.Created };
            var approveTime = DateTime.Parse("2019-01-31 12:31");
            var userId = 10;
            var impersonatorId = (int?)null;
            var comment = "Commented!";

            requisition.Comment(approveTime, userId, impersonatorId, comment);

            Assert.Equal(RequisitionStatus.Commented, requisition.Status);
            Assert.Equal(approveTime, requisition.ProcessedAt);
            Assert.Equal(userId, requisition.ProcessedBy);
            Assert.Equal(impersonatorId, requisition.ImpersonatingProcessedBy);
            Assert.Equal(comment, requisition.DenyMessage);
        }

        [Theory]
        [InlineData(RequisitionStatus.Reviewed)]
        [InlineData(RequisitionStatus.AutomaticGeneratedFromCancelledOrder)]
        [InlineData(RequisitionStatus.Commented)]
        public void Comment_Invalid(RequisitionStatus status)
        {
            var requisition = new Requisition { Status = status };
            Assert.Throws<InvalidOperationException>(() => requisition.Comment(DateTime.Now, 10, null, "Test"));
        }
    }
}
