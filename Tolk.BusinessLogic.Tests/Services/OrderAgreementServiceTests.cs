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
using Tolk.BusinessLogic.Utilities;
using Xunit;

namespace Tolk.BusinessLogic.Tests.Services
{
    public class OrderAgreementServiceTests
    {
        private readonly ILogger<StandardBusinessDocumentService> _logger;
        private readonly StubSwedishClock _clock;
        private const string OrderNumberVersionOne = "2018-123456";
        private const string OrderNumberVersionTwo = "2018-123457";

        private const int FirstCustomerOrganisationId = 1;
        private const int SecondCustomerOrganisationId = 2;
        private const int FirstBrokerId = 1;
        private const int SecondBrokerId = 2;
        private const int ThirdBrokerId = 3;
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
        
        private TolkDbContext GetBaseContext(bool withRequisitions, bool customerHasOrderAgreementSetting = true, int mockCustomerUserIndex = 0)
        {
            var tolkDbContext = CreateTolkDbContext();
            var mockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers);
            var customerOrganisationId = mockCustomerUsers[mockCustomerUserIndex].CustomerOrganisationId.Value;
            var mockCustomerSettings = new CustomerSetting
            {
                CustomerOrganisationId = customerOrganisationId,
                CustomerSettingType = CustomerSettingType.UseOrderAgreements,
                Value = customerHasOrderAgreementSetting
            };
            var firstMockRequest = CreateMockRequest(CreateMockOrder(mockCustomerUsers[mockCustomerUserIndex], orderId: 1, OrderNumberVersionOne), FrameworkAgreementResponseRuleset.VersionOne, OrderNumberVersionOne, requestId: 1, rankingId: 1, brokerId: 1);
            tolkDbContext.Add(firstMockRequest);
            tolkDbContext.AddRange(GetRequestPriceRows(firstMockRequest.RequestId));

            var secondMockRequest = CreateMockRequest(CreateMockOrder(mockCustomerUsers[mockCustomerUserIndex], orderId: 2, OrderNumberVersionTwo), FrameworkAgreementResponseRuleset.VersionTwo, OrderNumberVersionTwo, requestId: 2, rankingId: 2, brokerId: 2);
            tolkDbContext.Add(secondMockRequest);
            tolkDbContext.AddRange(GetRequestPriceRows(secondMockRequest.RequestId));
            tolkDbContext.Add(mockCustomerSettings);

            if(withRequisitions)
            {
                AddRequisitionsAndPriceRows(new List<Request> { firstMockRequest, secondMockRequest }, tolkDbContext);
            }

            if (customerHasOrderAgreementSetting)
            {
                var orderAgreementSettings = new List<CustomerOrderAgreementSettings>{
                    CreateMockCustomerOrderAgreementSettings(customerOrganisationId,FirstBrokerId, DateTime.Parse("2021-10-24 10:00:00").ToDateTimeOffsetSweden()),
                    CreateMockCustomerOrderAgreementSettings(customerOrganisationId,SecondBrokerId, DateTime.Parse("2021-10-22 10:00:00").ToDateTimeOffsetSweden())
                };
                tolkDbContext.AddRange(orderAgreementSettings);
            }
            tolkDbContext.AddRange(MockEntities.FrameworkAgreements);

            tolkDbContext.SaveChanges();
            return tolkDbContext;
        }

        private TolkDbContext GetContext(bool customerHasOrderAgreementSetting = true)
        {
            return GetBaseContext(withRequisitions:true,customerHasOrderAgreementSetting);         
        }
        
        private TolkDbContext GetContextWithoutRequisition( bool customerHasOrderAgreementSetting = true, int mockCustomerUserIndex = 0)
        {
            return GetBaseContext(withRequisitions: false, customerHasOrderAgreementSetting, mockCustomerUserIndex);
        }

        private void AddRequisitionsAndPriceRows(List<Request> requests,TolkDbContext tolkDbContext)
        {
            foreach (var request in requests)
            {
                var mockRequisition = CreateMockRequisitionWithSpecificId(request, request.RequestId);
                tolkDbContext.Add(mockRequisition);
                tolkDbContext.AddRange(GetRequisitionPriceRows(mockRequisition.RequisitionId));
            }
        }

        private TolkDbContext GetContextWithoutCustomerOrderAgreementSettings(bool customerHasOrderAgreementSetting = true)
        {
            var tolkDbContext = CreateTolkDbContext();            
            var mockCustomerSettings = new List<CustomerSetting>
            {
                new CustomerSetting{
                    CustomerOrganisationId = FirstCustomerOrganisationId,
                    CustomerSettingType = CustomerSettingType.UseOrderAgreements,
                    Value = customerHasOrderAgreementSetting
                },
                new CustomerSetting
                {
                    CustomerOrganisationId = SecondCustomerOrganisationId,
                    CustomerSettingType = CustomerSettingType.UseOrderAgreements,
                    Value = customerHasOrderAgreementSetting
                }
            };
            tolkDbContext.AddRange(MockEntities.FrameworkAgreements);
            tolkDbContext.AddRange(mockCustomerSettings);

            tolkDbContext.SaveChanges();

            return tolkDbContext;
        }

        private CustomerOrderAgreementSettings CreateMockCustomerOrderAgreementSettings(int customerOrganisationId, int brokerId, DateTimeOffset? enabledAt)
        {
            return new CustomerOrderAgreementSettings
            {
                CustomerOrganisationId = customerOrganisationId,
                BrokerId = brokerId,
                EnabledAt = enabledAt
            };
        }

        private void AddOrderAgreementSettingsIfNotExist(TolkDbContext tolkDbContext, List<CustomerOrderAgreementSettings> orderAgreementSettings)
        {
            tolkDbContext.AddRange(orderAgreementSettings);          
            tolkDbContext.SaveChanges();
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
                    context.AddRange(requisitionPriceRows);
                }
                else
                {
                    var requisitionPriceRows = GetRequisitionPriceRows(mockRequisition.RequisitionId);
                    context.AddRange(requisitionPriceRows);
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
                    CustomerOrganisationId = mockOrder.CustomerOrganisationId,
                    OrderNumber = orderNumber,
                    Status = OrderStatus.RequestRespondedAwaitingApproval,
                    StartAt = DateTime.Parse("2021-10-25 10:00:00").ToDateTimeOffsetSweden(),
                    EndAt = DateTime.Parse("2021-10-25 12:00:00").ToDateTimeOffsetSweden()
                },
                Ranking = new Ranking { RankingId = rankingId, Broker = new Broker { BrokerId = brokerId, Name = "MockBroker", OrganizationNumber = "123123-1234", PeppolId = "0007:1231231234" }, Rank = 1, FrameworkAgreementId = MockEntities.FrameworkAgreements.First(f => f.FrameworkAgreementResponseRuleset == ruleset).FrameworkAgreementId },
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

        private TolkDbContext CreateTolkDbContext()
        {
            var options = new DbContextOptionsBuilder<TolkDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new TolkDbContext(options);
        }

        [Fact]
        public async Task CreateValidOrderAgreementDocument()
        {
            using var tolkDbContext = GetContext();
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
            using var tolkDbContext = GetContextWithoutRequisition();
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
            using var tolkDbContext = GetContext();
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-24 00:00:00 +02:00"));
            Assert.False(await service.HandleStandardDocumentCreation());
        }
        [Fact]
        public async Task HandleOrderAgreementCreation()
        {
            using var tolkDbContext = GetContext();           

            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-25 10:02:00 +02:00"));
            Assert.True(await service.HandleStandardDocumentCreation());
            Assert.Equal(2, tolkDbContext.PeppolPayloads.Count());
        }

        [Fact]
        public async Task HandleOrderAgreementCreation_CustomerDoesNotUseOrderAgreements()
        {
            using var tolkDbContext = GetContext(false);
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-25 10:02:00 +02:00"));
            Assert.True(await service.HandleStandardDocumentCreation());
            Assert.Equal(0, tolkDbContext.PeppolPayloads.Count());
        }

        [Fact]
        public async Task HandleOrderAgreementCreation_CreateOrderResponseIfAgreementExists()
        {
            using var tolkDbContext = GetContextWithoutRequisition();
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
            using var tolkDbContext = GetContext();
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-25 09:00:00 +02:00"));
            Assert.True(await service.HandleStandardDocumentCreation());
            Assert.Equal(0, tolkDbContext.PeppolPayloads.Count());
        }

        [Fact]
        public async Task HandleOrderAgreementCreation_ShouldNotCreateAgreementOrResponseIfAlreadyCreated()
        {
            using var tolkDbContext = GetContext();
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-25 10:02:00 +02:00"));
            await service.HandleStandardDocumentCreation();
            Assert.True(await service.HandleStandardDocumentCreation());
            Assert.Equal(2, tolkDbContext.PeppolPayloads.Count());
        }

        [Fact]
        public async Task HandleOrderAgreement_ShouldNotCreateResponseIfPricesAreNotChanged()
        {
            using var tolkDbContext = GetContextWithoutRequisition();
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
            using var tolkDbContext = GetContextWithoutRequisition();
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
            using var tolkDbContext = GetContext();
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
            using var tolkDbContext = GetContextWithoutRequisition();
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
            using var tolkDbContext = GetContextWithoutRequisition();
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-25 10:00:00"));
            var customerOrderAgreementSettings = new List<CustomerOrderAgreementSettingsModel>()
            {
                new CustomerOrderAgreementSettingsModel {
                    BrokerId = 1,
                    CustomerOrganisationId = 1,
                    EnabledAt = new DateTimeOffset(new DateTime(1900, 1, 1))
                },
                new CustomerOrderAgreementSettingsModel {
                    BrokerId = 2,
                    CustomerOrganisationId = 1,
                    EnabledAt = new DateTimeOffset(new DateTime(1900, 1, 1))
                }
            };
            var requestIds = await service.GetRequestIdsForDocumentCreation(customerOrderAgreementSettings);
                //1, new DateTime(1900, 1, 1));
            Assert.Equal(2, requestIds.Count());
            Assert.Contains(1, requestIds);
            Assert.Contains(2, requestIds);
        }

        // Test That correct RequestIds are returned from StandardDocumentBusiness
        [Fact]
        public async Task Service_Should_Return_CorrectIds_DocumentCreatedForRequest()
        {
            using var tolkDbContext = GetContextWithoutRequisition();
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-25 10:00:00"));
            var customerOrderAgreementSettings = new List<CustomerOrderAgreementSettingsModel>()
            {
                new CustomerOrderAgreementSettingsModel {
                    BrokerId = 1,
                    CustomerOrganisationId = 1,
                    EnabledAt = new DateTimeOffset(new DateTime(1900, 1, 1))
                },
                new CustomerOrderAgreementSettingsModel {
                    BrokerId = 2,
                    CustomerOrganisationId = 1,
                    EnabledAt = new DateTimeOffset(new DateTime(1900, 1, 1))
                }
            };
            await service.HandleStandardDocumentCreation();
            var requestIds = await service.GetRequestIdsForDocumentCreation(customerOrderAgreementSettings);
            Assert.Empty(requestIds);
        }

        [Fact]
        public async Task Service_Should_Return_CorrectIds_DocumentCreatedForRequest_With_Requisitions_Added_After()
        {
            using var tolkDbContext = GetContextWithoutRequisition();
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-25 10:00:00"));
            var customerOrderAgreementSettings = new List<CustomerOrderAgreementSettingsModel>()
            {
                new CustomerOrderAgreementSettingsModel {
                    BrokerId = 1,
                    CustomerOrganisationId = 1,
                    EnabledAt = new DateTimeOffset(new DateTime(1900, 1, 1))
                },
                new CustomerOrderAgreementSettingsModel {
                    BrokerId = 2,
                    CustomerOrganisationId = 1,
                    EnabledAt = new DateTimeOffset(new DateTime(1900, 1, 1))
                }
            };
            await service.HandleStandardDocumentCreation();
            var requests = tolkDbContext.Requests.Where(r => r.RequestId == 1).ToList();
            AddRequisitionsToContext(tolkDbContext, requests);
            var requestIds = await service.GetRequestIdsForDocumentCreation(customerOrderAgreementSettings);
            Assert.Single(requestIds);
            Assert.Contains(1, requestIds);
        }

        [Fact]
        public async Task Should_Only_Create_OrderAgreement_For_Enabled_Brokers()
        {
            using var tolkDbContext = GetContextWithoutCustomerOrderAgreementSettings();
            // Create multiple Orders and requests with different brokers
            var mockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers);
            var requests = new List<Request>
            {
                 CreateMockRequest(CreateMockOrder(mockCustomerUsers[0], orderId: 1, OrderNumberVersionOne), FrameworkAgreementResponseRuleset.VersionTwo, OrderNumberVersionOne, requestId: 1, rankingId: 1, brokerId: 1),
                 CreateMockRequest(CreateMockOrder(mockCustomerUsers[0], orderId: 2, OrderNumberVersionTwo), FrameworkAgreementResponseRuleset.VersionTwo, OrderNumberVersionTwo, requestId: 2, rankingId: 2, brokerId: 2),
                 CreateMockRequest(CreateMockOrder(mockCustomerUsers[0], orderId: 3, OrderNumberVersionTwo), FrameworkAgreementResponseRuleset.VersionTwo, OrderNumberVersionTwo, requestId: 3, rankingId: 3, brokerId: 3),
                 CreateMockRequest(CreateMockOrder(mockCustomerUsers[0], orderId: 4, OrderNumberVersionTwo), FrameworkAgreementResponseRuleset.VersionTwo, OrderNumberVersionTwo, requestId: 4, rankingId: 4, brokerId: 4)
            };

            foreach (var request in requests)
            {
                if (!tolkDbContext.Requests.Any(r => r.RequestId == request.RequestId))
                {
                    tolkDbContext.Add(request);
                }
                var requestPriceRows = GetRequestPriceRows(request.RequestId);
                tolkDbContext.AddRange(requestPriceRows.Where(pr =>
                  !tolkDbContext.RequestPriceRows.Select(r => r.RequestId).Contains(pr.RequestId)));
            }
                 
            var orderAgreementSettings = new List<CustomerOrderAgreementSettings>{
                    CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:1, DateTime.Parse("2021-10-26 10:00:00").ToDateTimeOffsetSweden()),
                    CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:2, DateTime.Parse("2021-10-22 10:00:00").ToDateTimeOffsetSweden()),
                    CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:3, null),
                    CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:4, null)
                };

            AddOrderAgreementSettingsIfNotExist(tolkDbContext, orderAgreementSettings);
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-26 10:02:00 +02:00"));
            Assert.True(await service.HandleStandardDocumentCreation());
            Assert.Equal(SecondBrokerId, tolkDbContext.PeppolPayloads.Single().Request.Ranking.Broker.BrokerId);
            Assert.Single(tolkDbContext.PeppolPayloads);
        }

        [Fact]
        public async Task Should_Create_OrderAgreement_Per_Customer_And_Broker_SpecificSetting()
        {
            using var tolkDbContext = GetContextWithoutCustomerOrderAgreementSettings();
            // Create multiple Orders and requests with different brokers
            var mockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers);
            var requests = new List<Request>
            {
                 CreateMockRequest(CreateMockOrder(mockCustomerUsers[0], orderId: 1, OrderNumberVersionOne), FrameworkAgreementResponseRuleset.VersionTwo, OrderNumberVersionOne, requestId: 1, rankingId: 1, brokerId: 1),
                 CreateMockRequest(CreateMockOrder(mockCustomerUsers[0], orderId: 2, OrderNumberVersionTwo), FrameworkAgreementResponseRuleset.VersionTwo, OrderNumberVersionTwo, requestId: 2, rankingId: 2, brokerId: 2),
                 CreateMockRequest(CreateMockOrder(mockCustomerUsers[1], orderId: 3, OrderNumberVersionTwo), FrameworkAgreementResponseRuleset.VersionTwo, OrderNumberVersionTwo, requestId: 3, rankingId: 3, brokerId: 3),
                 CreateMockRequest(CreateMockOrder(mockCustomerUsers[1], orderId: 4, OrderNumberVersionTwo), FrameworkAgreementResponseRuleset.VersionTwo, OrderNumberVersionTwo, requestId: 4, rankingId: 4, brokerId: 4)
            };

            foreach (var request in requests)
            {
                if (!tolkDbContext.Requests.Any(r => r.RequestId == request.RequestId))
                {
                    tolkDbContext.Add(request);
                }
                var requestPriceRows = GetRequestPriceRows(request.RequestId);
                tolkDbContext.AddRange(requestPriceRows.Where(pr =>
                  !tolkDbContext.RequestPriceRows.Select(r => r.RequestId).Contains(pr.RequestId)));
            }

            var orderAgreementSettings = new List<CustomerOrderAgreementSettings>{
                    CreateMockCustomerOrderAgreementSettings(customerOrganisationId:mockCustomerUsers[0].CustomerOrganisationId.Value,brokerId:1, DateTime.Parse("2021-10-26 10:00:00").ToDateTimeOffsetSweden()),
                    CreateMockCustomerOrderAgreementSettings(customerOrganisationId:mockCustomerUsers[0].CustomerOrganisationId.Value,brokerId:2, DateTime.Parse("2021-10-22 10:00:00").ToDateTimeOffsetSweden()),
                    CreateMockCustomerOrderAgreementSettings(customerOrganisationId:mockCustomerUsers[1].CustomerOrganisationId.Value,brokerId:3,  DateTime.Parse("2021-09-02 10:00:00").ToDateTimeOffsetSweden()),
                    CreateMockCustomerOrderAgreementSettings(customerOrganisationId:mockCustomerUsers[1].CustomerOrganisationId.Value,brokerId:4,  DateTime.Parse("2021-10-23 10:00:00").ToDateTimeOffsetSweden())
                };

            AddOrderAgreementSettingsIfNotExist(tolkDbContext, orderAgreementSettings);
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-26 10:02:00 +02:00"));
            Assert.True(await service.HandleStandardDocumentCreation());         
            Assert.Equal(3,tolkDbContext.PeppolPayloads.Count());
        }

        [Fact]
        public async Task Service_Should_Not_Create_OrderResponse_If_Not_Enabled()
        {
            using var tolkDbContext = GetContextWithoutRequisition(mockCustomerUserIndex:1);
            var service = CreateStandardBusinessDocumentService(tolkDbContext, new StubSwedishClock("2021-10-25 10:00:00 +01:00"));
            await service.HandleStandardDocumentCreation();
            Assert.Equal(2, tolkDbContext.PeppolPayloads.Count());

            var requests = tolkDbContext.Requests.Where(r => r.RequestId == 1).ToList();
            AddRequisitionsToContext(tolkDbContext, requests);
            Assert.True(await service.HandleStandardDocumentCreation());
            Assert.Equal(2, tolkDbContext.PeppolPayloads.Where(pp => pp.PeppolMessageType == PeppolMessageType.OrderAgreement).Count());
            Assert.Empty(tolkDbContext.PeppolPayloads.Where(pp => pp.PeppolMessageType == PeppolMessageType.OrderResponse));
        }

    }
}
