using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly ILogger<StandardBusinessDocumentService> _logger;
        private readonly StubSwedishClock _clock;
        private const string OrderNumberVersionOne = "2018-123456";
        private const string OrderNumberVersionTwo = "2018-123457";
        private CacheService _cache;
        public OrderAgreementServiceTests()
        {
            _logger = Mock.Of<ILogger<StandardBusinessDocumentService>>();
            _clock = new StubSwedishClock("2018-12-12 00:00:00");
        }
        private StandardBusinessDocumentService CreateStandardBusinessDocumentService(TolkDbContext dbContext, StubSwedishClock clock = null)
        {
            IDistributedCache cache = Mock.Of<IDistributedCache>();
            TolkBaseOptionsService optionService = new TolkBaseOptionsService(Options.Create(new TolkOptions() { RoundPriceDecimals = true }));
            _cache = new CacheService(cache, dbContext, optionService, _clock);
            var emailService = new EmailService(Mock.Of<ILogger<EmailService>>(), Options.Create(new TolkOptions()), _clock);
            return new StandardBusinessDocumentService(_logger, clock ?? _clock, dbContext, _cache, optionService, new DateCalculationService(_cache), emailService);
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
                var mockRequest = CreateMockRequest(mockOrder, (FrameworkAgreementResponseRuleset)i, OrderNumberVersionOne, requestId: i, rankingId: i, brokerId: i);
                var mockRequisition = CreateMockRequisitionWithSpecificId(mockRequest, requisitionId: i);

                if (!tolkDbContext.Requisitions.Any(r => r.RequisitionId == mockRequisition.RequisitionId))
                {
                    tolkDbContext.Add(mockRequisition);
                }
                if (!tolkDbContext.Requests.Any(r => r.RequestId == mockRequest.RequestId))
                {
                    tolkDbContext.Add(mockRequest);
                }

                var requestPriceRows = GetRequestPriceRows(mockRequest.RequestId);
                var requisitionPriceRows = GetRequisitionPriceRows(mockRequisition.RequisitionId);

                tolkDbContext.AddRange(requestPriceRows.Where(pr =>
                        !tolkDbContext.RequestPriceRows.Select(r => r.RequestId).Contains(pr.RequestId)));
                tolkDbContext.AddRange(requisitionPriceRows.Where(pr =>
                         !tolkDbContext.RequisitionPriceRows.Select(r => r.RequisitionId).Contains(pr.RequisitionId)));

            }

            if (!tolkDbContext.CustomerSettings.Any(r => r.CustomerOrganisationId == mockCustomerSettings.CustomerOrganisationId))
            {
                tolkDbContext.Add(mockCustomerSettings);
            }
            else
            {
                tolkDbContext.CustomerSettings.Where(r => r.CustomerOrganisationId == mockCustomerSettings.CustomerOrganisationId && r.CustomerSettingType == mockCustomerSettings.CustomerSettingType).Single().Value = customerHasOrderAgreementSetting;
            }

            tolkDbContext.PeppolPayloads.FromSqlRaw("delete from PeppolPayloads");
            tolkDbContext.SaveChanges();
            return tolkDbContext;
        }

        private TolkDbContext GetContextWithoutRequisition(string name, bool customerHasOrderAgreementSetting = true)
        {
            var tolkDbContext = CreateTolkDbContext(name);
            var mockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers);
            var id = 1;
            var mockCustomerSettings = new CustomerSetting
            {
                CustomerOrganisationId = id,
                CustomerSettingType = CustomerSettingType.UseOrderAgreements,
                Value = customerHasOrderAgreementSetting
            };
            var mockRequestAgreementVersionOne = CreateMockRequest(CreateMockOrder(mockCustomerUsers[0], id, OrderNumberVersionOne), FrameworkAgreementResponseRuleset.VersionOne, OrderNumberVersionOne, requestId: id, rankingId: id, brokerId: id);

            id = 2;
            var mockRequestAgreementVersionTwo = CreateMockRequest(CreateMockOrder(mockCustomerUsers[0], id, OrderNumberVersionTwo), FrameworkAgreementResponseRuleset.VersionTwo, OrderNumberVersionTwo, requestId: id, rankingId: id, brokerId: id);

            if (!tolkDbContext.Requests.Any(r => r.RequestId == mockRequestAgreementVersionOne.RequestId))
            {
                tolkDbContext.Add(mockRequestAgreementVersionOne);
            }

            if (!tolkDbContext.Requests.Any(r => r.RequestId == mockRequestAgreementVersionTwo.RequestId))
            {
                tolkDbContext.Add(mockRequestAgreementVersionTwo);
            }

            if (!tolkDbContext.CustomerSettings.Any(r => r.CustomerOrganisationId == mockCustomerSettings.CustomerOrganisationId))
            {
                tolkDbContext.Add(mockCustomerSettings);
            }
            else
            {
                tolkDbContext.CustomerSettings.Where(r => r.CustomerOrganisationId == mockCustomerSettings.CustomerOrganisationId && r.CustomerSettingType == mockCustomerSettings.CustomerSettingType).Single().Value = customerHasOrderAgreementSetting;
            }
            var requestOnePriceRows = GetRequestPriceRows(mockRequestAgreementVersionOne.RequestId);
            var requestTwoPriceRows = GetRequestPriceRows(mockRequestAgreementVersionTwo.RequestId);

            tolkDbContext.AddRange(requestOnePriceRows.Where(pr =>
                    !tolkDbContext.RequestPriceRows.Select(r => r.RequestId).Contains(pr.RequestId)));

            tolkDbContext.AddRange(requestTwoPriceRows.Where(pr =>
                !tolkDbContext.RequestPriceRows.Select(r => r.RequestId).Contains(pr.RequestId)));

            tolkDbContext.SaveChanges();

            return tolkDbContext;
        }

        private List<RequestPriceRow> GetRequestPriceRows(int requestId)
        {
            var priceRows = new List<RequestPriceRow>();
            foreach (var priceRow in MockEntities.MockRequestPriceRows)
            {
                priceRows.Add(priceRow);
            }
            priceRows.ForEach(pr => pr.RequestId = requestId);
            return priceRows;
        }

        private List<RequisitionPriceRow> GetRequisitionPriceRows(int requisitionId)
        {
            var priceRows = new List<RequisitionPriceRow>();
            foreach (var priceRow in MockEntities.MockRequisitionPriceRows)
            {
                priceRows.Add(priceRow);
            }
            priceRows.ForEach(pr => pr.RequisitionId = requisitionId);
            return priceRows;
        }

        private List<RequisitionPriceRow> GetRequisitionPriceRowsSamePriceAsRequest(int requisitionId)
        {
            var priceRows = new List<RequisitionPriceRow>();
            foreach (var priceRow in MockEntities.MockRequestPriceRows)
            {
                var requisitionPriceRow = new RequisitionPriceRow
                {
                    StartAt = priceRow.StartAt,
                    EndAt = priceRow.EndAt,
                    Price = priceRow.Price,
                    PriceRowType = priceRow.PriceRowType,
                    Quantity = priceRow.Quantity
                };
                priceRows.Add(requisitionPriceRow);
            }
            priceRows.ForEach(pr => pr.RequisitionId = requisitionId);
            return priceRows;
        }


        private async void AddRequisitionsToContext(TolkDbContext context, List<Request> mockRequests, bool useRequestPrices = false)
        {
            foreach (var request in mockRequests)
            {
                var previousMockRequisition = await context.Requisitions.Where(r => r.RequestId == request.RequestId && r.ReplacedByRequisitionId == null).SingleOrDefaultAsync();
                var mockRequisition = CreateMockRequisition(request);
                context.Add(mockRequisition);
                if (useRequestPrices)
                {
                    var requisitionPriceRows = GetRequisitionPriceRowsSamePriceAsRequest(mockRequisition.RequisitionId);
                    context.AddRange(requisitionPriceRows.Where(pr =>
                             !context.RequisitionPriceRows.Select(r => r.RequisitionId).Contains(pr.RequisitionId)));
                }
                else
                {
                    var requisitionPriceRows = GetRequisitionPriceRows(mockRequisition.RequisitionId);
                    context.AddRange(requisitionPriceRows.Where(pr =>
                             !context.RequisitionPriceRows.Select(r => r.RequisitionId).Contains(pr.RequisitionId)));
                }
                if (previousMockRequisition != null)
                {
                    previousMockRequisition.ReplacedByRequisitionId = mockRequisition.RequisitionId;
                }
            }
            context.SaveChanges();
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
                    StartAt = DateTime.Parse("2021-10-25 10:00:00").ToDateTimeOffsetSweden(),
                    EndAt = DateTime.Parse("2021-10-25 12:00:00").ToDateTimeOffsetSweden()
                },
                Ranking = new Ranking { RankingId = rankingId, Broker = new Broker { BrokerId = brokerId, Name = "MockBroker", OrganizationNumber = "123123-1234" }, Rank = 1, FrameworkAgreement = MockEntities.FrameworkAgreements.First(f => f.FrameworkAgreementResponseRuleset == ruleset) },
            };
        }

        private Requisition CreateMockRequisitionWithSpecificId(Request mockRequest, int requisitionId)
        {
            return new Requisition
            {
                Message = string.Empty,
                RequisitionId = requisitionId,
                Status = RequisitionStatus.Created,
                SessionStartedAt = new DateTime(2018, 12, 10, 10, 10, 10),
                SessionEndedAt = new DateTime(2018, 12, 10, 12, 10, 10),
                CreatedBy = 1,
                ProcessedBy = 1,
                Request = mockRequest
            };
        }

        private Requisition CreateMockRequisition(Request mockRequest)
        {
            return new Requisition
            {
                Message = string.Empty,
                Status = RequisitionStatus.Created,
                SessionStartedAt = new DateTime(2018, 12, 10, 10, 10, 10),
                SessionEndedAt = new DateTime(2018, 12, 10, 12, 10, 10),
                CreatedBy = 1,
                ProcessedBy = 1,
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
            var service = CreateStandardBusinessDocumentService(tolkDbContext);
            var payload = await service.CreateAndStoreStandardDocument(1);
            using var memoryStream = new MemoryStream(payload.Payload);
            memoryStream.Position = 0;
            StreamReader sr = new(memoryStream);
            var root = XElement.Load(sr);
            var elements = from el in root.Elements() where el.Name.LocalName == "OrderLine" select el;
            Assert.Equal(4, elements.Count());
            Assert.Equal(payload.IdentificationNumber, (from el in root.Elements() where el.Name.LocalName == "ID" select el).First().Value);
            Assert.Equal(Constants.OrderAgreementCustomizationId, (from el in root.Elements() where el.Name.LocalName == "CustomizationID" select el).First().Value);
        }

        [Fact]
        public async Task CreateValidOrderResponseDocument()
        {
            using var tolkDbContext = GetContextWithoutRequisition(nameof(CreateValidOrderResponseDocument));
            var service = CreateStandardBusinessDocumentService(tolkDbContext);
            var requests = tolkDbContext.Requests.Where(r => r.RequestId == 1).ToList();
            await service.CreateAndStoreStandardDocument(requests.First().RequestId);
            AddRequisitionsToContext(tolkDbContext, requests);
            var payload = await service.CreateAndStoreStandardDocument(requests.First().RequestId);

            using var memoryStream = new MemoryStream(payload.Payload);
            memoryStream.Position = 0;
            StreamReader sr = new StreamReader(memoryStream);
            var root = XElement.Load(sr);
            var elements = from el in root.Elements() where el.Name.LocalName == "OrderLine" select el;

            Assert.Equal(4, elements.Count());
            Assert.Equal(payload.IdentificationNumber, (from el in root.Elements() where el.Name.LocalName == "ID" select el).First().Value);
            Assert.Equal(Constants.OrderResponseCustomizationId, (from el in root.Elements() where el.Name.LocalName == "CustomizationID" select el).First().Value);
            Assert.Equal("Created from requisition", root.Elements().Where(e => e.Name.LocalName == "Note").Select(e => e.Value).Single());
        }

        [Fact]
        public async Task HandleOrderAgreementCreationNotOnSunday()
        {
            using var tolkDbContext = GetContext(DbNameWithData);
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-24 00:00:00 +02:00"));
            Assert.False(await service.HandleStandardDocumentCreation());
        }
        [Fact]
        public async Task HandleOrderAgreementCreation()
        {
            using var tolkDbContext = GetContext(DbNameWithData);
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-25 10:02:00 +02:00"));
            Assert.True(await service.HandleStandardDocumentCreation());
            Assert.Equal(2, tolkDbContext.PeppolPayloads.Count());
        }

        [Fact]
        public async Task HandleOrderAgreementCreation_CustomerDoesNotUseOrderAgreements()
        {
            using var tolkDbContext = GetContext(nameof(HandleOrderAgreementCreation_CustomerDoesNotUseOrderAgreements), false);
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-25 10:02:00 +02:00"));
            Assert.True(await service.HandleStandardDocumentCreation());
            Assert.Equal(0, tolkDbContext.PeppolPayloads.Count());
        }

        [Fact]
        public async Task HandleOrderAgreementCreation_CreateOrderResponseIfAgreementExists()
        {
            using var tolkDbContext = GetContextWithoutRequisition(nameof(HandleOrderAgreementCreation_CreateOrderResponseIfAgreementExists));
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-25 10:00:00 +01:00"));
            await service.HandleStandardDocumentCreation();
            Assert.Equal(2, tolkDbContext.PeppolPayloads.Count());

            var requests = tolkDbContext.Requests.Where(r => r.RequestId == 1).ToList();
            AddRequisitionsToContext(tolkDbContext, requests);
            Assert.True(await service.HandleStandardDocumentCreation());
            Assert.Equal(2, tolkDbContext.PeppolPayloads.Where(pp => pp.PeppolMessageType == PeppolMessageType.OrderAgreement).Count());
            Assert.Equal(1, tolkDbContext.PeppolPayloads.Where(pp => pp.PeppolMessageType == PeppolMessageType.OrderResponse).Count());
        }

        [Fact]
        public async Task HandleOrderAgreementCreation_ShouldNotCreateAgreementIfOrderHasNotStarted()
        {
            using var tolkDbContext = GetContext(nameof(HandleOrderAgreementCreation_ShouldNotCreateAgreementIfOrderHasNotStarted));
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-25 09:00:00 +02:00"));
            Assert.True(await service.HandleStandardDocumentCreation());
            Assert.Equal(0, tolkDbContext.PeppolPayloads.Count());
        }

        [Fact]
        public async Task HandleOrderAgreementCreation_ShouldNotCreateAgreementOrResponseIfAlreadyCreated()
        {
            using var tolkDbContext = GetContext(nameof(HandleOrderAgreementCreation_ShouldNotCreateAgreementOrResponseIfAlreadyCreated));
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-25 10:02:00 +02:00"));
            await service.HandleStandardDocumentCreation();
            Assert.True(await service.HandleStandardDocumentCreation());
            Assert.Equal(2, tolkDbContext.PeppolPayloads.Count());
        }

        [Fact]
        public async Task HandleOrderAgreement_ShouldNotCreateResponseIfPricesAreNotChanged()
        {
            using var tolkDbContext = GetContextWithoutRequisition(nameof(HandleOrderAgreement_ShouldNotCreateResponseIfPricesAreNotChanged));
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-25 10:00:00"));
            await service.HandleStandardDocumentCreation();
            Assert.Equal(2, tolkDbContext.PeppolPayloads.Count());

            var requests = tolkDbContext.Requests.Where(r => r.RequestId == 1).ToList();
            AddRequisitionsToContext(tolkDbContext, requests, useRequestPrices: true);
            Assert.True(await service.HandleStandardDocumentCreation());
            Assert.Equal(2, tolkDbContext.PeppolPayloads.Count());
        }

        [Theory]
        [InlineData(1, FrameworkAgreementResponseRuleset.VersionOne)]
        [InlineData(2, FrameworkAgreementResponseRuleset.VersionTwo)]
        public async Task ContractIDShouldMatchFrameworkAgreementNumber_WhenCreatedFromRequest(int requestId, FrameworkAgreementResponseRuleset ruleset)
        {
            using var tolkDbContext = GetContextWithoutRequisition(nameof(ContractIDShouldMatchFrameworkAgreementNumber_WhenCreatedFromRequest));
            var service = CreateStandardBusinessDocumentService(tolkDbContext);
            var payload = await service.CreateAndStoreStandardDocument(requestId);
            using var memoryStream = new MemoryStream(payload.Payload);
            memoryStream.Position = 0;
            StreamReader sr = new StreamReader(memoryStream);
            var root = XElement.Load(sr);
            Assert.Equal(MockEntities.FrameworkAgreements.First(f => f.FrameworkAgreementResponseRuleset == ruleset).AgreementNumber, root.Elements()
                .Where(e => e.Name.LocalName == "Contract")
                .Elements().Where(e => e.Name.LocalName == "ID")
                .Select(e => e.Value).Single());
            Assert.Equal("Created from request", root.Elements().Where(e => e.Name.LocalName == "Note").Select(e => e.Value).Single());
        }

        [Theory]
        [InlineData(1, FrameworkAgreementResponseRuleset.VersionOne)]
        [InlineData(2, FrameworkAgreementResponseRuleset.VersionTwo)]
        public async Task ContractIDShouldMatchFrameworkAgreementNumber_WhenCreatedFromRequisition(int requestId, FrameworkAgreementResponseRuleset ruleset)
        {
            using var tolkDbContext = GetContext(DbNameWithData);
            var requests = tolkDbContext.Requests.ToList();
            var service = CreateStandardBusinessDocumentService(tolkDbContext);
            var payload = await service.CreateAndStoreStandardDocument(requestId);
            using var memoryStream = new MemoryStream(payload.Payload);
            memoryStream.Position = 0;
            StreamReader sr = new StreamReader(memoryStream);
            var root = XElement.Load(sr);
            Assert.Equal(MockEntities.FrameworkAgreements.First(f => f.FrameworkAgreementResponseRuleset == ruleset).AgreementNumber, root.Elements()
                .Where(e => e.Name.LocalName == "Contract")
                .Elements().Where(e => e.Name.LocalName == "ID")
                .Select(e => e.Value).Single());
            Assert.Equal("Created from requisition", root.Elements().Where(e => e.Name.LocalName == "Note").Select(e => e.Value).Single());
        }

        // Test Replacing OR =>
        [Fact]
        public async Task HandleOrderAgreement_ShouldCreateAndReplaceResponseIfPricesAreUpdated()
        {
            using var tolkDbContext = GetContextWithoutRequisition(nameof(HandleOrderAgreement_ShouldCreateAndReplaceResponseIfPricesAreUpdated));
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-25 10:00:00"));
            await service.HandleStandardDocumentCreation();
            Assert.Equal(2, tolkDbContext.PeppolPayloads.Count());

            var requests = tolkDbContext.Requests.Where(r => r.RequestId == 1).ToList();
            AddRequisitionsToContext(tolkDbContext, requests);
            await service.HandleStandardDocumentCreation();
            Assert.Equal(3, tolkDbContext.PeppolPayloads.Count());

            AddRequisitionsToContext(tolkDbContext, requests, useRequestPrices: true);
            Assert.True(await service.HandleStandardDocumentCreation());
            Assert.Equal(4, tolkDbContext.PeppolPayloads.Count());
        }

        // Test That correct RequestIds are returned from StandardDocumentBusiness
        [Fact]
        public async Task Service_Should_Return_CorrectIds_NoDocumentCreatedForRequest()
        {
            using var tolkDbContext = GetContextWithoutRequisition(nameof(Service_Should_Return_CorrectIds_NoDocumentCreatedForRequest));
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-25 10:00:00"));
            var requestIds = await service.GetRequestIdsForDocumentCreation(1, new DateTime(1900, 1, 1));
            Assert.Equal(2, requestIds.Count());
            Assert.Contains(1, requestIds);
            Assert.Contains(2, requestIds);
        }

        // Test That correct RequestIds are returned from StandardDocumentBusiness
        [Fact]
        public async Task Service_Should_Return_CorrectIds_DocumentCreatedForRequest()
        {
            using var tolkDbContext = GetContextWithoutRequisition(nameof(Service_Should_Return_CorrectIds_DocumentCreatedForRequest));
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-25 10:00:00"));
            await service.HandleStandardDocumentCreation();
            var requestIds = await service.GetRequestIdsForDocumentCreation(1, new DateTime(1900, 1, 1));
            Assert.Empty(requestIds);
        }

        [Fact]
        public async Task Service_Should_Return_CorrectIds_DocumentCreatedForRequest_With_Requisitions_Added_After()
        {
            using var tolkDbContext = GetContextWithoutRequisition(nameof(Service_Should_Return_CorrectIds_DocumentCreatedForRequest_With_Requisitions_Added_After));
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-25 10:00:00"));
            await service.HandleStandardDocumentCreation(); 
            var requests = tolkDbContext.Requests.Where(r => r.RequestId == 1).ToList();
            AddRequisitionsToContext(tolkDbContext, requests);
            var requestIds = await service.GetRequestIdsForDocumentCreation(1, new DateTime(1900, 1, 1));
            Assert.Single(requestIds);
            Assert.Contains(1, requestIds);            
        }
        
    }
}
