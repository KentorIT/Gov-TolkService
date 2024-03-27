using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Models.CustomerSpecificProperties;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class CacheService
    {
        private readonly IDistributedCache _cache;
        private readonly TolkDbContext _dbContext;
        private readonly ITolkBaseOptions _options;
        private readonly ISwedishClock _clock;

        public CacheService(IDistributedCache cache, TolkDbContext dbContext, ITolkBaseOptions options, ISwedishClock clock)
        {
            _cache = cache;
            _dbContext = dbContext;
            _options = options;
            _clock = clock;
        }

        public async Task FlushAll()
        {
            await _cache.RemoveAsync(CacheKeys.BrokerFeesByRegionAndBroker);
            await _cache.RemoveAsync(CacheKeys.BrokerFeesByRegionAndServiceType);
            await _cache.RemoveAsync(CacheKeys.OrganisationSettings);
            await _cache.RemoveAsync(CacheKeys.Holidays);
            await _cache.RemoveAsync(CacheKeys.CustomerSettings);
            await _cache.RemoveAsync(CacheKeys.CurrentOrLatestFrameworkAgreement);
            await _cache.RemoveAsync(CacheKeys.FrameworkAgreementList);
            await _cache.RemoveAsync(CacheKeys.CustomerSpecificProperties);
            await _cache.RemoveAsync(CacheKeys.CustomerOrderAgreementSettings);
        }

        public async Task Flush(string id)
        {
            await _cache.RemoveAsync(id);
        }

        public CurrentOrLatestFrameworkAgreement CurrentOrLatestFrameworkAgreement
        {
            get
            {
                var currentFrameworkAgreement = _cache.Get(CacheKeys.CurrentOrLatestFrameworkAgreement).FromByteArray<CurrentOrLatestFrameworkAgreement>();

                if (currentFrameworkAgreement == null)
                {
                    var now = _clock.SwedenNow;
                    var agreement = _dbContext.FrameworkAgreements.GetCurrentOrLatestActiveFrameworkAgreementByDate(now.Date);
                    currentFrameworkAgreement = agreement != null ?
                        new CurrentOrLatestFrameworkAgreement
                        {
                            FrameworkAgreementId = agreement.FrameworkAgreementId,
                            AgreementNumber = agreement.AgreementNumber,
                            LastValidDate = agreement.LastValidDate,
                            FirstValidDate = agreement.FirstValidDate,
                            OriginalLastValidDate = agreement.OriginalLastValidDate,                            
                            Description = agreement.Description,
                            BrokerFeeCalculationType = agreement.BrokerFeeCalculationType,
                            FrameworkAgreementResponseRuleset = agreement.FrameworkAgreementResponseRuleset,
                            IsActive = agreement.LastValidDate >= now.Date && now.Date >= agreement.FirstValidDate,
                        } : 
                        new CurrentOrLatestFrameworkAgreement { IsActive = false };

                    _cache.Set(CacheKeys.CurrentOrLatestFrameworkAgreement, currentFrameworkAgreement.ToByteArray(),new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = now.Date.AddDays(1) - now.Date });
                    
                }                
                return currentFrameworkAgreement;
            }
        }

        public IEnumerable<FrameworkAgreementNumberIdModel> FrameworkAgreementList
        {
            get
            {
                var frameworkAgreementList = _cache.Get(CacheKeys.FrameworkAgreementList).FromByteArray<IEnumerable<FrameworkAgreementNumberIdModel>>();

                if (frameworkAgreementList == null)
                {
                    var now = _clock.SwedenNow;
                    frameworkAgreementList = _dbContext.FrameworkAgreements.Select(f => new FrameworkAgreementNumberIdModel
                    {
                        FrameWorkAgreementId = f.FrameworkAgreementId,
                        FrameWorkAgreementNumber = f.AgreementNumber
                    });
                    _cache.Set(CacheKeys.FrameworkAgreementList, frameworkAgreementList.ToByteArray());
                }
                return frameworkAgreementList;
            }
        }

        public IEnumerable<PriceInformationBrokerFee> BrokerFeeByRegionAndBrokerPriceList
        {
            get
            {
                var brokerFees = _cache.Get(CacheKeys.BrokerFeesByRegionAndBroker).FromByteArray<IEnumerable<PriceInformationBrokerFee>>();

                if (brokerFees == null)
                {
                    brokerFees = GetBrokerFeePriceList();
                    _cache.Set(CacheKeys.BrokerFeesByRegionAndBroker, brokerFees.ToByteArray());
                }
                return brokerFees;
            }
        }

        public IEnumerable<BrokerFeeByRegionAndServiceType> BrokerFeeByRegionAndServiceTypePriceList
        {
            get
            {
                var brokerFees = _cache.Get(CacheKeys.BrokerFeesByRegionAndServiceType).FromByteArray<IEnumerable<BrokerFeeByRegionAndServiceType>>();

                if (brokerFees == null)
                {
                    brokerFees = _dbContext.BrokerFeeByServiceTypePriceListRows.Select(bf => new BrokerFeeByRegionAndServiceType
                    {
                        BrokerFee = bf.Price,
                        CompetenceLevel = bf.CompetenceLevel,
                        InterpreterLocation = bf.InterpreterLocation,
                        RegionId = bf.RegionId,
                        StartDate = bf.FirstValidDate,
                        EndDate = bf.LastValidDate
                    }).ToList();
                    _cache.Set(CacheKeys.BrokerFeesByRegionAndServiceType, brokerFees.ToByteArray());
                }
                return brokerFees;
            }
        }

        public IEnumerable<Holiday> Holidays
        {
            get
            {
                var holidays = _cache.Get(CacheKeys.Holidays).FromByteArray<IEnumerable<Holiday>>();

                if (holidays == null)
                {
                    holidays = _dbContext.Holidays.ToList().AsReadOnly();
                    _cache.Set(CacheKeys.Holidays, holidays.ToByteArray(), new DistributedCacheEntryOptions().SetAbsoluteExpiration(DateTimeOffset.Now.AddDays(1)));
                }

                return holidays;
            }
        }

        public IEnumerable<OrganisationNotificationSettings> OrganisationNotificationSettings
        {
            get
            {
                var organisationNotificationSettings = _cache.Get(CacheKeys.OrganisationSettings).FromByteArray<IEnumerable<OrganisationNotificationSettings>>();

                if (organisationNotificationSettings == null)
                {
                    organisationNotificationSettings = _dbContext.Users
                        .Where(u => (u.BrokerId != null) && u.IsApiUser)
                        .SelectMany(u => u.NotificationSettings)
                        .Select(n => new OrganisationNotificationSettings
                        {
                            ReceivingOrganisationId = n.User.BrokerId.Value,
                            NotificationConsumerType = NotificationConsumerType.Broker,
                            ContactInformation = n.ConnectionInformation ?? (n.NotificationChannel == NotificationChannel.Email ? n.User.Email : null),
                            NotificationChannel = n.NotificationChannel,
                            NotificationType = n.NotificationType,
                            RecipientUserId = n.UserId
                        }).ToList().Union(_dbContext.Users
                        .Where(u => (u.CustomerOrganisationId != null) && u.IsApiUser)
                        .SelectMany(u => u.NotificationSettings)
                        .Select(n => new OrganisationNotificationSettings
                        {
                            ReceivingOrganisationId = n.User.CustomerOrganisationId.Value,
                            ContactInformation = n.ConnectionInformation ?? (n.NotificationChannel == NotificationChannel.Email ? n.User.Email : null),
                            NotificationConsumerType = NotificationConsumerType.Customer,
                            NotificationChannel = n.NotificationChannel,
                            NotificationType = n.NotificationType,
                            RecipientUserId = n.UserId
                        })).ToList().Union(_dbContext.CustomerSettings
                        .Where(s => s.CustomerSettingType == CustomerSettingType.UseOrderAgreements && s.Value)
                        .Select(s => new OrganisationNotificationSettings
                        {
                            ReceivingOrganisationId = s.CustomerOrganisationId,
                            NotificationConsumerType = NotificationConsumerType.Customer,
                            NotificationChannel = NotificationChannel.Peppol,
                            NotificationType = NotificationType.OrderAgreementCreated,
                            StartUsingNotificationAt = s.CustomerOrganisation.UseOrderAgreementsFromDate
                        })).ToList().AsReadOnly();
                    _cache.Set(CacheKeys.OrganisationSettings, organisationNotificationSettings.ToByteArray(), new DistributedCacheEntryOptions().SetAbsoluteExpiration(DateTimeOffset.Now.AddDays(1)));
                }
                return organisationNotificationSettings;
            }
        }

        private List<PriceInformationBrokerFee> GetBrokerFeePriceList()
        {
            List<PriceListRow> prices = _dbContext.PriceListRows.Where(p => p.MaxMinutes == 60 && p.PriceListRowType == PriceListRowType.BasePrice && p.PriceListType == PriceListType.Court).ToList();
            List<Ranking> ranks = _dbContext.Rankings.Where(r => r.FrameworkAgreement.BrokerFeeCalculationType == BrokerFeeCalculationType.ByRegionAndBroker).ToList();

            List<PriceInformationBrokerFee> priceListBrokerFee = new List<PriceInformationBrokerFee>();
            foreach (var item in prices)
            {
                priceListBrokerFee.AddRange(ranks.Select(r => new PriceInformationBrokerFee
                {
                    BrokerFee = r.BrokerFee.Value,
                    FirstValidDateRanking = r.FirstValidDate,
                    LastValidDateRanking = r.LastValidDate,
                    RankingId = r.RankingId,
                    CompetenceLevel = item.CompetenceLevel,
                    EndDatePriceList = item.EndDate,
                    BasePrice = item.Price,
                    PriceListRowId = item.PriceListRowId.Value,
                    StartDatePriceList = item.StartDate,
                    RoundDecimals = _options.RoundPriceDecimals
                }).ToList());
            }
            return priceListBrokerFee;
        }

        public IEnumerable<CustomerSettingsModel> CustomerSettings
        {
            get
            {
                var customerSettings = _cache.Get(CacheKeys.CustomerSettings).FromByteArray<IEnumerable<CustomerSettingsModel>>();
                if (customerSettings == null)
                {
                    //only get the used settings (value = true)
                    var tempCustomerSettings = _dbContext.CustomerSettings.ToList().GroupBy(cs => cs.CustomerOrganisationId);
                    customerSettings = tempCustomerSettings.Select(cs => new CustomerSettingsModel { CustomerOrganisationId = cs.Key, UsedCustomerSettingTypes = cs.Where(c => c.Value).Select(c => c.CustomerSettingType).ToList() }).ToList();
                    _cache.Set(CacheKeys.CustomerSettings, customerSettings.ToByteArray(), new DistributedCacheEntryOptions().SetAbsoluteExpiration(DateTimeOffset.Now.AddDays(1)));
                }
                return customerSettings;
            }
        }

        public IEnumerable<CustomerSpecificPropertyModel> ActiveCustomerSpecificProperties
        {
            get
            {
                var customerSpecificProperties = _cache.Get(CacheKeys.CustomerSpecificProperties).FromByteArray<IEnumerable<CustomerSpecificPropertyModel>>();
                if (customerSpecificProperties == null)
                {
                    customerSpecificProperties = _dbContext.CustomerSpecificProperties.Select(c => new CustomerSpecificPropertyModel(c)).ToList();
                    _cache.Set(CacheKeys.CustomerSpecificProperties, customerSpecificProperties.ToByteArray(), new DistributedCacheEntryOptions().SetAbsoluteExpiration(DateTimeOffset.Now.AddDays(1)));
                }
                return customerSpecificProperties.Where(csp => csp.Enabled);
            }
        }

        public IEnumerable<CustomerSpecificPropertyModel> AllCustomerSpecificProperties
        {
            get {
                var customerSpecificProperties = _cache.Get(CacheKeys.CustomerSpecificProperties).FromByteArray<IEnumerable<CustomerSpecificPropertyModel>>();
                if(customerSpecificProperties == null)
                {
                    customerSpecificProperties = _dbContext.CustomerSpecificProperties.Select(c => new CustomerSpecificPropertyModel(c)).ToList();
                    _cache.Set(CacheKeys.CustomerSpecificProperties, customerSpecificProperties.ToByteArray(), new DistributedCacheEntryOptions().SetAbsoluteExpiration(DateTimeOffset.Now.AddDays(1)));
                }
                return customerSpecificProperties;
            }
        }

        public IEnumerable<CustomerOrderAgreementSettingsModel> CustomerOrderAgreementSettings
        {
            get {
                var customerOrderAgreementSettings = _cache.Get(CacheKeys.CustomerOrderAgreementSettings).FromByteArray<IEnumerable<CustomerOrderAgreementSettingsModel>>();
                if (customerOrderAgreementSettings == null)
                {
                    customerOrderAgreementSettings = _dbContext.CustomerOrderAgreementSettings.Include(coas => coas.Broker).Include(coas => coas.CustomerOrganisation).Select(
                        coas => new CustomerOrderAgreementSettingsModel{
                            CustomerOrganisationId = coas.CustomerOrganisationId,
                            CustomerName = coas.CustomerOrganisation.Name,
                            BrokerId = coas.BrokerId,
                            BrokerName = coas.Broker.Name,
                            EnabledAt = coas.EnabledAt,                            
                    }).ToList();

                    _cache.Set(CacheKeys.CustomerOrderAgreementSettings, customerOrderAgreementSettings.ToByteArray(), new DistributedCacheEntryOptions().SetAbsoluteExpiration(DateTimeOffset.Now.AddDays(1)));
                }
                return customerOrderAgreementSettings;
            }
        }        
    }
}

