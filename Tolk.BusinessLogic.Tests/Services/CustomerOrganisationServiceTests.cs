using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Xunit;

namespace Tolk.BusinessLogic.Tests.Services
{
    public class CustomerOrganisationServiceTests
    {
        private readonly StubSwedishClock _clock;
        private readonly List<Broker> _brokers;
        public CustomerOrganisationServiceTests()
        {            
             _clock = new StubSwedishClock("2024-02-08 00:00:00");
            _brokers = new List<Broker>
            {
                new Broker{BrokerId=1,Name="FirstBroker"},
                new Broker{BrokerId=2,Name="SecondBroker"},
                new Broker{BrokerId=3,Name="ThirdBroker"},
                new Broker{BrokerId=4,Name="FourthBroker"},

            };
        }

        private TolkDbContext CreateTolkDbContext(string databaseName = "empty")
        {
            var options = new DbContextOptionsBuilder<TolkDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new TolkDbContext(options);
        }

        private TolkDbContext GetBaseContext()
        {
            var tolkDbContext = CreateTolkDbContext(Guid.NewGuid().ToString());          
     
            tolkDbContext.AddRange(_brokers);

            var mockCustomerUsers = MockEntities.MockCustomers;
            tolkDbContext.AddRange(mockCustomerUsers);

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

        [Fact]
        public async Task Should_Create_Initial_OrderAgreementSettings()
        {
            var context = GetBaseContext();
            var customerService = new CustomerOrganisationService(context, _clock);
            var settings = await customerService.CreateInitialCustomerOrderAgreementSettings(DateTime.Parse("2024-02-09 10:00:00"));

            Assert.Equal(_brokers.Count, settings.Count);
        }

        [Fact]
        public async Task Should_Create_OrderAgreement_Settings_If_Enabled_For_Customer()
        {
            var context = GetBaseContext();
            var customerService = new CustomerOrganisationService(context, _clock);
            var customer = context.CustomerOrganisations.Include(c => c.CustomerOrderAgreementSettings).Single(c => c.CustomerOrganisationId == 1);
            Assert.Empty(customer.CustomerOrderAgreementSettings);

            var settings = await customerService.UpdateOrderAgreementSettings(customer, _clock.SwedenNow, userId:1);
            Assert.Equal(_brokers.Count, settings.Count);
        }

        [Theory]
        [InlineData(true, true, true, true,4)]

        [InlineData(true, true, true, false,3)]
        [InlineData(true, true, false, true, 3)]
        [InlineData(true, false, true, true, 3)]
        [InlineData(false, true, true, true, 3)]
        
        [InlineData(true, true, false, false, 2)]
        [InlineData(true, false, true, false, 2)]
        [InlineData(false, true, true, false, 2)]
        [InlineData(false, true, false, true,2)]        
        [InlineData(false, false, true, true,2)]        
        [InlineData(true, false, false, true,2)]        
                
        [InlineData(true, false, false, false,1)]
        [InlineData(false, true, false, false,1)]
        [InlineData(false, false, true, false,1)]
        [InlineData(false, false, false, true, 1)]
       
        public async Task Should_Keep_Disabled_When_UseOrderAgreementsFromDate_Is_Changed(bool firstBrokerSetting, bool secondBrokerSetting, bool thirdBrokerSetting, bool fourthBrokerSetting, int expectedEnabled)
        {
            var context = GetBaseContext();
            var customerService = new CustomerOrganisationService(context, _clock);
            var customer = context.CustomerOrganisations.Include(c => c.CustomerOrderAgreementSettings).Single(c => c.CustomerOrganisationId == 1);
            var enabledDate = DateTime.Parse("2021-10-26 10:00:00").ToDateTimeOffsetSweden();

            var orderAgreementSettings = new List<CustomerOrderAgreementSettings>{
                CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:1, firstBrokerSetting ? enabledDate : null),
                CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:2, secondBrokerSetting ? enabledDate : null),
                CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:3, thirdBrokerSetting ? enabledDate : null),
                CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:4, fourthBrokerSetting ? enabledDate : null)
            };

            context.AddRange(orderAgreementSettings);
            context.SaveChanges();
            var settings = await customerService.UpdateOrderAgreementSettings(customer, _clock.SwedenNow, userId: 1);
            var enabledSettings = settings.Where(s => !s.Disabled).ToList();
            Assert.Equal(expectedEnabled, enabledSettings.Count());
            Assert.True(enabledSettings.All(s => s.EnabledAt == _clock.SwedenNow));
        }

        [Fact]
        public async Task Should_Enable_All_OrderAgreementSettings_For_Customer_If_All_Is_Disabled_And_UseOrderAgreementsFromDate_Is_Changed()
        {
            var context = GetBaseContext();
            var customerService = new CustomerOrganisationService(context, _clock);
            var customer = context.CustomerOrganisations.Include(c => c.CustomerOrderAgreementSettings).Single(c => c.CustomerOrganisationId == 1);            

            var orderAgreementSettings = new List<CustomerOrderAgreementSettings>{
                CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:1, null),
                CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:2, null),
                CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:3, null),
                CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:4, null)
            };

            context.AddRange(orderAgreementSettings);
            context.SaveChanges();
            var settings = await customerService.UpdateOrderAgreementSettings(customer, _clock.SwedenNow, userId: 1);            
            Assert.True(settings.All(s => !s.Disabled));
            Assert.True(settings.All(s => s.EnabledAt == _clock.SwedenNow));
        }


        [Fact]
        public async Task Should_Update_EnabledAt_OrderAgreementSettings_When_UseOrderAgreementsFromDate_Is_Changed()
        {
            var context = GetBaseContext();
            var customerService = new CustomerOrganisationService(context, _clock);
            var customer = context.CustomerOrganisations.Include(c => c.CustomerOrderAgreementSettings).Single(c => c.CustomerOrganisationId == 1);
            var enabledDate = DateTime.Parse("2021-10-26 10:00:00").ToDateTimeOffsetSweden();

            var orderAgreementSettings = new List<CustomerOrderAgreementSettings>{
                CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:1, enabledDate),
                CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:2, enabledDate),
                CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:3, enabledDate),
                CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:4, enabledDate)
            };

            context.AddRange(orderAgreementSettings);
            context.SaveChanges();
            var settings = await customerService.UpdateOrderAgreementSettings(customer, _clock.SwedenNow, userId: 1);
            Assert.True(settings.All(s => !s.Disabled));
            Assert.True(settings.All(s => s.EnabledAt == _clock.SwedenNow));
        }

        [Fact]
        public async Task Should_Change_EnabledAt_When_Setting_Is_Enabled()
        {
            var context = GetBaseContext();
            var customerService = new CustomerOrganisationService(context, _clock);
            var customer = context.CustomerOrganisations.Include(c => c.CustomerOrderAgreementSettings).Single(c => c.CustomerOrganisationId == 1);
            var enabledDate = DateTime.Parse("2021-10-26 10:00:00").ToDateTimeOffsetSweden();
            var orderAgreementSettings = new List<CustomerOrderAgreementSettings>{
                CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:1, null),
                CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:2, enabledDate),
            };

            context.AddRange(orderAgreementSettings);
            context.SaveChanges();
            var settings = (await customerService.ToggleSpecificOrderAgreementSettings(customerOrganisationId:1, brokerId:1, userId: 1)).CustomerOrderAgreementSettings;
            Assert.True(settings.All(s => !s.Disabled));
            Assert.Single(settings.Where(s => s.EnabledAt == _clock.SwedenNow));
            Assert.Single(settings.Where(s => s.EnabledAt == enabledDate));
            Assert.Equal(2,settings.Count);
        }

        [Fact]
        public async Task Should_Set_Enabled_At_To_Null_When_Disabled()
        {
            var context = GetBaseContext();
            var customerService = new CustomerOrganisationService(context, _clock);
            var customer = context.CustomerOrganisations.Include(c => c.CustomerOrderAgreementSettings).Single(c => c.CustomerOrganisationId == 1);
            var enabledDate = DateTime.Parse("2021-10-26 10:00:00").ToDateTimeOffsetSweden();
            var orderAgreementSettings = new List<CustomerOrderAgreementSettings>{
                CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:1, enabledDate),
                CreateMockCustomerOrderAgreementSettings(customerOrganisationId:1,brokerId:2, enabledDate),
            };

            context.AddRange(orderAgreementSettings);
            context.SaveChanges();
            var settings = (await customerService.ToggleSpecificOrderAgreementSettings(customerOrganisationId: 1, brokerId: 2, userId: 1)).CustomerOrderAgreementSettings;            
            Assert.Empty(settings.Where(s => s.EnabledAt == _clock.SwedenNow));
            Assert.Single(settings.Where(s => s.EnabledAt == enabledDate));
            Assert.Equal(2, settings.Count);

        }

    }
}
