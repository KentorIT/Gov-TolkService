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

        public CacheService(IDistributedCache cache, TolkDbContext dbContext, ITolkBaseOptions options)
        {
            _cache = cache;
            _dbContext = dbContext;
            _options = options;
        }

        public async Task FlushAll()
        {
            await _cache.RemoveAsync(CacheKeys.BrokerFees);
            await _cache.RemoveAsync(CacheKeys.OrganisationSettings);
            await _cache.RemoveAsync(CacheKeys.Holidays);
            await _cache.RemoveAsync(CacheKeys.CustomerSettings);
        }

        public async Task Flush(string id)
        {
            await _cache.RemoveAsync(id);
        }

        public IEnumerable<PriceInformationBrokerFee> BrokerFeePriceList
        {
            get
            {
                var brokerFees = _cache.Get(CacheKeys.BrokerFees).FromByteArray<IEnumerable<PriceInformationBrokerFee>>();

                if (brokerFees == null)
                {
                    brokerFees = GetBrokerFeePriceList();
                    _cache.Set(CacheKeys.BrokerFees, brokerFees.ToByteArray());
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
                        .Where(u => ( u.CustomerOrganisationId != null) && u.IsApiUser)
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

