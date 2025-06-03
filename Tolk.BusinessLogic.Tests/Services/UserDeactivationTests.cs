using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
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
    public class UserDeactivationTests
    {
        private readonly ILogger<UserService> _logger;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IOptions<TolkOptions> _options;
        private int[] thresholdDays = { 1, 5, 10, 20, 30 };
        private readonly string _userCreationDate = "2025-01-01 00:00:00 +02:00";
        public UserDeactivationTests()
        {
            _logger = Mock.Of<ILogger<UserService>>();            
            _notificationService = Mock.Of<INotificationService>();
            _options = Options.Create(
              new TolkOptions()
              {
                  UserInactivity = new UserInactivitySettings
                  {
                      EnableAutomaticDeactivation = true,
                      NotifyDaysBeforeDeactivation = thresholdDays.ToList(),
                      InactivationThresholdMonths = 12
                  },
                  PublicOrigin = "https://test.tolk.avropa.se",
                  Support = new SupportSettings
                  {
                      FirstLineEmail = "help@deactivation.se"
                  }
              });
        }        

        private UserService CreateUserService(TolkDbContext dbContext, StubSwedishClock clock)
        {
            return new UserService(dbContext, null, _options, clock, _notificationService, _logger);
        }

        private TolkDbContext CreateTolkDbContext()
        {
            var options = new DbContextOptionsBuilder<TolkDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new TolkDbContext(options);
        }
        private TolkDbContext GetBaseContext(bool isActive = true, bool? stateChangeByAdmin = false)
        {
            var tolkDbContext = CreateTolkDbContext();
            var mockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers);
            foreach (var user in mockCustomerUsers)
            {
                user.IsActive = isActive;
                user.ActivityStateChangedByAdmin = stateChangeByAdmin;
            }
            tolkDbContext.Users.AddRange(mockCustomerUsers);
            foreach (var user in mockCustomerUsers)
            {
                tolkDbContext.UserAuditLogEntries.Add(new UserAuditLogEntry
                {
                    UserId = user.Id,
                    UserChangeType = UserChangeType.Created,
                    LoggedAt = DateTimeOffset.Parse(_userCreationDate)
                });
            }
            tolkDbContext.SaveChanges();
            return tolkDbContext;
        }


        [Theory]
        [InlineData(new int[] { 5, 4, 3, 2, 1 }, new int[] { 5, 4, 3, 2, 1 })]
        [InlineData(new int[] {1, 2, 3, 4, 5}, new int[] { 5, 4, 3, 2, 1 })]
        [InlineData(new int[] { 1, 3, 2, 5, 4 }, new int[] { 5, 4, 3, 2, 1 })]
        public void ThresholdDaysShouldAlwaysReturnSortedDescending(int[] days, int[] expected)
        {
            var inactivitySettings = new UserInactivitySettings
            {
                NotifyDaysBeforeDeactivation = days.ToList()
            };

            Assert.Equal(inactivitySettings.NotifyDaysBeforeDeactivation, expected);
        }
        
        [Theory]
        [InlineData(1, "2024-07-01 00:00:00 +02:00", "2025-06-01 00:00:00 +02:00")]
        [InlineData(2, "2024-06-21 00:00:00 +02:00", "2025-06-01 00:00:00 +02:00")]
        [InlineData(3, "2024-06-11 14:00:00 +02:00", "2025-06-01 02:00:00 +02:00")]
        [InlineData(4, "2024-06-06 00:00:00 +02:00", "2025-06-01 00:00:00 +02:00")]
        [InlineData(5, "2024-06-02 00:00:00 +02:00", "2025-06-01 00:00:00 +02:00")]
        public async Task UserShouldGetReminderOfDeactivationPriorToDeactivation(int userId, string lastLogin, string now)
        {
            // ARRANGE
            var dbContext = GetBaseContext();
            var user = dbContext.Users.First(u => u.Id == userId);
            user.LastLoginAt = DateTimeOffset.Parse(lastLogin);
            dbContext.SaveChanges();
            var clock = new StubSwedishClock(now);
            var sut = CreateUserService(dbContext, clock);            
            var daysDiff = (user.LastLoginAt.Value.AddMonths(_options.Value.UserInactivity.InactivationThresholdMonths) - clock.SwedenNow).Days;

            // ACT
            await sut.HandleInactiveUsers();

            // ASSERT 
            var notifyMock = Mock.Get(_notificationService);
            notifyMock.Verify(l => l.CreateEmail(
                It.Is<string>(s => s == user.Email),
                It.Is<string>(s => s == $"Ditt konto hos {Constants.SystemName} kommer snart att deaktiveras"),
                It.Is<string>(s => s.Contains($"inaktiveras om {daysDiff} dagar")),
                It.Is<string>(s => s.Contains($"inaktiveras om <b>{daysDiff}</b> dagar")),
                It.Is<NotificationType>(s => s == NotificationType.DeactivationReminder),
                null,
                It.Is<bool>(b => b == false),
                It.Is<bool>(b => b == false)
                ), Times.Once);

        }

        [Theory]
        [InlineData(1, "2024-06-01 00:00:00 +02:00", "2025-06-01 00:00:00 +02:00")]      
        [InlineData(1, "2022-12-01 00:00:00 +01:00", "2025-06-01 00:00:00 +02:00")]      
        [InlineData(1, "2020-06-01 00:00:00 +02:00", "2025-06-01 00:00:00 +02:00")]   
        public async Task UserShouldBeDeactivatedBySystem(int userId, string lastLogin, string now)
        {
            // ARRANGE
            var dbContext = GetBaseContext();
            var user = dbContext.Users.First(u => u.Id == userId);
            user.LastLoginAt = DateTimeOffset.Parse(lastLogin);
            dbContext.SaveChanges();
            var clock = new StubSwedishClock(now);
            var sut = CreateUserService(dbContext, clock);
            //var daysDiff = (user.LastLoginAt.Value.AddMonths(_options.Value.UserInactivity.InactivationThresholdMonths) - clock.SwedenNow).Days;
            // ACT
            await sut.HandleInactiveUsers();
            // ASSERT 
            var auditLog = dbContext.UserAuditLogEntries.SingleOrDefault(ale => ale.UserId == userId && ale.UserChangeType == UserChangeType.ChangedActivityState);
            Assert.False(user.IsActive);
            Assert.False(user.ActivityStateChangedByAdmin);
            Assert.NotNull(auditLog);

            var notifyMock = Mock.Get(_notificationService);
            notifyMock.Verify(l => l.CreateEmail(
                It.Is<string>(s => s == user.Email),
                It.Is<string>(s => s == $"Ditt konto hos {Constants.SystemName} kommer snart att deaktiveras"),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<NotificationType>(s => s == NotificationType.DeactivationReminder),
                null,
                It.Is<bool>(b => b == false),
                It.Is<bool>(b => b == false)
                ), Times.Never);
        }
      
        [Theory]
        [InlineData(1, "2026-01-01 00:00:00 +01:00")]      
        [InlineData(1, "2026-04-02 00:00:00 +02:00")]      
        [InlineData(1, "2026-03-03 00:00:00 +01:00")]   
        public async Task UserShouldBeDeactivatedBySystemIfNeverLoggedIn(int userId, string now)
        {
            // ARRANGE
            var dbContext = GetBaseContext();
            var user = dbContext.Users.First(u => u.Id == userId);
            var clock = new StubSwedishClock(now);
            var sut = CreateUserService(dbContext, clock);            

            // ACT
            await sut.HandleInactiveUsers();

            // ASSERT 
            var auditLog = dbContext.UserAuditLogEntries.SingleOrDefault(ale => ale.UserId == userId && ale.UserChangeType == UserChangeType.ChangedActivityState);
            Assert.False(user.IsActive);
            Assert.False(user.ActivityStateChangedByAdmin);
            Assert.NotNull(auditLog);

            var notifyMock = Mock.Get(_notificationService);
            notifyMock.Verify(l => l.CreateEmail(
                It.Is<string>(s => s == user.Email),
                It.Is<string>(s => s == $"Ditt konto hos {Constants.SystemName} kommer snart att deaktiveras"),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<NotificationType>(s => s == NotificationType.DeactivationReminder),
                null,
                It.Is<bool>(b => b == false),
                It.Is<bool>(b => b == false)
                ), Times.Never);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task UserShouldBeAbleToReactivateIfNotDeactivatedByAdmin(int userId)
        {
            // ARRANGE
            var dbContext = GetBaseContext(isActive: false);
            var user = dbContext.Users.First(u => u.Id == userId);
            var clock = new StubSwedishClock("2025-01-01 00:00:00 +02:00");
            var sut = CreateUserService(dbContext, clock);

            // ACT
            var successfulActivation = await sut.TryActivateUser(user);

            // ASSERT 
            Assert.True(successfulActivation);
            Assert.True(user.IsActive);
            Assert.False(user.ActivityStateChangedByAdmin);
            
        }

        [Theory]
        [InlineData(1,true)]
        [InlineData(2,null)] // Null is seen as deactivated by admin
        [InlineData(3,true)]
        public async Task UserShouldNotBeAbleToReactivateIfNotDeactivatedByAdmin(int userId, bool? stateChangeByAdmin)
        {
            // ARRANGE
            var dbContext = GetBaseContext(isActive: false,stateChangeByAdmin: stateChangeByAdmin);
            var user = dbContext.Users.First(u => u.Id == userId);
            var clock = new StubSwedishClock("2025-01-01 00:00:00 +02:00");
            var sut = CreateUserService(dbContext, clock);

            // ACT
            var successfulActivation = await sut.TryActivateUser(user);

            // ASSERT 
            Assert.False(successfulActivation);
            Assert.False(user.IsActive);
            Assert.True(user.ActivityStateChangedByAdmin ?? true);

        }

        // Test reminders        
        [Theory]
        [InlineData(1, "2024-07-02 00:00:00 +02:00", "2025-06-01 00:00:00 +02:00")]
        [InlineData(2, "2024-06-20 00:00:00 +02:00", "2025-06-01 00:00:00 +02:00")]
        [InlineData(3, "2024-06-10 14:00:00 +02:00", "2025-06-01 02:00:00 +02:00")]
        [InlineData(4, "2024-06-07 00:00:00 +02:00", "2025-06-01 00:00:00 +02:00")]
        [InlineData(5, "2024-06-04 00:00:00 +02:00", "2025-06-01 00:00:00 +02:00")]
        public async Task UserShouldNotGetReminderIfNotOnThresholdDay(int userId, string lastLogin, string now)
        {
            // ARRANGE
            var dbContext = GetBaseContext();
            var user = dbContext.Users.First(u => u.Id == userId);
            user.LastLoginAt = DateTimeOffset.Parse(lastLogin);
            dbContext.SaveChanges();
            var clock = new StubSwedishClock(now);
            var sut = CreateUserService(dbContext, clock);
            var daysDiff = (user.LastLoginAt.Value.AddMonths(_options.Value.UserInactivity.InactivationThresholdMonths) - clock.SwedenNow).Days;
            // ACT
            await sut.HandleInactiveUsers();
            // ASSERT 

            var notifyMock = Mock.Get(_notificationService);
            notifyMock.Verify(l => l.CreateEmail(
                It.Is<string>(s => s == user.Email),
                It.Is<string>(s => s == $"Ditt konto hos {Constants.SystemName} kommer snart att deaktiveras"),
                It.Is<string>(s => s.Contains($"inaktiveras om {daysDiff} dagar")),
                It.Is<string>(s => s.Contains($"inaktiveras om <b>{daysDiff}</b> dagar")),
                It.Is<NotificationType>(s => s == NotificationType.DeactivationReminder),
                null,
                It.Is<bool>(b => b == false),
                It.Is<bool>(b => b == false)
                ), Times.Never);

        }

        [Theory]
        [InlineData(1, "2024-07-01 00:00:00 +02:00", "2025-06-01 00:00:00 +02:00")]
        [InlineData(2, "2024-06-21 00:00:00 +02:00", "2025-06-01 00:00:00 +02:00")]
        [InlineData(3, "2024-06-11 14:00:00 +02:00", "2025-06-01 02:00:00 +02:00")]
        [InlineData(4, "2024-06-06 00:00:00 +02:00", "2025-06-01 00:00:00 +02:00")]
        [InlineData(5, "2024-06-02 00:00:00 +02:00", "2025-06-01 00:00:00 +02:00")]
        public async Task UserShouldNotGetReminderIfAlreadyDeactivated(int userId, string lastLogin, string now)
        {
            // ARRANGE
            var dbContext = GetBaseContext(isActive: false);
            var user = dbContext.Users.First(u => u.Id == userId);
            user.LastLoginAt = DateTimeOffset.Parse(lastLogin);
            dbContext.SaveChanges();
            var clock = new StubSwedishClock(now);
            var sut = CreateUserService(dbContext, clock);
            var daysDiff = (user.LastLoginAt.Value.AddMonths(_options.Value.UserInactivity.InactivationThresholdMonths) - clock.SwedenNow).Days;
            // ACT
            await sut.HandleInactiveUsers();
            // ASSERT 

            var notifyMock = Mock.Get(_notificationService);
            notifyMock.Verify(l => l.CreateEmail(
                It.Is<string>(s => s == user.Email),
                It.Is<string>(s => s == $"Ditt konto hos {Constants.SystemName} kommer snart att deaktiveras"),
                It.Is<string>(s => s.Contains($"inaktiveras om {daysDiff} dagar")),
                It.Is<string>(s => s.Contains($"inaktiveras om <b>{daysDiff}</b> dagar")),
                It.Is<NotificationType>(s => s == NotificationType.DeactivationReminder),
                null,
                It.Is<bool>(b => b == false),
                It.Is<bool>(b => b == false)
                ), Times.Never);

        }

        [Theory]
        [InlineData(1, "2024-07-01 00:00:00 +02:00", "2025-06-01 00:00:00 +02:00")]
        [InlineData(2, "2024-06-21 00:00:00 +02:00", "2025-06-01 00:00:00 +02:00")]
        [InlineData(3, "2024-06-11 14:00:00 +02:00", "2025-06-01 02:00:00 +02:00")]
        [InlineData(4, "2024-06-06 00:00:00 +02:00", "2025-06-01 00:00:00 +02:00")]
        [InlineData(5, "2024-06-02 00:00:00 +02:00", "2025-06-01 00:00:00 +02:00")]
        public async Task UsersShouldNotBeDeactivatedIfFeatureNotActive(int userId, string lastLogin, string now)
        {
            // ARRANGE
            _options.Value.UserInactivity.EnableAutomaticDeactivation = false;
            var dbContext = GetBaseContext(isActive: false);
            var user = dbContext.Users.First(u => u.Id == userId);
            user.LastLoginAt = DateTimeOffset.Parse(lastLogin);
            dbContext.SaveChanges();
            var clock = new StubSwedishClock(now);
            var sut = CreateUserService(dbContext, clock);
            var daysDiff = (user.LastLoginAt.Value.AddMonths(_options.Value.UserInactivity.InactivationThresholdMonths) - clock.SwedenNow).Days;
            // ACT
            await sut.HandleInactiveUsers();
            // ASSERT 

            var notifyMock = Mock.Get(_notificationService);
            notifyMock.Verify(l => l.CreateEmail(
                It.Is<string>(s => s == user.Email),
                It.Is<string>(s => s == $"Ditt konto hos {Constants.SystemName} kommer snart att deaktiveras"),
                It.Is<string>(s => s.Contains($"inaktiveras om {daysDiff} dagar")),
                It.Is<string>(s => s.Contains($"inaktiveras om <b>{daysDiff}</b> dagar")),
                It.Is<NotificationType>(s => s == NotificationType.DeactivationReminder),
                null,
                It.Is<bool>(b => b == false),
                It.Is<bool>(b => b == false)
                ), Times.Never);

        }       

    }
}
