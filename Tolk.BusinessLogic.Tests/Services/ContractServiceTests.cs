using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Tolk.BusinessLogic.Utilities;
using Xunit;

namespace Tolk.BusinessLogic.Tests.Services
{
    public class ContractServiceTests
    {
        private readonly ILogger<ContractService> _logger;
        private readonly INotificationService _notificationService;
        private const string DbWithAgreementRankingsAndPriceLists = nameof(DbWithAgreementRankingsAndPriceLists);
        private const int AgreementIdForBrokerFeesByRegionBroker = 1;
        private const int AgreementIdBrokerFeeByRegionGroupAndServiceType = 2;
        public static FrameworkAgreement[] FrameworkAgreements => new[] {
            new FrameworkAgreement { FrameworkAgreementId = 1, AgreementNumber= "1234", Description = "", FirstValidDate = new DateTime(2016, 01, 01), LastValidDate = new DateTime(2030, 06, 01), BrokerFeeCalculationType = BrokerFeeCalculationType.ByRegionAndBroker, FrameworkAgreementResponseRuleset = FrameworkAgreementResponseRuleset.VersionOne },
            new FrameworkAgreement { FrameworkAgreementId = 2, AgreementNumber= "4321", Description = "", FirstValidDate = new DateTime(2030, 06, 02), LastValidDate = new DateTime(2040, 12, 31), BrokerFeeCalculationType = BrokerFeeCalculationType.ByRegionGroupAndServiceType, FrameworkAgreementResponseRuleset = FrameworkAgreementResponseRuleset.VersionTwo },
        };


        public ContractServiceTests()
        {
            using (var tolkDbContext = CreateTolkDbContext(DbWithAgreementRankingsAndPriceLists))
            {
                AddAgreementsToContext(tolkDbContext);                
                tolkDbContext.SaveChanges();
            }
            _logger = Mock.Of<ILogger<ContractService>>();
            _notificationService = new StubNotificationService();

        }      

        private TolkDbContext CreateTolkDbContext(string databaseName = "empty")
        {
            var options = new DbContextOptionsBuilder<TolkDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            return new TolkDbContext(options);
        }

        private List<BrokerFeeByRegionAndServiceType> GetOffSiteBrokerFeeByRegionAndServiceType(DateTime brokerFeeStartDate, DateTime brokerFeeEndDate, int regionId)
        {
            return new List<BrokerFeeByRegionAndServiceType> {
                new BrokerFeeByRegionAndServiceType { StartDate = brokerFeeStartDate, EndDate = brokerFeeEndDate, CompetenceLevel = CompetenceLevel.OtherInterpreter, InterpreterLocation = Enums.InterpreterLocation.OffSitePhone, RegionId = regionId, BrokerFee = 10 },
                new BrokerFeeByRegionAndServiceType { StartDate = brokerFeeStartDate, EndDate = brokerFeeEndDate, CompetenceLevel = CompetenceLevel.OtherInterpreter, InterpreterLocation = Enums.InterpreterLocation.OffSiteVideo, RegionId = regionId, BrokerFee = 10 },
                new BrokerFeeByRegionAndServiceType { StartDate = brokerFeeStartDate, EndDate = brokerFeeEndDate, CompetenceLevel = CompetenceLevel.EducatedInterpreter, InterpreterLocation = Enums.InterpreterLocation.OffSitePhone, RegionId = regionId, BrokerFee = 10 },
                new BrokerFeeByRegionAndServiceType { StartDate = brokerFeeStartDate, EndDate = brokerFeeEndDate, CompetenceLevel = CompetenceLevel.EducatedInterpreter, InterpreterLocation = Enums.InterpreterLocation.OffSiteVideo, RegionId = regionId, BrokerFee = 10 },
                new BrokerFeeByRegionAndServiceType { StartDate = brokerFeeStartDate, EndDate = brokerFeeEndDate, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, InterpreterLocation = Enums.InterpreterLocation.OffSitePhone, RegionId = regionId, BrokerFee = 10 },
                new BrokerFeeByRegionAndServiceType { StartDate = brokerFeeStartDate, EndDate = brokerFeeEndDate, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, InterpreterLocation = Enums.InterpreterLocation.OffSiteVideo, RegionId = regionId, BrokerFee = 10 },
                new BrokerFeeByRegionAndServiceType { StartDate = brokerFeeStartDate, EndDate = brokerFeeEndDate, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, InterpreterLocation = Enums.InterpreterLocation.OffSitePhone, RegionId = regionId, BrokerFee = 10 },
                new BrokerFeeByRegionAndServiceType { StartDate = brokerFeeStartDate, EndDate = brokerFeeEndDate, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, InterpreterLocation = Enums.InterpreterLocation.OffSiteVideo, RegionId = regionId, BrokerFee = 10 }
            };
        }
        private List<BrokerFeeByRegionAndServiceType> GetOnSiteBrokerFeeByRegionAndServiceType(DateTime brokerFeeStartDate, DateTime brokerFeeEndDate, int regionId)
        {
            return new List<BrokerFeeByRegionAndServiceType> {
                new BrokerFeeByRegionAndServiceType { StartDate = brokerFeeStartDate, EndDate = brokerFeeEndDate, CompetenceLevel = CompetenceLevel.OtherInterpreter, InterpreterLocation = Enums.InterpreterLocation.OnSite, RegionId = regionId, BrokerFee = 10 },
                new BrokerFeeByRegionAndServiceType { StartDate = brokerFeeStartDate, EndDate = brokerFeeEndDate, CompetenceLevel = CompetenceLevel.OtherInterpreter, InterpreterLocation = Enums.InterpreterLocation.OffSiteDesignatedLocation, RegionId = regionId, BrokerFee = 10 },
                new BrokerFeeByRegionAndServiceType { StartDate = brokerFeeStartDate, EndDate = brokerFeeEndDate, CompetenceLevel = CompetenceLevel.EducatedInterpreter, InterpreterLocation = Enums.InterpreterLocation.OnSite, RegionId = regionId, BrokerFee = 10 },
                new BrokerFeeByRegionAndServiceType { StartDate = brokerFeeStartDate, EndDate = brokerFeeEndDate, CompetenceLevel = CompetenceLevel.EducatedInterpreter, InterpreterLocation = Enums.InterpreterLocation.OffSiteDesignatedLocation, RegionId = regionId, BrokerFee = 10 },
                new BrokerFeeByRegionAndServiceType { StartDate = brokerFeeStartDate, EndDate = brokerFeeEndDate, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, InterpreterLocation = Enums.InterpreterLocation.OnSite, RegionId = regionId, BrokerFee = 10 },
                new BrokerFeeByRegionAndServiceType { StartDate = brokerFeeStartDate, EndDate = brokerFeeEndDate, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, InterpreterLocation = Enums.InterpreterLocation.OffSiteDesignatedLocation, RegionId = regionId, BrokerFee = 10 },
                new BrokerFeeByRegionAndServiceType { StartDate = brokerFeeStartDate, EndDate = brokerFeeEndDate, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, InterpreterLocation = Enums.InterpreterLocation.OnSite, RegionId = regionId, BrokerFee = 10 },
                new BrokerFeeByRegionAndServiceType { StartDate = brokerFeeStartDate, EndDate = brokerFeeEndDate, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, InterpreterLocation = Enums.InterpreterLocation.OffSiteDesignatedLocation, RegionId = regionId, BrokerFee = 10 }
            };
        }

        private List<BrokerFeeByRegionAndServiceType> GenerateBrokerFeesByRegionAndServiceType(int startYear, int noOfYears, int[] regionIds)
        {
            var brokerFees = new List<BrokerFeeByRegionAndServiceType>();
            for (int i = 0; i < noOfYears; i++)
            {
                for (int j = 0; j < regionIds.Length; j++)
                {
                    brokerFees.AddRange(GetOffSiteBrokerFeeByRegionAndServiceType(new DateTime(startYear, 01, 01), new DateTime(startYear, 12, 31), regionIds[j]));
                    brokerFees.AddRange(GetOnSiteBrokerFeeByRegionAndServiceType(new DateTime(startYear, 01, 01), new DateTime(startYear, 12, 31), regionIds[j]));
                }
                startYear++;
            }
            return brokerFees;
        }

        private List<PriceInformationBrokerFee> BrokerFeesByRegionAndBroker()
        {
            var priceList = new List<PriceInformationBrokerFee>();            
            var agreementActiveInYears = 15;
            var initialYear = 2016;
            var initialStartDate = DateTimeOffset.Parse("2016-01-01 00:00:00");
            var initialEndDate = DateTimeOffset.Parse("2016-12-31 00:00:00");
            // Add pricelistrows each year from 2016-01-01 to 2029-12-31
            for (int i = 0; i < agreementActiveInYears; i++)
            {
                priceList.Add(new PriceInformationBrokerFee
                {                    
                    StartDatePriceList = initialStartDate.AddYears(i),
                    EndDatePriceList = initialEndDate.AddYears(i),
                    FirstValidDateRanking = new DateTime(initialYear, 01, 01),
                    LastValidDateRanking = new DateTime(initialYear + 1, 12, 31),
                    BasePrice = 100,
                    RoundDecimals = true,
                    BrokerFee = 0.10M,
                    RankingId = 1
                });
                priceList.Add(new PriceInformationBrokerFee
                {                    
                    StartDatePriceList = initialStartDate.AddYears(i),
                    EndDatePriceList = initialEndDate.AddYears(i),
                    FirstValidDateRanking = new DateTime(initialYear, 01, 01),
                    LastValidDateRanking = new DateTime(initialYear + 1, 12, 31),
                    BasePrice = 100,
                    RoundDecimals = true,
                    BrokerFee = 0.10M,
                    RankingId = 2
                });
                initialYear++;
            }
            return priceList;
        }

        private void AddAgreementsToContext(TolkDbContext tolkDbContext)
        {
            tolkDbContext.AddRange(FrameworkAgreements.Where(newAgreement =>
                !tolkDbContext.FrameworkAgreements.Select(existingAgreement => existingAgreement.FrameworkAgreementId).Contains(newAgreement.FrameworkAgreementId)));            
        }   

        [Theory]
        [InlineData("2016-01-01 00:00:00 +01:00", AgreementIdForBrokerFeesByRegionBroker, true)]        
        [InlineData("2015-12-31 00:00:00 +01:00", AgreementIdForBrokerFeesByRegionBroker, false)]
        [InlineData("2015-12-31 23:59:00 +01:00", AgreementIdForBrokerFeesByRegionBroker, false)]
        [InlineData("2030-06-02 00:00:00 +02:00", AgreementIdForBrokerFeesByRegionBroker, false)]
        [InlineData("2030-06-01 00:00:00 +02:00", AgreementIdForBrokerFeesByRegionBroker, true)]
        [InlineData("2030-06-01 23:59:00 +02:00", AgreementIdForBrokerFeesByRegionBroker, true)]

        [InlineData("2030-06-02 00:00:00 +02:00", AgreementIdBrokerFeeByRegionGroupAndServiceType, true)]       
        [InlineData("2030-06-01 00:00:00 +02:00", AgreementIdBrokerFeeByRegionGroupAndServiceType, false)]
        [InlineData("2030-06-01 23:59:00 +02:00", AgreementIdBrokerFeeByRegionGroupAndServiceType, false)]
        [InlineData("2041-01-01 00:00:00 +01:00", AgreementIdBrokerFeeByRegionGroupAndServiceType, false)]
        [InlineData("2040-12-31 00:00:00 +01:00", AgreementIdBrokerFeeByRegionGroupAndServiceType, true)]
        [InlineData("2040-12-31 23:59:00 +01:00", AgreementIdBrokerFeeByRegionGroupAndServiceType, true)]
        public void Should_Get_Agreement_And_CorrectStatus(string now, int frameworkAgreementId,bool isActive)
        {            
            var clock = new StubSwedishClock(now);
            var context = CreateTolkDbContext(DbWithAgreementRankingsAndPriceLists);
            var sut = new ContractService(context, clock);            

            var result = sut.GetFrameworkAgreementById(frameworkAgreementId).Result;

            result.IsActive.Should().Be(isActive);            
        }        

        [Theory]
        [InlineData("2015-12-31 00:00:00 +01:00", 0, null, null)]
        [InlineData("2015-12-31 23:59:00 +01:00", 0, null, null)]
        [InlineData("2016-01-01 00:00:00 +01:00", 2, "2016-01-01", "2016-12-31")]
        [InlineData("2016-06-02 00:00:00 +02:00", 2, "2016-01-01", "2016-12-31")]        
        [InlineData("2016-12-31 23:59:00 +01:00", 2, "2016-01-01", "2016-12-31")]
        [InlineData("2017-01-01 00:00:00 +01:00", 2, "2017-01-01", "2017-12-31")]        
        [InlineData("2025-01-01 00:00:00 +01:00", 2, "2025-01-01", "2025-12-31")]
        [InlineData("2025-12-31 23:59:00 +01:00", 2, "2025-01-01", "2025-12-31")]
        [InlineData("2029-01-01 00:00:00 +01:00", 2, "2029-01-01", "2029-12-31")]
        [InlineData("2029-12-31 23:59:00 +01:00", 2, "2029-01-01", "2029-12-31")]
        [InlineData("2030-06-02 00:00:00 +02:00", 2, "2030-01-01", "2030-12-31")]
        [InlineData("2030-12-31 23:59:00 +01:00", 2, "2030-01-01", "2030-12-31")]
        [InlineData("2035-01-01 00:00:00 +01:00", 2, "2030-01-01", "2030-12-31")]
        [InlineData("2035-07-20 00:00:00 +02:00", 2, "2030-01-01", "2030-12-31")]
        public void Should_Return_Current_Or_LastActive_BrokerFeesByRegionBroker_For_Agreement(string now, int activeBrokerFees, string brokerFeeStartDate, string brokerFeeEndDate)
        {
            var clock = new StubSwedishClock(now);
            var context = CreateTolkDbContext(DbWithAgreementRankingsAndPriceLists);
            var sut = new ContractService(context, clock);
            var agreement = sut.GetFrameworkAgreementById(AgreementIdForBrokerFeesByRegionBroker).Result;
            var priceList = BrokerFeesByRegionAndBroker();

            var filteredList = priceList.CurrentOrLastActiveBrokerFeesForAgreement(agreement, clock.SwedenNow.Date).ToList();
            
            filteredList.Count.Should().Be(activeBrokerFees);            
            if(activeBrokerFees != 0)
            {
                filteredList.First().StartDate.Should().Be(DateTimeOffset.Parse(brokerFeeStartDate));
                filteredList.First().EndDate.Should().Be(DateTimeOffset.Parse(brokerFeeEndDate));
            }
        }


        // Add more specific pricelistrows with different Dates and control the different dates for them

        [Theory]
        [InlineData("2030-06-01 00:00:00 +02:00", 0, null, null)]
        [InlineData("2030-06-01 23:59:00 +02:00", 0, null, null)]
        [InlineData("2030-06-02 00:00:00 +02:00", 16, "2030-01-01", "2030-12-31")]
        [InlineData("2031-06-12 00:00:00 +02:00", 16, "2031-01-01", "2031-12-31")]
        [InlineData("2040-07-20 00:00:00 +02:00", 16, "2040-01-01", "2040-12-31")]
        [InlineData("2100-02-20 00:00:00 +01:00", 16, "2040-01-01", "2040-12-31")]        
        public void Should_Return_Current_Or_LastActive_OffSite_BrokerFeeByRegionGroupAndServiceTypePriceList_For_Agreement(string now, int activeBrokerFees, string brokerFeeStartDate, string brokerFeeEndDate)
        {
            var clock = new StubSwedishClock(now);
            var context = CreateTolkDbContext(DbWithAgreementRankingsAndPriceLists);
            var sut = new ContractService(context, clock);
            var agreement = sut.GetFrameworkAgreementById(AgreementIdBrokerFeeByRegionGroupAndServiceType).Result;
            var brokerFees = GenerateBrokerFeesByRegionAndServiceType(2030, 11, new int[] { 1, 2 });
            var currentOrLastActive = brokerFees.CurrentOrLastActiveDistanceBrokerFeesForAgreement(agreement, clock.SwedenNow.Date);
            currentOrLastActive.Count().Should().Be(activeBrokerFees);
            if(activeBrokerFees != 0)
            {
                currentOrLastActive.First().StartDate.Should().Be(DateTimeOffset.Parse(brokerFeeStartDate));                
                currentOrLastActive.First().InterpreterLocation.Should().NotBe(InterpreterLocation.OffSiteDesignatedLocation);                
                currentOrLastActive.First().InterpreterLocation.Should().NotBe(InterpreterLocation.OnSite);                
                currentOrLastActive.First().EndDate.Should().Be(DateTimeOffset.Parse(brokerFeeEndDate));
            }
        }

        [Theory]
        [InlineData("2030-06-01 00:00:00 +02:00", 0, null, null)]
        [InlineData("2030-06-01 23:59:00 +02:00", 0, null, null)]
        [InlineData("2030-06-02 00:00:00 +02:00", 16, "2030-01-01", "2030-12-31")]
        [InlineData("2031-06-12 00:00:00 +02:00", 16, "2031-01-01", "2031-12-31")]
        [InlineData("2040-07-20 00:00:00 +02:00", 16, "2040-01-01", "2040-12-31")]
        [InlineData("2100-02-20 00:00:00 +01:00", 16, "2040-01-01", "2040-12-31")]
        public void Should_Return_Current_Or_LastActive_OnSite_BrokerFeeByRegionGroupAndServiceTypePriceList_For_Agreement(string now, int activeBrokerFees, string brokerFeeStartDate, string brokerFeeEndDate)
        {
            var clock = new StubSwedishClock(now);
            var context = CreateTolkDbContext(DbWithAgreementRankingsAndPriceLists);
            var sut = new ContractService(context, clock);
            var agreement = sut.GetFrameworkAgreementById(AgreementIdBrokerFeeByRegionGroupAndServiceType).Result;
            var brokerFees = GenerateBrokerFeesByRegionAndServiceType(2030, 11, new int[] { 1, 2 });
            var currentOrLastActive = brokerFees.CurrentOrLastActiveOnSiteBrokerFeesForAgreement(agreement, clock.SwedenNow.Date);
            currentOrLastActive.Count().Should().Be(activeBrokerFees);
            if (activeBrokerFees != 0)
            {
                currentOrLastActive.First().StartDate.Should().Be(DateTimeOffset.Parse(brokerFeeStartDate));
                currentOrLastActive.First().InterpreterLocation.Should().NotBe(InterpreterLocation.OffSitePhone);
                currentOrLastActive.First().InterpreterLocation.Should().NotBe(InterpreterLocation.OffSiteVideo);
                currentOrLastActive.First().EndDate.Should().Be(DateTimeOffset.Parse(brokerFeeEndDate));
            }
        }
    }
}
