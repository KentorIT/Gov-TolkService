using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Utilities;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Xunit;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Services;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Microsoft.Extensions.Options;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Enums;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Tolk.BusinessLogic.Tests.Services
{
    public class PriceUpdateTests
    {
        private const string DefaultPriceListRowEndDate = "9999-12-31";
        private const string DefaultPriceListRowStartDate = "2018-01-01";
        private AspNetUser mockCustomerUser;
        private Language[] mockLanguages;
        private Ranking[] mockRankings;
        private FrameworkAgreement[] frameWorkAgreements;
        private PriceListRow[] mockPriceRows;
        private PriceCalculationCharge[] mockPriceCalculationCharges;
        private BrokerFeeByServiceTypePriceListRow[] mockBrokerFeeByRegionAndServiceType;

        public PriceUpdateTests()
        {
            mockLanguages = MockEntities.MockLanguages;

            mockCustomerUser = new AspNetUser(1, "Arne@a.se", "Arne", "Arne", "Aronson",
                new CustomerOrganisation { CustomerOrganisationId = 1, Name = "Myndighet A", PeppolId = "ignore", OrganisationNumber = "734001-9810", UseOrderAgreementsFromDate = new DateTime(1990, 1, 1, 13, 0, 0), PriceListType = PriceListType.Court }
                );

            mockRankings = new[]
                {
                    new Ranking {
                        RankingId = 1,
                        Rank = 1,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2020, 05, 31),
                        BrokerFee = (decimal)0.1,
                        BrokerId = 1,
                        RegionId = 1,
                        FrameworkAgreementId = 1,
                        Quarantines = new List<Quarantine>()
                    },
                    new Ranking {
                        RankingId = 2,
                        Rank = 1,
                        FirstValidDate = new DateTime(2020, 06, 01),
                        LastValidDate = new DateTime(2099, 01, 01),
                        BrokerFee = (decimal)0.2,
                        BrokerId = 2,
                        RegionId = 2,
                        FrameworkAgreementId = 2,
                        Quarantines = new List<Quarantine>()
                    }
                };

            frameWorkAgreements = new[]
               {
                    new FrameworkAgreement
                    {
                        FrameworkAgreementId = 1,
                        AgreementNumber = "1234",
                        Description = "",
                        FirstValidDate = new DateTime(2016, 01, 01),
                        LastValidDate = new DateTime(2020, 05, 31),
                        BrokerFeeCalculationType = BrokerFeeCalculationType.ByRegionAndBroker,
                        FrameworkAgreementResponseRuleset = FrameworkAgreementResponseRuleset.VersionOne
                    },
                     new FrameworkAgreement
                    {
                        FrameworkAgreementId = 2,
                        AgreementNumber = "4321",
                        Description = "",
                        FirstValidDate = new DateTime(2020, 06, 01),
                        LastValidDate = new DateTime(2030, 05, 31),
                        BrokerFeeCalculationType = BrokerFeeCalculationType.ByRegionAndServiceType,
                        FrameworkAgreementResponseRuleset = FrameworkAgreementResponseRuleset.VersionTwo
                    }
                };
            mockPriceRows = new PriceListRow[]
            {
                    new PriceListRow {
                        PriceListRowId = 1,
                        Price = 100,
                        PriceListRowType = PriceListRowType.BasePrice,
                        PriceListType = PriceListType.Court,
                        CompetenceLevel = CompetenceLevel.OtherInterpreter,
                        StartDate = DateTime.Parse(DefaultPriceListRowStartDate),
                        EndDate = DateTime.Parse(DefaultPriceListRowEndDate),
                        MaxMinutes = 60
                    },
                     new PriceListRow {
                        PriceListRowId = 2,
                        Price = 200,
                        PriceListRowType = PriceListRowType.BasePrice,
                        PriceListType = PriceListType.Court,
                        CompetenceLevel = CompetenceLevel.OtherInterpreter,
                        StartDate = DateTime.Parse(DefaultPriceListRowStartDate),
                        EndDate = DateTime.Parse(DefaultPriceListRowEndDate),
                        MaxMinutes = 120
                    },
                    new PriceListRow {
                        PriceListRowId = 3,
                        Price = 300,
                        PriceListRowType = PriceListRowType.BasePrice,
                        PriceListType = PriceListType.Court,
                        CompetenceLevel = CompetenceLevel.OtherInterpreter,
                        StartDate = DateTime.Parse(DefaultPriceListRowStartDate),
                        EndDate = DateTime.Parse(DefaultPriceListRowEndDate),
                        MaxMinutes = 180
                    },

                    new PriceListRow {
                        PriceListRowId = 4,
                        Price = 200,
                        PriceListRowType = PriceListRowType.WeekendIWH,
                        PriceListType = PriceListType.Court,
                        CompetenceLevel = CompetenceLevel.OtherInterpreter,
                        StartDate = DateTime.Parse(DefaultPriceListRowStartDate),
                        EndDate = DateTime.Parse(DefaultPriceListRowEndDate),
                        MaxMinutes = 30
                    }            
            };

            mockBrokerFeeByRegionAndServiceType = new BrokerFeeByServiceTypePriceListRow[]
            {
                new BrokerFeeByServiceTypePriceListRow
                {
                    BrokerFeeByServiceTypePriceListRowId = 1,
                    CompetenceLevel = CompetenceLevel.OtherInterpreter,
                    Price = 50,
                    InterpreterLocation = InterpreterLocation.OnSite,
                    FirstValidDate = new DateTime(2020, 06, 01),
                    LastValidDate = new DateTime(2020, 07, 31),
                    RegionId = Region.Regions.Where(r => r.Name == "Stockholm").Single().RegionId
                },
                   new BrokerFeeByServiceTypePriceListRow
                {
                    BrokerFeeByServiceTypePriceListRowId = 2,
                    CompetenceLevel = CompetenceLevel.OtherInterpreter,
                    Price = 100,
                    InterpreterLocation = InterpreterLocation.OnSite,
                    FirstValidDate = new DateTime(2020, 08, 01),
                    LastValidDate = new DateTime(9999, 12, 31),
                    RegionId = Region.Regions.Where(r => r.Name == "Stockholm").Single().RegionId
                },
            };

            mockPriceCalculationCharges = new PriceCalculationCharge[]
            {
                    new PriceCalculationCharge() { PriceCalculationChargeId = 1, ChargePercentage = (decimal)31.42, ChargeTypeId = ChargeType.SocialInsuranceCharge, StartDate = new DateTime(2018, 01, 01), EndDate = new DateTime(2099, 01, 01) },
                    new PriceCalculationCharge() { PriceCalculationChargeId = 2, ChargePercentage = (decimal)0.7, ChargeTypeId = ChargeType.AdministrativeCharge, StartDate = new DateTime(2018, 01, 01), EndDate = new DateTime(2099, 01, 01) },
            };

        }

        private void AddOrderAndRequest(DateTimeOffset orderCreatedAt, DateTimeOffset requestStartAt, DateTimeOffset requestEndAt, int id, TolkDbContext tolkDbContext,Ranking mockRanking)
        {
            var mockOrder = new Order(mockCustomerUser, null, mockCustomerUser.CustomerOrganisation, orderCreatedAt)
            {
                OrderId = id,
                CreatedBy = mockCustomerUser.Id,
                ContactPersonId = mockCustomerUser.Id,
                CustomerOrganisationId = mockCustomerUser.CustomerOrganisation.CustomerOrganisationId,
                CustomerReferenceNumber = "Number1",
                OrderNumber = "2018-001337",
                StartAt = requestStartAt,
                EndAt = requestEndAt,
                RegionId = Region.Regions.Where(r => r.Name == "Stockholm").Single().RegionId,
                LanguageId = mockLanguages.Where(l => l.Name == "English").Single().LanguageId,
                Status = OrderStatus.RequestRespondedNewInterpreter,
                Requests = new List<Request>
                    {
                        new Request(mockRanking, new RequestExpiryResponse { ExpiryAt = new DateTimeOffset(2099,12,31,12,00,00, new TimeSpan(02,00,00)), RequestAnswerRuleType = RequestAnswerRuleType.AnswerRequiredNextDay },orderCreatedAt)
                        {
                            RequestId = id,
                            CompetenceLevel = 1,
                            InterpreterLocation = 1,
                            PriceRows = new List<RequestPriceRow> {
                                new RequestPriceRow()
                                {
                                   PriceListRowId = 3,
                                   Price = 300,
                                   StartAt = requestStartAt,
                                   EndAt = requestEndAt,
                                   PriceRowType = PriceRowType.InterpreterCompensation,
                                   Quantity = 1
                                }
                            }
                        },
                    },
            };

            tolkDbContext.Add(mockOrder);
        }

        private TolkDbContext CreateTolkDbContext(string databaseName)
        {
            var options = new DbContextOptionsBuilder<TolkDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new TolkDbContext(options);
        }

        private RequestService CreateRequestService(TolkDbContext dbContext, string now = "2018-12-12 00:00:00")
        {
            var clock = new StubSwedishClock(now);
            TolkBaseOptionsService optionService = new TolkBaseOptionsService(Options.Create(new TolkOptions() { RoundPriceDecimals = true }));
            var _cache = new CacheService(Mock.Of<IDistributedCache>(), dbContext, optionService, clock);
            var emailService = new EmailService(Mock.Of<ILogger<EmailService>>(), Options.Create(new TolkOptions()), clock);
            var notificationService = new StubNotificationService();
            return new RequestService(
                new PriceCalculationService(dbContext, _cache),
                new DateCalculationService(_cache),
                Mock.Of<ILogger<RequestService>>(),
                notificationService,
                null,
                dbContext,
                clock,
                null,
                emailService,
                optionService
            );
        }

        private void InitialDbSetup(TolkDbContext tolkDbContext)
        {
            tolkDbContext.AddRange(mockPriceRows);
            tolkDbContext.AddRange(mockPriceCalculationCharges);
            tolkDbContext.AddRange(frameWorkAgreements);
            tolkDbContext.AddRange(mockBrokerFeeByRegionAndServiceType);
            tolkDbContext.Add(mockCustomerUser.CustomerOrganisation);
        }

        private void AddNewPriceRows(DateTime startDate, string database,int priceIncrease)
        {            
            var endDate = DateTime.Parse(DefaultPriceListRowEndDate);
            //Update current rows
            using var context = CreateTolkDbContext(database);
            var currentPriceRows = context.PriceListRows.ToList();
            currentPriceRows.ForEach(cpr =>
            {
                cpr.EndDate = startDate.AddDays(-1);
            });
            context.SaveChanges();
            var maxId = context.PriceListRows.Max(prl => prl.PriceListRowId);

            var updatedPriceListRows = currentPriceRows.ToList();
            updatedPriceListRows.ForEach(p =>
            {
                p.PriceListRowId = p.PriceListRowId + maxId;
                p.Price = p.Price + priceIncrease;
                p.StartDate = startDate;
                p.EndDate = endDate;
            });

            context.AddRange(updatedPriceListRows.Where(newPrice =>
               !context.PriceListRows.Select(existPrice => existPrice.PriceListRowId).Contains(newPrice.PriceListRowId)));

            context.SaveChanges();
        }


        [Theory]
        [InlineData("2018-05-01 13:00:00 +02:00", "2018-06-10 13:00:00 +02:00", "2018-06-01",1,1)]
        [InlineData("2018-05-01 13:00:00 +02:00", "2018-05-30 13:00:00 +02:00", "2018-06-01",1,0)]
        [InlineData("2018-05-01 13:00:00 +02:00", "2018-05-10 13:00:00 +02:00", "2018-06-01",5,1)]
        [InlineData("2018-05-01 13:00:00 +02:00", "2018-05-10 13:00:00 +02:00", "2018-05-01",10,10)]
        public async Task Should_Return_Correct_Number_Of_Requests_To_Update(string orderCreatedAtString, string initialRequestStartAtString, string priceRowsStartDateString, int numberOfOrdersToCreate, int expectedRequestsToUpdate)
        {
            var dbGuid = Guid.NewGuid();
            var orderCreatedAt = DateTimeOffset.Parse(orderCreatedAtString);
            var initialRequestStart = DateTimeOffset.Parse(initialRequestStartAtString);
            var priceRowsStartDate = DateTime.Parse(priceRowsStartDateString);
            using (var tolkDbContext = CreateTolkDbContext(dbGuid.ToString()))
            {
                InitialDbSetup(tolkDbContext);
                for (int i = 0; i < numberOfOrdersToCreate; i++)
                {                   
                    AddOrderAndRequest(orderCreatedAt, initialRequestStart, initialRequestStart.AddHours(3), i + 1, tolkDbContext, mockRankings[0]);                    
                    initialRequestStart = initialRequestStart.AddDays(7);
                }
                tolkDbContext.SaveChanges();
            }

            using (var tolkDbContext = CreateTolkDbContext(dbGuid.ToString()))
            {
                AddNewPriceRows(priceRowsStartDate, dbGuid.ToString(),priceIncrease:100);
                var requests = await tolkDbContext.Requests.GetActiveRequestWithPriceRowsToUpdate();
                Assert.Equal(expectedRequestsToUpdate,requests.Count());
            }     
        }

        [Theory]
        [InlineData("2018-05-01 13:00:00 +02:00", "2018-05-30 13:00:00 +02:00", "2018-06-01", 1, 500, 0)]
        [InlineData("2018-05-01 13:00:00 +02:00", "2018-05-10 13:00:00 +02:00", "2018-06-01", 5, 100, 1)]
        [InlineData("2018-05-01 13:00:00 +02:00", "2018-05-10 13:00:00 +02:00", "2018-05-01", 10, 30, 10)]
        [InlineData("2018-05-01 13:00:00 +02:00", "2018-06-11 13:00:00 +02:00", "2018-05-01", 1, 30, 1)]
        public async Task Should_Create_New_Request_With_Updated_PriceRows_FrameworkAgreementVersionOne(string orderCreatedAtString, string initialRequestStartAtString, string priceRowsStartDateString, int numberOfOrdersToCreate,int priceIncrease, int expectedRequestsToUpdate)
        {
            var dbGuid = Guid.NewGuid();
            var orderCreatedAt = DateTimeOffset.Parse(orderCreatedAtString);
            var initialRequestStart = DateTimeOffset.Parse(initialRequestStartAtString);
            var priceRowsStartDate = DateTime.Parse(priceRowsStartDateString);
            var numberOfUpdatedRequests = numberOfOrdersToCreate - expectedRequestsToUpdate;
            var totalNumberOfRequests = numberOfOrdersToCreate + expectedRequestsToUpdate;            
            using (var tolkDbContext = CreateTolkDbContext(dbGuid.ToString()))
            {
                InitialDbSetup(tolkDbContext);
                for (int i = 0; i < numberOfOrdersToCreate; i++)
                {
                    AddOrderAndRequest(orderCreatedAt, initialRequestStart, initialRequestStart.AddHours(3), i + 1, tolkDbContext, mockRankings[0]);
                    initialRequestStart = initialRequestStart.AddDays(7);
                }
                tolkDbContext.SaveChanges();
            }

            using (var tolkDbContext = CreateTolkDbContext(dbGuid.ToString()))
            {             
                AddNewPriceRows(priceRowsStartDate, dbGuid.ToString(),priceIncrease);

                var sut = CreateRequestService(tolkDbContext);
                await sut.SyncRequestPrices();
                var requests = tolkDbContext.Requests.ToList();
                var nonUpdatedRequests = requests.Where(r => r.ReplacedByRequest == null && !r.ReplacingRequestId.HasValue).ToList();
                var replacedRequests = requests.Where(r => r.Status == RequestStatus.ReplacedAfterPriceUpdate).ToList();
                var newRequests = requests.Where(r => r.ReplacingRequestId.HasValue).ToList();

                Assert.Equal(totalNumberOfRequests, requests.Count());                
                Assert.Equal(expectedRequestsToUpdate, newRequests.Count());

                foreach (var request in replacedRequests)
                {
                    var replacingRequest = request.ReplacedByRequest;
                    var oldCompensationRow = request.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Single();
                    var newCompensationRow = replacingRequest.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Single();
                    var diff = newCompensationRow.Price - oldCompensationRow.Price;
                    Assert.Equal(priceIncrease, diff);                    
                }             
            }
        }

        [Theory]
        [InlineData("2020-06-01 13:00:00 +02:00", "2020-07-30 13:00:00 +02:00", "2020-08-01", 1, 500, 0)]
        [InlineData("2020-06-01 13:00:00 +02:00", "2020-07-06 13:00:00 +02:00", "2020-08-01", 5, 100, 1)]
        [InlineData("2020-06-01 13:00:00 +02:00", "2020-08-10 13:00:00 +02:00", "2020-08-01", 10, 30, 10)]
        [InlineData("2020-06-01 13:00:00 +02:00", "2020-08-11 13:00:00 +02:00", "2020-08-01", 1, 30, 1)]
        public async Task Should_Create_New_Request_With_Updated_PriceRows_FrameworkAgreementVersionTwo(string orderCreatedAtString, string initialRequestStartAtString, string priceRowsStartDateString, int numberOfOrdersToCreate, int priceIncrease, int expectedRequestsToUpdate)
        {
            var dbGuid = Guid.NewGuid();
            var orderCreatedAt = DateTimeOffset.Parse(orderCreatedAtString);
            var initialRequestStart = DateTimeOffset.Parse(initialRequestStartAtString);
            var priceRowsStartDate = DateTime.Parse(priceRowsStartDateString);
            var numberOfUpdatedRequests = numberOfOrdersToCreate - expectedRequestsToUpdate;
            var totalNumberOfRequests = numberOfOrdersToCreate + expectedRequestsToUpdate;
            var brokerFee = mockBrokerFeeByRegionAndServiceType[0].Price;
            using (var tolkDbContext = CreateTolkDbContext(dbGuid.ToString()))
            {
                InitialDbSetup(tolkDbContext);
                for (int i = 0; i < numberOfOrdersToCreate; i++)
                {
                    AddOrderAndRequest(orderCreatedAt, initialRequestStart, initialRequestStart.AddHours(3), i + 1, tolkDbContext, mockRankings[1]);
                    initialRequestStart = initialRequestStart.AddDays(7);
                }
                tolkDbContext.SaveChanges();
            }

            using (var tolkDbContext = CreateTolkDbContext(dbGuid.ToString()))
            {
                AddNewPriceRows(priceRowsStartDate, dbGuid.ToString(), priceIncrease);

                var sut = CreateRequestService(tolkDbContext);
                await sut.SyncRequestPrices();
                var requests = tolkDbContext.Requests.ToList();
                var nonUpdatedRequests = requests.Where(r => r.ReplacedByRequest == null && !r.ReplacingRequestId.HasValue).ToList();
                var replacedRequests = requests.Where(r => r.Status == RequestStatus.ReplacedAfterPriceUpdate).ToList();
                var newRequests = requests.Where(r => r.ReplacingRequestId.HasValue).ToList();

                Assert.Equal(totalNumberOfRequests, requests.Count());
                Assert.Equal(expectedRequestsToUpdate, newRequests.Count());                
                foreach (var request in replacedRequests)
                {
                    var replacingRequest = request.ReplacedByRequest;
                    var oldCompensationRow = request.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Single();
                    var newCompensationRow = replacingRequest.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.InterpreterCompensation).Single();
                    var brokerFeeRow = replacingRequest.PriceRows.Where(pr => pr.PriceRowType == PriceRowType.BrokerFee).Single().Price;
                    var diff = newCompensationRow.Price - oldCompensationRow.Price;
                    Assert.Equal(priceIncrease, diff);
                    Assert.Equal(brokerFee, brokerFeeRow);
                }
            }
        }
    }
}
