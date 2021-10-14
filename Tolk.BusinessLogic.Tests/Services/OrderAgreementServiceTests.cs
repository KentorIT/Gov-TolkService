using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Xunit;

namespace Tolk.BusinessLogic.Tests.Services
{
    public class OrderAgreementServiceTests
    {
        private const string DbNameWithRequisitionData = "OrderAgreementService_WithRequisitionData";
        private const string DbNameWithRequestData = "OrderAgreementService_WithRequestData";
        private readonly ILogger<OrderAgreementService> _logger;
        private readonly StubSwedishClock _clock;
        private const string OrderNumber = "2018-123456";
        public OrderAgreementServiceTests()
        {
            _logger = Mock.Of<ILogger<OrderAgreementService>>();
            _clock = new StubSwedishClock("2018-12-12 00:00:00");
        }

        private TolkDbContext GetContext(string name)
        {
            var tolkDbContext = CreateTolkDbContext(name);
            var mockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers);
            var mockOrder = new Order(mockCustomerUsers[2], null, mockCustomerUsers[2].CustomerOrganisation, new DateTimeOffset(2018, 05, 07, 13, 00, 00, new TimeSpan(02, 00, 00)))
            {
                OrderId = 8,
                CustomerReferenceNumber = "EmptyOrder",
                OrderNumber = "2018-000008",
                Status = OrderStatus.Requested,
                Requests = new List<Request>()
            };
            var mockRequest = new Request
            {
                RequestId = 1,
                Status = RequestStatus.Delivered,
                Order = new Order(mockOrder)
                {
                    OrderNumber = OrderNumber,
                    Status = OrderStatus.RequestResponded,
                },
                Ranking = new Ranking { RankingId = 1, Broker = new Broker { Name = "MockBroker", OrganizationNumber = "123123-1234" }, Rank = 1 },
            };
            var mockRequisition = new Requisition
            {
                RequisitionId = 1,
                Status = RequisitionStatus.Created,
                SessionStartedAt = new DateTime(2018, 12, 10, 10, 10, 10),
                SessionEndedAt = new DateTime(2018, 12, 10, 12, 10, 10),

                Request = mockRequest
            };

            tolkDbContext.Add(mockRequisition);
            tolkDbContext.Add(mockRequest);
            tolkDbContext.AddRange(MockEntities.MockRequisitionPriceRows);
            tolkDbContext.AddRange(MockEntities.MockRequestPriceRows);
            tolkDbContext.SaveChanges();
            return tolkDbContext;
        }

        private TolkDbContext CreateTolkDbContext(string databaseName = "empty")
        {
            var options = new DbContextOptionsBuilder<TolkDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new TolkDbContext(options);
        }

        [Fact]
        public async Task CreateValidOrderAgreementDocument()
        {
            using var tolkdbContext = GetContext(DbNameWithRequisitionData);
            var service = new OrderAgreementService(tolkdbContext, _logger, _clock);
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            await service.CreateOrderAgreementFromRequisition(1, writer);
            memoryStream.Position = 0;
            StreamReader sr = new StreamReader(memoryStream);
            var root = XElement.Load(sr);
            var elements = from el in root.Elements() where el.Name.LocalName == "OrderLine" select el;
            Assert.Equal(4, elements.Count());
        }

        [Fact]
        public async Task CreateValidOrderAgreementDocumentFromRequest()
        {
            using var tolkdbContext = GetContext(DbNameWithRequestData);
            var service = new OrderAgreementService(tolkdbContext, _logger, _clock);
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            await service.CreateOrderAgreementFromRequest(1, writer);
            memoryStream.Position = 0;
            StreamReader sr = new StreamReader(memoryStream);
            var root = XElement.Load(sr);
            var elements = from el in root.Elements() where el.Name.LocalName == "OrderLine" select el;
            Assert.Equal(4, elements.Count());
        }
    }
}
