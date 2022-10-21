using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
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
            await _cache.RemoveAsync(CacheKeys.BrokerFeesByRegionGroupAndServiceType);
            await _cache.RemoveAsync(CacheKeys.OrganisationSettings);
            await _cache.RemoveAsync(CacheKeys.Holidays);
            await _cache.RemoveAsync(CacheKeys.CustomerSettings);
            await _cache.RemoveAsync(CacheKeys.CurrentFrameworkAgreement);
        }

        public async Task Flush(string id)
        {
            await _cache.RemoveAsync(id);
        }

        public CurrentFrameworkAgreement CurrentFrameworkAgreement
        {
            get
            {
                var currentFrameworkAgreement = _cache.Get(CacheKeys.CurrentFrameworkAgreement).FromByteArray<CurrentFrameworkAgreement>();

                if (currentFrameworkAgreement == null)
                {
                    var now = _clock.SwedenNow;
                    currentFrameworkAgreement = _dbContext.FrameworkAgreements
                        .Where(f => f.FirstValidDate < now.Date && f.LastValidDate >= now.Date)
                        .Select(f => new CurrentFrameworkAgreement
                        {
                            FrameworkAgreementId = f.FrameworkAgreementId,
                            AgreementNumber = f.AgreementNumber,
                            LastValidDate = f.LastValidDate,
                            FirstValidDate = f.FirstValidDate,
                            OriginalLastValidDate  = f.OriginalLastValidDate,
                            PossibleAgreementExtensionsInMonths = f.PossibleAgreementExtensionsInMonths,
                            Description = f.Description,
                            BrokerFeeCalculationType = f.BrokerFeeCalculationType,
                            FrameworkAgreementResponseRuleset = f.FrameworkAgreementResponseRuleset,
                            IsActive = true
                        }).SingleOrDefault() ?? new CurrentFrameworkAgreement { IsActive = false };

                    _cache.Set(CacheKeys.CurrentFrameworkAgreement, currentFrameworkAgreement.ToByteArray());
                }
                return currentFrameworkAgreement;
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

        public IEnumerable<BrokerFeeByRegionAndServiceType> BrokerFeeByRegionGroupAndServiceTypePriceList
        {
            get
            {
                var brokerFees = _cache.Get(CacheKeys.BrokerFeesByRegionGroupAndServiceType).FromByteArray<IEnumerable<BrokerFeeByRegionAndServiceType>>();

                if (brokerFees == null)
                {
                    brokerFees = _dbContext.BrokerFeeByServiceTypePriceListRows
                        .Join(_dbContext.Regions, 
                            f => f.RegionGroupId,
                            r => r.RegionGroupId,
                            (f, r) => new BrokerFeeByRegionAndServiceType
                    {
                        BrokerFee = f.Price,
                        CompetenceLevel = f.CompetenceLevel,
                        InterpreterLocation = f.InterpreterLocation,
                        RegionId = r.RegionId,
                        StartDate = f.FirstValidDate,
                        EndDate = f.LastValidDate
                    }).ToList();
                    _cache.Set(CacheKeys.BrokerFeesByRegionGroupAndServiceType, brokerFees.ToByteArray());
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
            List<Ranking> ranks = _dbContext.Rankings.ToList();

            List<PriceInformationBrokerFee> priceListBrokerFee = new List<PriceInformationBrokerFee>();
            foreach (var item in prices)
            {
                priceListBrokerFee.AddRange(ranks.Select(r => new PriceInformationBrokerFee
                {
                    BrokerFee = r.BrokerFee,
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
    }
}

