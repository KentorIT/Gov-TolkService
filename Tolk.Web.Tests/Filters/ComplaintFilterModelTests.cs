using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Models;
using Xunit;

namespace Tolk.Web.Tests.Filters
{
    public class ComplaintFilterModelTests
    {
        private ComplaintListItemModel[] complaints;

        public ComplaintFilterModelTests()
        {
            complaints = new[]
            {
                new ComplaintListItemModel { OrderNumber = "2018-000000", Status = ComplaintStatus.Confirmed },
                new ComplaintListItemModel { OrderNumber = "2018-000011", Status = ComplaintStatus.Confirmed },
                new ComplaintListItemModel { OrderNumber = "2018-000305", Status = ComplaintStatus.DisputePendingTrial },
                new ComplaintListItemModel { OrderNumber = "2018-000104", Status = ComplaintStatus.TerminatedTrialConfirmedComplaint },
                new ComplaintListItemModel { OrderNumber = "2018-000331", Status = ComplaintStatus.Created },
                new ComplaintListItemModel { OrderNumber = "2018-000502", Status = ComplaintStatus.Created },
                new ComplaintListItemModel { OrderNumber = "2018-000971", Status = ComplaintStatus.Created },
            };
        }

        [Fact]
        public void ComplaintFilter_ByOrderNumber()
        {
            var orderNum = "3";
            var filter = new ComplaintFilterModel
            {
                OrderNumber = orderNum
            };

            var list = filter.Apply(complaints.AsQueryable());
            var actual = complaints.Where(c => c.OrderNumber.Contains(orderNum));

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }

        [Fact]
        public void ComplaintFilter_ByStatus()
        {
            var status = ComplaintStatus.Confirmed;
            var filter = new ComplaintFilterModel
            {
                Status = status
            };

            var list = filter.Apply(complaints.AsQueryable());
            var actual = complaints.Where(c => c.Status == status);

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }

        [Fact]
        public void ComplaintFilter_ComboByOrderNumberStatus()
        {
            var orderNum = "5";
            var status = ComplaintStatus.Created;
            var filter = new ComplaintFilterModel
            {
                OrderNumber = orderNum,
                Status = status
            };

            var list = filter.Apply(complaints.AsQueryable());
            var actual = complaints.Where(c => c.OrderNumber.Contains(orderNum)
                && c.Status == status).Single();

            list.Should().OnlyContain(c => c == actual);
        }
    }
}
