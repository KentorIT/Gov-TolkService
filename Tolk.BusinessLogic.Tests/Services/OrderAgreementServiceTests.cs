﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Xunit;

namespace Tolk.BusinessLogic.Tests.Services
{
    public class OrderAgreementServiceTests
    {
        private const string DbNameWithData = nameof(DbNameWithData);
        private const string DbNameWithClearedData = nameof(DbNameWithClearedData);
        private readonly ILogger<OrderAgreementService> _logger;
        private readonly StubSwedishClock _clock;
        private const string OrderNumber = "2018-123456";
        private CacheService _cache;
        public OrderAgreementServiceTests()
        {
            _logger = Mock.Of<ILogger<OrderAgreementService>>();
            _clock = new StubSwedishClock("2018-12-12 00:00:00");
        }
        private OrderAgreementService CreateOrderAgreementService(TolkDbContext dbContext, StubSwedishClock clock = null)
        {
            IDistributedCache cache = Mock.Of<IDistributedCache>();
            TolkBaseOptionsService optionService = new TolkBaseOptionsService(Options.Create(new TolkOptions() { RoundPriceDecimals = true }));
            _cache = new CacheService(cache, dbContext, optionService, _clock);
            var emailService = new EmailService(Mock.Of<ILogger<EmailService>>(), Options.Create(new TolkOptions()), _clock);
            return new OrderAgreementService(dbContext, _logger, clock ?? _clock, _cache, new DateCalculationService(_cache), optionService, emailService);
        }

        private TolkDbContext GetContext(string name, bool customerHasOrderAgreementSetting = true)
        {
            var tolkDbContext = CreateTolkDbContext(name);
            var mockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers);

            var mockCustomerSettings = new CustomerSetting
            {
                CustomerOrganisationId = 1,
                CustomerSettingType = CustomerSettingType.UseOrderAgreements,
                Value = customerHasOrderAgreementSetting
            };
            for (int i = 1; i < 3; i++)
            {
                var mockOrder = CreateMockOrder(mockCustomerUsers[0], 7 + i, "2018-000008");
                var mockRequest = CreateMockRequest(mockOrder, (FrameworkAgreementResponseRuleset)i, OrderNumber, requestId: i, rankingId: i, brokerId: i);
                var mockRequisition = CreateMockRequisition(mockRequest, requisitionId: i);

                if (!tolkDbContext.Requisitions.Any(r => r.RequisitionId == mockRequisition.RequisitionId))
                {
                    tolkDbContext.Add(mockRequisition);
                }
                if (!tolkDbContext.Requests.Any(r => r.RequestId == mockRequest.RequestId))
                {
                    tolkDbContext.Add(mockRequest);
                }
            }

            if (!tolkDbContext.CustomerSettings.Any(r => r.CustomerOrganisationId == mockCustomerSettings.CustomerOrganisationId))
            {
                tolkDbContext.Add(mockCustomerSettings);
            }
            else
            {
                tolkDbContext.CustomerSettings.Where(r => r.CustomerOrganisationId == mockCustomerSettings.CustomerOrganisationId && r.CustomerSettingType == mockCustomerSettings.CustomerSettingType).Single().Value = customerHasOrderAgreementSetting;
            }
            tolkDbContext.AddRange(MockEntities.MockRequisitionPriceRows.Where(pr =>
                !tolkDbContext.RequisitionPriceRows.Select(r => r.RequisitionPriceRowId).Contains(pr.RequisitionPriceRowId)));
            tolkDbContext.AddRange(MockEntities.MockRequestPriceRows.Where(pr =>
                !tolkDbContext.RequestPriceRows.Select(r => r.RequestPriceRowId).Contains(pr.RequestPriceRowId)));
            tolkDbContext.OrderAgreementPayloads.FromSqlRaw("delete from OrderAgreementPayloads");
            tolkDbContext.SaveChanges();
            return tolkDbContext;
        }

        private Order CreateMockOrder(AspNetUser mockCostumerUser, int orderId, string orderNumber)
        {
            return new Order(mockCostumerUser, null, mockCostumerUser.CustomerOrganisation, new DateTimeOffset(2018, 05, 07, 13, 00, 00, new TimeSpan(02, 00, 00)))
            {
                OrderId = orderId,
                CustomerReferenceNumber = "EmptyOrder",
                OrderNumber = orderNumber,
                Status = OrderStatus.Requested,
                Requests = new List<Request>()
            };
        }

        private Request CreateMockRequest(Order mockOrder, FrameworkAgreementResponseRuleset ruleset, string orderNumber, int requestId, int rankingId, int brokerId)
        {
            return new Request
            {
                RequestId = requestId,
                Status = RequestStatus.Delivered,
                Order = new Order(mockOrder)
                {
                    CustomerOrganisationId = 1,
                    OrderNumber = orderNumber,
                    Status = OrderStatus.RequestRespondedAwaitingApproval,
                    EndAt = DateTime.Parse("2018-12-10 10:00:00").ToDateTimeOffsetSweden()
                },
                Ranking = new Ranking { RankingId = rankingId, Broker = new Broker { BrokerId = brokerId, Name = "MockBroker", OrganizationNumber = "123123-1234" }, Rank = 1, FrameworkAgreement = MockEntities.FrameworkAgreements.First(f => f.FrameworkAgreementResponseRuleset == ruleset) },
            };
        }

        private Requisition CreateMockRequisition(Request mockRequest, int requisitionId)
        {
            return new Requisition
            {
                Message = string.Empty,
                RequisitionId = requisitionId,
                Status = RequisitionStatus.Created,
                SessionStartedAt = new DateTime(2018, 12, 10, 10, 10, 10),
                SessionEndedAt = new DateTime(2018, 12, 10, 12, 10, 10),

                Request = mockRequest
            };
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
            using var tolkDbContext = GetContext(DbNameWithData);
            var service = CreateOrderAgreementService(tolkDbContext);
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            var idNumber = await service.CreateOrderAgreementFromRequisition(1, writer);
            memoryStream.Position = 0;
            StreamReader sr = new StreamReader(memoryStream);
            var root = XElement.Load(sr);
            var elements = from el in root.Elements() where el.Name.LocalName == "OrderLine" select el;
            Assert.Equal(4, elements.Count());
            Assert.Equal(idNumber, (from el in root.Elements() where el.Name.LocalName == "ID" select el).First().Value);
        }

        [Fact]
        public async Task CreateValidOrderAgreementDocumentFromRequest()
        {
            using var tolkDbContext = GetContext(DbNameWithData);
            var service = CreateOrderAgreementService(tolkDbContext);
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            var idNumber = await service.CreateOrderAgreementFromRequest(1, writer);
            memoryStream.Position = 0;
            StreamReader sr = new StreamReader(memoryStream);
            var root = XElement.Load(sr);
            var elements = from el in root.Elements() where el.Name.LocalName == "OrderLine" select el;
            Assert.Equal(4, elements.Count());
            Assert.Equal(idNumber, (from el in root.Elements() where el.Name.LocalName == "ID" select el).First().Value);
        }

        [Fact]
        public async Task HandleOrderAgreementCreationNotOnSunday()
        {
            using var tolkDbContext = GetContext(DbNameWithData);
            var service = CreateOrderAgreementService(tolkDbContext, new StubSwedishClock("2021-10-24 00:00:00"));
            Assert.False(await service.HandleOrderAgreementCreation());
        }
        [Fact]
        public async Task HandleOrderAgreementCreation()
        {
            using var tolkDbContext = GetContext(DbNameWithData);
            var service = CreateOrderAgreementService(tolkDbContext, new StubSwedishClock("2021-10-25 10:00:00"));
            Assert.True(await service.HandleOrderAgreementCreation());
            await tolkDbContext.SaveChangesAsync();
            Assert.Equal(2, tolkDbContext.OrderAgreementPayloads.Count());
        }
        [Fact]
        public async Task HandleOrderAgreementCreation_CustomerDoesNotUseORderAgreements()
        {
            using var tolkDbContext = GetContext(DbNameWithClearedData, false);
            var service = CreateOrderAgreementService(tolkDbContext, new StubSwedishClock("2021-10-25 10:00:00"));
            Assert.True(await service.HandleOrderAgreementCreation());
            await tolkDbContext.SaveChangesAsync();
            Assert.Equal(0, tolkDbContext.OrderAgreementPayloads.Count());
        }

        [Theory]
        [InlineData(1, FrameworkAgreementResponseRuleset.VersionOne)]
        [InlineData(2, FrameworkAgreementResponseRuleset.VersionTwo)]
        public async Task ContractIDShouldMatchFrameworkAgreementNumber_WhenCreatedFromRequisition(int requsitionId, FrameworkAgreementResponseRuleset ruleset)
        {
            using var tolkDbContext = GetContext(DbNameWithData);
            var service = CreateOrderAgreementService(tolkDbContext);
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            await service.CreateOrderAgreementFromRequisition(requsitionId, writer);
            memoryStream.Position = 0;
            StreamReader sr = new StreamReader(memoryStream);
            var root = XElement.Load(sr);
            Assert.Equal(MockEntities.FrameworkAgreements.First(f => f.FrameworkAgreementResponseRuleset == ruleset).AgreementNumber, root.Elements()
                .Where(e => e.Name.LocalName == "Contract")
                .Elements().Where(e => e.Name.LocalName == "ID")
                .Select(e => e.Value).Single());
        }

        [Theory]
        [InlineData(1, FrameworkAgreementResponseRuleset.VersionOne)]
        [InlineData(2, FrameworkAgreementResponseRuleset.VersionTwo)]
        public async Task ContractIDShouldMatchFrameworkAgreementNumber_WhenCreatedFromRequest(int requsitionId, FrameworkAgreementResponseRuleset ruleset)
        {
            using var tolkDbContext = GetContext(DbNameWithData);
            var service = CreateOrderAgreementService(tolkDbContext);
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            var idNumber = await service.CreateOrderAgreementFromRequest(requsitionId, writer);
            memoryStream.Position = 0;
            StreamReader sr = new StreamReader(memoryStream);
            var root = XElement.Load(sr);
            Assert.Equal(MockEntities.FrameworkAgreements.First(f => f.FrameworkAgreementResponseRuleset == ruleset).AgreementNumber, root.Elements()
                .Where(e => e.Name.LocalName == "Contract")
                .Elements().Where(e => e.Name.LocalName == "ID")
                .Select(e => e.Value).Single());
        }
    }
}
