﻿using DocumentFormat.OpenXml.Drawing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
    public class OrderServiceTests
    {
        private readonly ILogger<OrderService> _logger;
        private CacheService _cache;
        public OrderServiceTests()
        {
            _logger = Mock.Of<ILogger<OrderService>>();
        }
        private OrderService CreateOrderService(TolkDbContext dbContext, string now = "2018-12-12 00:00:00")
        {
            // OrderService(
            //TolkDbContext tolkDbContext,
            //ISwedishClock clock,
            //RankingService rankingService,
            //DateCalculationService dateCalculationService,
            //PriceCalculationService priceCalculationService,
            //ILogger < OrderService > logger,
            //INotificationService notificationService,
            //VerificationService verificationService,
            // EmailService emailService,
            //ITolkBaseOptions tolkBaseOptions,
            //CacheService cacheService
            //)
            var clock = new StubSwedishClock(now);
            IDistributedCache cache = Mock.Of<IDistributedCache>();
            TolkBaseOptionsService optionService = new TolkBaseOptionsService(Options.Create(new TolkOptions() { RoundPriceDecimals = true }));
            _cache = new CacheService(cache, dbContext, optionService, clock);
            var emailService = new EmailService(Mock.Of<ILogger<EmailService>>(), Options.Create(new TolkOptions()), clock);
            var notificationService = new StubNotificationService();
            return new OrderService(
                dbContext,
                clock,
                new RankingService(dbContext),
                new DateCalculationService(_cache),
                new PriceCalculationService(dbContext, _cache),
                _logger,
                notificationService,
                new VerificationService(dbContext, clock, optionService, Mock.Of<ILogger<VerificationService>>(), notificationService, emailService),
                emailService,
                optionService,
                _cache);
        }

        private TolkDbContext CreateTolkDbContext(string databaseName = "empty")
        {
            var options = new DbContextOptionsBuilder<TolkDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new TolkDbContext(options);
        }

        [Theory]
        //lördag till måndag => panik
        [InlineData("2022-11-05 19:05:00", "2022-11-07 11:25:00", FrameworkAgreementResponseRuleset.VersionOne, null, null, RequestAnswerRuleType.ResponseSetByCustomer)]
        [InlineData("2022-11-05 19:05:00", "2022-11-07 11:25:00", FrameworkAgreementResponseRuleset.VersionTwo, null, null, RequestAnswerRuleType.ResponseSetByCustomer)]
        //lördag till tisdag => 16:30
        [InlineData("2022-11-05 19:05:00", "2022-11-08 11:25:00", FrameworkAgreementResponseRuleset.VersionOne, "2022-11-07 16:30:00", null, RequestAnswerRuleType.RequestCreatedOneDayBefore)]
        [InlineData("2022-11-05 19:05:00", "2022-11-08 11:25:00", FrameworkAgreementResponseRuleset.VersionTwo, "2022-11-07 16:30:00", null, RequestAnswerRuleType.RequestCreatedOneDayBefore)]
        //lördag till onsdag => 15:00
        [InlineData("2022-11-05 19:05:00", "2022-11-09 11:25:00", FrameworkAgreementResponseRuleset.VersionOne, "2022-11-08 15:00:00", null, RequestAnswerRuleType.AnswerRequiredNextDay)]
        [InlineData("2022-11-05 19:05:00", "2022-11-09 11:25:00", FrameworkAgreementResponseRuleset.VersionTwo, "2022-11-08 15:00:00", null, RequestAnswerRuleType.AnswerRequiredNextDay)]
        //söndag till måndag => panik
        [InlineData("2022-11-06 19:05:00", "2022-11-07 11:25:00", FrameworkAgreementResponseRuleset.VersionOne, null, null, RequestAnswerRuleType.ResponseSetByCustomer)]
        [InlineData("2022-11-06 19:05:00", "2022-11-07 11:25:00", FrameworkAgreementResponseRuleset.VersionTwo, null, null, RequestAnswerRuleType.ResponseSetByCustomer)]
        //söndag till tisdag => 16:30
        [InlineData("2022-11-06 19:05:00", "2022-11-08 11:25:00", FrameworkAgreementResponseRuleset.VersionOne, "2022-11-07 16:30:00", null, RequestAnswerRuleType.RequestCreatedOneDayBefore)]
        [InlineData("2022-11-06 19:05:00", "2022-11-08 11:25:00", FrameworkAgreementResponseRuleset.VersionTwo, "2022-11-07 16:30:00", null, RequestAnswerRuleType.RequestCreatedOneDayBefore)]
        //söndag till onsdag => 15:00
        [InlineData("2022-11-06 19:05:00", "2022-11-09 11:25:00", FrameworkAgreementResponseRuleset.VersionOne, "2022-11-08 15:00:00", null, RequestAnswerRuleType.AnswerRequiredNextDay)]
        [InlineData("2022-11-06 19:05:00", "2022-11-09 11:25:00", FrameworkAgreementResponseRuleset.VersionTwo, "2022-11-08 15:00:00", null, RequestAnswerRuleType.AnswerRequiredNextDay)]
        //måndag 18:00 till måndag 18:01 två veckor senare => >10 dagar v2
        [InlineData("2022-11-07 18:00:00", "2022-11-21 18:01:00", FrameworkAgreementResponseRuleset.VersionOne, "2022-11-08 15:00:00", null, RequestAnswerRuleType.AnswerRequiredNextDay)]
        [InlineData("2022-11-07 18:00:00", "2022-11-21 18:01:00", FrameworkAgreementResponseRuleset.VersionTwo, "2022-11-14 18:01:00", "2022-11-09 18:00:00", RequestAnswerRuleType.RequestCreatedMoreThanTenDaysBefore)]
        //måndag 18:00 till måndag 17:59 två veckor senare => <10 dagar v2 + v1
        [InlineData("2022-11-07 18:00:00", "2022-11-21 17:59:00", FrameworkAgreementResponseRuleset.VersionOne, "2022-11-08 15:00:00", null, RequestAnswerRuleType.AnswerRequiredNextDay)]
        [InlineData("2022-11-07 18:00:00", "2022-11-21 17:59:00", FrameworkAgreementResponseRuleset.VersionTwo, "2022-11-08 15:00:00", null, RequestAnswerRuleType.AnswerRequiredNextDay)]
        //söndag 17:30 till måndag 10:15 två veckor senare => >10 dagar v2 bekräfta onsdag 00:00, namnge typ måndagen mellan 10:15
        [InlineData("2022-11-06 17:30:00", "2022-11-21 10:15:00", FrameworkAgreementResponseRuleset.VersionOne, "2022-11-08 15:00:00", null, RequestAnswerRuleType.AnswerRequiredNextDay)]
        [InlineData("2022-11-06 17:30:00", "2022-11-21 10:15:00", FrameworkAgreementResponseRuleset.VersionTwo, "2022-11-14 10:15:00", "2022-11-09 00:00:00", RequestAnswerRuleType.RequestCreatedMoreThanTenDaysBefore)]
        //måndag 18:00 till måndag 18:01 fyra veckor senare => >20 dagar v2
        [InlineData("2022-11-07 18:00:00", "2022-12-05 18:01:00", FrameworkAgreementResponseRuleset.VersionOne, "2022-11-08 15:00:00", null, RequestAnswerRuleType.AnswerRequiredNextDay)]
        [InlineData("2022-11-07 18:00:00", "2022-12-05 18:01:00", FrameworkAgreementResponseRuleset.VersionTwo, "2022-11-24 18:01:00", "2022-11-11 18:00:00", RequestAnswerRuleType.RequestCreatedMoreThanTwentyDaysBefore)]
        //måndag 18:00 till måndag 17:59 två veckor senare => <10 dagar v2 + v1
        [InlineData("2022-11-07 18:00:00", "2022-12-05 17:59:00", FrameworkAgreementResponseRuleset.VersionOne, "2022-11-08 15:00:00", null, RequestAnswerRuleType.AnswerRequiredNextDay)]
        [InlineData("2022-11-07 18:00:00", "2022-12-05 17:59:00", FrameworkAgreementResponseRuleset.VersionTwo, "2022-11-28 17:59:00", "2022-11-09 18:00:00", RequestAnswerRuleType.RequestCreatedMoreThanTenDaysBefore)]
        //söndag 17:30 till måndag 10:15 två veckor senare => >10 dagar v2 bekräfta onsdag 00:00, namnge typ måndagen mellan 10:15
        [InlineData("2022-11-06 17:30:00", "2022-12-05 10:15:00", FrameworkAgreementResponseRuleset.VersionOne, "2022-11-08 15:00:00", null, RequestAnswerRuleType.AnswerRequiredNextDay)]
        [InlineData("2022-11-06 17:30:00", "2022-12-05 10:15:00", FrameworkAgreementResponseRuleset.VersionTwo, "2022-11-24 10:15:00", "2022-11-11 00:00:00", RequestAnswerRuleType.RequestCreatedMoreThanTwentyDaysBefore)]

        [InlineData("2022-10-04 10:13:12", "2022-12-08 12:30:00", FrameworkAgreementResponseRuleset.VersionOne, "2022-10-05 15:00:00", null, RequestAnswerRuleType.AnswerRequiredNextDay)]
        [InlineData("2022-11-08 10:13:12", "2022-11-24 12:30:00", FrameworkAgreementResponseRuleset.VersionOne, "2022-11-09 15:00:00", null, RequestAnswerRuleType.AnswerRequiredNextDay)]
        [InlineData("2022-11-08 10:13:12", "2022-11-15 12:30:00", FrameworkAgreementResponseRuleset.VersionOne, "2022-11-09 15:00:00", null, RequestAnswerRuleType.AnswerRequiredNextDay)]
        [InlineData("2022-11-08 10:13:12", "2022-11-09 12:30:00", FrameworkAgreementResponseRuleset.VersionOne, "2022-11-08 16:30:00", null, RequestAnswerRuleType.RequestCreatedOneDayBefore)]
        [InlineData("2022-11-08 14:00:01", "2022-11-09 12:30:00", FrameworkAgreementResponseRuleset.VersionOne, null, null, RequestAnswerRuleType.ResponseSetByCustomer)]
        [InlineData("2022-10-04 10:13:12", "2022-12-08 12:30:45", FrameworkAgreementResponseRuleset.VersionTwo, "2022-11-29 12:30:00", "2022-10-10 10:13:00", RequestAnswerRuleType.RequestCreatedMoreThanTwentyDaysBefore)]
        [InlineData("2022-11-08 10:13:12", "2022-11-24 12:30:00", FrameworkAgreementResponseRuleset.VersionTwo, "2022-11-17 12:30:00", "2022-11-10 10:13:00", RequestAnswerRuleType.RequestCreatedMoreThanTenDaysBefore)]
        [InlineData("2022-11-08 10:13:12", "2022-11-15 12:30:00", FrameworkAgreementResponseRuleset.VersionTwo, "2022-11-09 15:00:00", null, RequestAnswerRuleType.AnswerRequiredNextDay)]
        [InlineData("2022-11-08 10:13:12", "2022-11-09 12:30:00", FrameworkAgreementResponseRuleset.VersionTwo, "2022-11-08 16:30:00", null, RequestAnswerRuleType.RequestCreatedOneDayBefore)]
        [InlineData("2022-11-08 14:00:01", "2022-11-09 12:30:00", FrameworkAgreementResponseRuleset.VersionTwo, null, null, RequestAnswerRuleType.ResponseSetByCustomer)]
        public void CalculateExpiryForNewRequestTest(string now, string startTime, FrameworkAgreementResponseRuleset ruleset, string expectedExpiry, string expectedLastAcceptBy, RequestAnswerRuleType answerRuleType)
        {
            DateTimeOffset start = DateTimeOffset.Parse(startTime);
            DateTimeOffset? expectedExpiryTime = !string.IsNullOrEmpty(expectedExpiry) ? DateTimeOffset.Parse(expectedExpiry) : null;
            DateTimeOffset? expectedLastAcceptByTime = !string.IsNullOrEmpty(expectedLastAcceptBy) ? DateTimeOffset.Parse(expectedLastAcceptBy) : null;

            using var tolkDbContext = CreateTolkDbContext();
            var service = CreateOrderService(tolkDbContext, now);
            var result = service.CalculateExpiryForNewRequest(start, ruleset);
            result.RequestAnswerRuleType.Should().Be(answerRuleType);
            result.ExpiryAt.Should().Be(expectedExpiryTime);
            if (expectedExpiryTime.HasValue)
            {
                result.ExpiryAt.Should().Be(expectedExpiryTime);
            }
            else
            {
                result.ExpiryAt.Should().BeNull();
            }
            if (expectedLastAcceptByTime.HasValue)
            {
                result.LastAcceptedAt.Should().Be(expectedLastAcceptByTime);
            }
            else
            {
                result.LastAcceptedAt.Should().BeNull();
            }
        }
    }
}
