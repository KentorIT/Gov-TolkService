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
            var filter = new ComplaintFilterModel
            {
                OrderNumber = "3"
            };

            var list = filter.Apply(complaints.AsQueryable());

            list.Should().HaveCount(2);
            list.Should().Contain(new[] { complaints[2], complaints[4] });
        }

        [Fact]
        public void ComplaintFilter_ByStatus()
        {
            var filter = new ComplaintFilterModel
            {
                Status = ComplaintStatus.Confirmed
            };

            var list = filter.Apply(complaints.AsQueryable());

            list.Should().HaveCount(2);
            list.Should().Contain(new[] { complaints[0], complaints[1] });
        }

        [Fact]
        public void ComplaintFilter_ComboByOrderNumberStatus()
        {
            var filter = new ComplaintFilterModel
            {
                OrderNumber = "5",
                Status = ComplaintStatus.Created
            };

            var list = filter.Apply(complaints.AsQueryable());

            list.Should().OnlyContain(c => c == complaints[5]);
        }
    }
}
