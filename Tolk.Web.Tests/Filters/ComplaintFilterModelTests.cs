using FluentAssertions;
using System;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Tolk.Web.Models;
using Xunit;

namespace Tolk.Web.Tests.Filters
{
    public class ComplaintFilterModelTests
    {
        private Complaint[] complaints;

        public ComplaintFilterModelTests()
        {
            var mockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers);

            complaints = new[]
            {
                new Complaint { Request = new Request{ Order = new Order(mockCustomerUsers[0], null, mockCustomerUsers[0].CustomerOrganisation, DateTimeOffset.Now){ OrderNumber = "2018-000000" } }, Status = ComplaintStatus.Confirmed },
                new Complaint { Request = new Request{ Order = new Order(mockCustomerUsers[0], null, mockCustomerUsers[0].CustomerOrganisation, DateTimeOffset.Now){ OrderNumber = "2018-000011"} }, Status = ComplaintStatus.Confirmed },
                new Complaint { Request = new Request{ Order = new Order(mockCustomerUsers[0], null, mockCustomerUsers[0].CustomerOrganisation, DateTimeOffset.Now){ OrderNumber = "2018-000305"} }, Status = ComplaintStatus.DisputePendingTrial },
                new Complaint { Request = new Request{ Order = new Order(mockCustomerUsers[0], null, mockCustomerUsers[0].CustomerOrganisation, DateTimeOffset.Now){ OrderNumber = "2018-000104"} }, Status = ComplaintStatus.TerminatedTrialConfirmedComplaint },
                new Complaint { Request = new Request{ Order = new Order(mockCustomerUsers[0], null, mockCustomerUsers[0].CustomerOrganisation, DateTimeOffset.Now){ OrderNumber = "2018-000331"} }, Status = ComplaintStatus.Created },
                new Complaint { Request = new Request{ Order = new Order(mockCustomerUsers[0], null, mockCustomerUsers[0].CustomerOrganisation, DateTimeOffset.Now){ OrderNumber = "2018-000502"} }, Status = ComplaintStatus.Created },
                new Complaint { Request = new Request{ Order = new Order(mockCustomerUsers[0], null, mockCustomerUsers[0].CustomerOrganisation, DateTimeOffset.Now){ OrderNumber = "2018-000971"} }, Status = ComplaintStatus.Created },
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
            var actual = complaints.Where(c => c.Request.Order.OrderNumber.Contains(orderNum));

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
            var actual = complaints.Where(c => c.Request.Order.OrderNumber.Contains(orderNum)
                && c.Status == status).Single();

            list.Should().OnlyContain(c => c == actual);
        }
    }
}
