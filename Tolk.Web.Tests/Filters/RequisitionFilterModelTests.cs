using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Models;
using Tolk.Web.Tests.TestHelpers;
using Xunit;
using Tolk.BusinessLogic.Tests.TestHelpers;

namespace Tolk.Web.Tests.Filters
{
    public class RequisitionFilterModelTests
    {
        private Language[] mockLanguages;
        private Requisition[] mockRequisitions;

        public RequisitionFilterModelTests()
        {
            mockLanguages = MockEntities.MockLanguages;
            var mockRankings = MockEntities.MockRankings;
            var mockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers);
            var mockOrders = MockEntities.MockOrders(mockLanguages, mockRankings, mockCustomerUsers);
            mockRequisitions = MockEntities.MockRequisitions(mockOrders);
        }

        [Fact]
        public void RequisitionFilter_ByOrderNumber()
        {
            var orderNum = "1337";
            var filter = new RequisitionFilterModel
            {
                OrderNumber = orderNum
            };

            var list = filter.Apply(mockRequisitions.AsQueryable());
            var actual = mockRequisitions.Where(r => r.Request.Order.OrderNumber.Contains(orderNum));

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }

        [Fact]
        public void RequisitionFilter_ByLanguage()
        {
            var language = mockLanguages.Where(l => l.Name == "French").Single();
            var filter = new RequisitionFilterModel
            {
                LanguageId = language.LanguageId
            };

            var list = filter.Apply(mockRequisitions.AsQueryable());
            var actual = mockRequisitions.Where(r => r.Request.Order.Language == language);

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }

        [Fact]
        public void RequisitionFilter_ByDateRange()
        {
            var filter = new RequisitionFilterModel
            {
                DateRange = new DateRange { Start = new DateTime(2018, 07, 01), End = new DateTime(2018, 11, 01) }
            };

            var list = filter.Apply(mockRequisitions.AsQueryable());

            list.Should().HaveCount(3);
            list.Should().Contain(new[] { mockRequisitions[1], mockRequisitions[2], mockRequisitions[3] });
        }

        [Fact]
        public void RequisitionFilter_ByStatus()
        {
            var filter = new RequisitionFilterModel
            {
                Status = BusinessLogic.Enums.RequisitionStatus.Reviewed
            };

            var list = filter.Apply(mockRequisitions.AsQueryable());
            var actual = mockRequisitions.Where(r => r.Status == BusinessLogic.Enums.RequisitionStatus.Reviewed);

            list.Should().HaveCount(actual.Count());
            list.Should().Contain(actual);
        }

        [Fact]
        public void RequisitionFilter_ComboDateStatusLanguage()
        {
            var filter = new RequisitionFilterModel
            {
                DateRange = new DateRange { Start = new DateTime(2018, 06, 01), End = new DateTime(2018, 10, 01) },
                Status = BusinessLogic.Enums.RequisitionStatus.Reviewed,
                LanguageId = mockLanguages.Where(l => l.Name == "Chinese").Single().LanguageId
            };

            var list = filter.Apply(mockRequisitions.AsQueryable());

            list.Should().OnlyContain(r => r == mockRequisitions[2]);
        }
    }
}
