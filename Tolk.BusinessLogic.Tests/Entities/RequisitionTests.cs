using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
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
        public void Approve_Valid()
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
            requisition.Request.Order.Status = OrderStatus.ResponseAccepted;
            var approveTime = DateTime.Parse("2019-01-31 12:31");
            var userId = 10;
            var impersonatorId = (int?)null;

            requisition.Approve(approveTime, userId, impersonatorId);

            Assert.Equal(RequisitionStatus.Approved, requisition.Status);
            Assert.Equal(OrderStatus.DeliveryAccepted, requisition.Request.Order.Status);
            Assert.Equal(approveTime, requisition.ProcessedAt);
            Assert.Equal(userId, requisition.ProcessedBy);
            Assert.Equal(impersonatorId, requisition.ImpersonatingProcessedBy);
        }

        [Theory]
        [InlineData(RequisitionStatus.Approved)]
        [InlineData(RequisitionStatus.AutomaticApprovalFromCancelledOrder)]
        [InlineData(RequisitionStatus.DeniedByCustomer)]
        public void Approve_Invalid(RequisitionStatus status)
        {
            var requisition = new Requisition { Status = status };
            Assert.Throws<InvalidOperationException>(() => requisition.Approve(DateTime.Now, 10, null));
        }

        [Fact]
        public void Deny_Valid()
        {
            var requisition = new Requisition { Status = RequisitionStatus.Created };
            var approveTime = DateTime.Parse("2019-01-31 12:31");
            var userId = 10;
            var impersonatorId = (int?)null;
            var denyMessage = "Denied!";

            requisition.Deny(approveTime, userId, impersonatorId, denyMessage);

            Assert.Equal(RequisitionStatus.DeniedByCustomer, requisition.Status);
            Assert.Equal(approveTime, requisition.ProcessedAt);
            Assert.Equal(userId, requisition.ProcessedBy);
            Assert.Equal(impersonatorId, requisition.ImpersonatingProcessedBy);
            Assert.Equal(denyMessage, requisition.DenyMessage);
        }

        [Theory]
        [InlineData(RequisitionStatus.Approved)]
        [InlineData(RequisitionStatus.AutomaticApprovalFromCancelledOrder)]
        [InlineData(RequisitionStatus.DeniedByCustomer)]
        public void Deny_Invalid(RequisitionStatus status)
        {
            var requisition = new Requisition { Status = status };
            Assert.Throws<InvalidOperationException>(() => requisition.Deny(DateTime.Now, 10, null, "Test"));
        }
    }
}
