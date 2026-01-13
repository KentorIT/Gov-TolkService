using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Models.TwoFactor;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Tolk.BusinessLogic.Utilities;
using Xunit;

namespace Tolk.BusinessLogic.Tests.Services
{
    public class TwoFactorVerificationTests
    {
        private readonly ILogger<UserService> _logger;
        private readonly INotificationService _notificationService;
        private readonly IOptions<TolkOptions> _options;
        public TwoFactorVerificationTests()
        {
            _logger = Mock.Of<ILogger<UserService>>();
            _notificationService = Mock.Of<INotificationService>();

            _options = Options.Create(
              new TolkOptions()
              {
                  TwoFactor = new TwoFactorSettings
                  {
                      Enabled = true,
                      Salt = "qweQWE-123",
                      TwoFactorDaysValidity = 7,
                      ValidationCodeMinutesValidity = 30,
                      CodeLength = 2,
                      MaxNumberOfTries = 1,
                  }
              });
        }
        private UserService CreateUserService(TolkDbContext dbContext, UserManager<AspNetUser> userManager, StubSwedishClock clock)
        {
            return new UserService(dbContext, userManager, _options, clock, _notificationService, _logger);
        }

        private TolkDbContext CreateTolkDbContext()
        {
            var options = new DbContextOptionsBuilder<TolkDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new TolkDbContext(options);
        }
        private TolkDbContext GetBaseContext()
        {
            var tolkDbContext = CreateTolkDbContext();
            var mockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers);
            foreach (var user in mockCustomerUsers)
            {
                user.TwoFactorEntries = [];
            }
            tolkDbContext.Users.AddRange(mockCustomerUsers);
            tolkDbContext.SaveChanges();
            return tolkDbContext;
        }

        [Theory]
        [InlineData("2018-05-01 13:00:00 +02:00", "2018-05-01 13:00:01 +02:00", null, null, null, TwoFactorState.Confirmed)]
        [InlineData("2018-05-01 13:00:00 +02:00", "2018-05-01 13:00:00 +02:00", null, null, null, TwoFactorState.NeedTwoFactorEmail)]
        [InlineData("2018-05-01 13:00:00 +02:00", "2018-05-01 12:59:59 +02:00", null, null, null, TwoFactorState.NeedTwoFactorEmail)]
        [InlineData("2018-05-01 13:00:00 +02:00", null, "2018-05-01 12:59:59 +02:00", null, null, TwoFactorState.NeedTwoFactorEmail)]
        [InlineData("2018-05-01 13:00:00 +02:00", null, "2018-05-02 13:00:00 +02:00", "code", null, TwoFactorState.Awaiting)]
        [InlineData("2018-05-01 13:00:00 +02:00", null, "2018-05-02 13:00:00 +02:00", null, null, TwoFactorState.NeedTwoFactorEmail)]
        [InlineData("2018-05-02 13:00:00 +02:00", null, null, null, "2018-05-02 14:00:00 +02:00", TwoFactorState.LockedOut)]
        [InlineData("2018-05-02 13:00:00 +02:00", null, null, null, "2018-05-02 12:00:00 +02:00", TwoFactorState.NeedTwoFactorEmail)]
        public void TwoFactorDto(string nowString, string twoFactorExpiresAtString, string validationCodeExpiresAtString, string validationCode, string lockedouExpiresAsString, TwoFactorState expected)
        {
            var now = DateTimeOffset.Parse(nowString);
            DateTimeOffset? twoFactorExpiresAt = !string.IsNullOrEmpty(twoFactorExpiresAtString) ? DateTimeOffset.Parse(twoFactorExpiresAtString) : null;
            DateTimeOffset? validationCodeExpiresAt = !string.IsNullOrEmpty(validationCodeExpiresAtString) ? DateTimeOffset.Parse(validationCodeExpiresAtString) : null;
            DateTimeOffset? lockedoutExpiresAt = !string.IsNullOrEmpty(lockedouExpiresAsString) ? DateTimeOffset.Parse(lockedouExpiresAsString) : null;

            var testDto = new TwoFactorDto { TwoFactorExpiresAt = twoFactorExpiresAt, ValidationCodeExpiresAt = validationCodeExpiresAt, ValidationCode = validationCode, LockoutExpiresAt = lockedoutExpiresAt };
            Assert.Equal(expected, testDto.NeedTwoFactor(now));
        }

        [Theory]
        [InlineData("2018-05-02 13:00:00 +02:00", null, null, TwoFactorState.NeedTwoFactorEmail)]
        [InlineData("2018-05-02 13:00:00 +02:00", "cookie", null, TwoFactorState.NeedTwoFactorEmail)]
        [InlineData("2018-05-02 13:00:00 +02:00", "cookie", "{\"TwoFactorExpiresAt\": \"2018-05-03 12:00:00 +02:00\",\"ValidationCode\": null,\"ValidationCodeExpiresAt\": null}", TwoFactorState.Confirmed)]
        [InlineData("2018-05-02 13:00:00 +02:00", "cookie", "{\"TwoFactorExpiresAt\": \"2018-05-01 12:00:00 +02:00\",\"ValidationCode\": null,\"ValidationCodeExpiresAt\": null}", TwoFactorState.NeedTwoFactorEmail)]
        [InlineData("2018-05-02 13:00:00 +02:00", "cookie", "{\"TwoFactorExpiresAt\": null,\"ValidationCode\": \"apa\",\"ValidationCodeExpiresAt\": \"2018-05-03 12:00:00 +02:00\"}", TwoFactorState.Awaiting)]
        [InlineData("2018-05-02 13:00:00 +02:00", "cookie", "{\"TwoFactorExpiresAt\": null,\"ValidationCode\": \"apa\",\"ValidationCodeExpiresAt\": \"2018-05-01 12:00:00 +02:00\"}", TwoFactorState.NeedTwoFactorEmail)]
        [InlineData("2018-05-02 13:00:00 +02:00", "cookie", "{\"TwoFactorExpiresAt\": null,\"ValidationCode\": null ,\"ValidationCodeExpiresAt\": null ,\"LockoutExpiresAt\": \"2018-05-02 14:00:00 +02:00\" }", TwoFactorState.LockedOut)]
        [InlineData("2018-05-02 13:00:00 +02:00", "cookie", "{\"TwoFactorExpiresAt\": null,\"ValidationCode\": null ,\"ValidationCodeExpiresAt\": null ,\"LockoutExpiresAt\": \"2018-05-02 12:00:00 +02:00\" }", TwoFactorState.NeedTwoFactorEmail)]
        public async Task GetCurrentTwoFactorClaim(string nowString, string cookieId, string claimValue, TwoFactorState expectedState)
        {
            var dbContext = GetBaseContext();

            var clock = new StubSwedishClock(nowString);
            var user = await dbContext.Users.SingleAsync(u => u.Id == 1);
            if (!string.IsNullOrEmpty(cookieId) && !string.IsNullOrEmpty(claimValue))
            {
                user.TwoFactorEntries = [new TwoFactorEntry()
                {
                    UserId = 1,
                    DeviceId = cookieId,
                    StateInformation = claimValue
                }];
            }
            else
            {
                user.TwoFactorEntries = [];
            }
            await dbContext.SaveChangesAsync();
            var sut = CreateUserService(dbContext, null, clock);
            (var returnedCookieId, TwoFactorState state) = await sut.GetCurrentTwoFactorClaim(user.UserName, cookieId);
            Assert.NotNull(returnedCookieId);
            Assert.Equal(expectedState, state);
        }

        [Fact]
        public void TestValidationCodeGeneration()
        {
            var sut = CreateUserService(null, null, null);
            string result = sut.GenerateValidationCode();
            Assert.Equal(_options.Value.TwoFactor.CodeLength, result.Length);
        }

        [Fact]
        public async Task Test_InitiateTwoFactorValidation()
        {
            var dbContext = GetBaseContext();

            var clock = new StubSwedishClock("2018-05-02 13:00:00 +02:00");
            var user = await dbContext.Users.SingleAsync(u => u.Id == 1);
            var sut = CreateUserService(dbContext, null, clock);
            await sut.InitiateTwoFactorValidation(user.UserName, "cookieId");

            Assert.Single((await dbContext.Users.GetUserByNameWithTwoFactor(user.UserName)).TwoFactorEntries);
        }
        [Fact]
        public async Task Test_InitiateTwoFactorValidation_Twice_from_same_Device()
        {
            var dbContext = GetBaseContext();

            var clock = new StubSwedishClock("2018-05-02 13:00:00 +02:00");
            var user = await dbContext.Users.SingleAsync(u => u.Id == 1);
            var sut = CreateUserService(dbContext, null, clock);
            await sut.InitiateTwoFactorValidation(user.UserName, "cookieId");
            await sut.InitiateTwoFactorValidation(user.UserName, "cookieId");

            Assert.Single((await dbContext.Users.GetUserByNameWithTwoFactor(user.UserName)).TwoFactorEntries);
        }

        [Fact]
        public async Task Test_InitiateTwoFactorValidation_Twice_from_different_Devices()
        {
            var dbContext = GetBaseContext();

            var clock = new StubSwedishClock("2018-05-02 13:00:00 +02:00");
            var user = await dbContext.Users.SingleAsync(u => u.Id == 1);
            var sut = CreateUserService(dbContext, null, clock);
            await sut.InitiateTwoFactorValidation(user.UserName, "cookieId");
            await sut.InitiateTwoFactorValidation(user.UserName, "cookieId2");

            Assert.Equal(2, (await dbContext.Users.GetUserByNameWithTwoFactor(user.UserName)).TwoFactorEntries.Count);
        }

        [Fact]
        public void Test_GenerateConfirmHash()
        {
            var sut = CreateUserService(null, null, null);
            Assert.NotEqual("apa", sut.GenerateConfirmHash("apa"));
        }

        [Fact]
        public void Test_ValidateConfirmHash()
        {
            var sut = CreateUserService(null, null, null);
            var hash = sut.GenerateConfirmHash("apa");
            Assert.True(sut.ValidateConfirmHash("apa", hash));
        }

        [Fact]
        public async Task Test_SetTwoFactorCode()
        {
            var dbContext = GetBaseContext();

            var clock = new StubSwedishClock("2018-05-02 13:00:00 +02:00");
            var user = await dbContext.Users.SingleAsync(u => u.Id == 1);
            var sut = CreateUserService(dbContext, null, clock);
            await sut.SetConfirmedTwoFactor(user.UserName, "id");
            (string _, TwoFactorState state) = (await sut.GetCurrentTwoFactorClaim(user.UserName, "id"));
            Assert.Equal(TwoFactorState.Confirmed, state);
        }

        [Fact]
        public async Task Test_AddFirstFailedTry()
        {
            var dbContext = GetBaseContext();

            var clock = new StubSwedishClock("2018-05-02 13:00:00 +02:00");
            var user = await dbContext.Users.SingleAsync(u => u.Id == 1);
            var sut = CreateUserService(dbContext, null, clock);
            await sut.InitiateTwoFactorValidation(user.UserName, "cookieId");
            Assert.True(await sut.AddFailedTwoFactorTry(user, "cookieId"));
        }
        [Fact]
        public async Task Test_AddFirstFailedTryWithoutValidation()
        {
            var dbContext = GetBaseContext();

            var clock = new StubSwedishClock("2018-05-02 13:00:00 +02:00");
            var user = await dbContext.Users.SingleAsync(u => u.Id == 1);
            var sut = CreateUserService(dbContext, null, clock);
            //No validation ongoing, should return true
            Assert.True(await sut.AddFailedTwoFactorTry(user, "cookieId"));
        }

        [Fact]
        public async Task Test_AddOneTooManyFailedTry()
        {
            var dbContext = GetBaseContext();

            var clock = new StubSwedishClock("2018-05-02 13:00:00 +02:00");
            var user = await dbContext.Users.SingleAsync(u => u.Id == 1);
            var sut = CreateUserService(dbContext, null, clock);
            await sut.InitiateTwoFactorValidation(user.UserName, "cookieId");
            for (int i = 0; i < _options.Value.TwoFactor.MaxNumberOfTries; ++i)
            {
                Assert.True(await sut.AddFailedTwoFactorTry(user, "cookieId"));
            }
            Assert.False(await sut.AddFailedTwoFactorTry(user, "cookieId"));
        }
    }
}
