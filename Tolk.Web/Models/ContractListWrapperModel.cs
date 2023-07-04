using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class ContractListWrapperModel
    {
        public BrokerFeeCalculationType ListType { get; set; }
        public ContractListByRegionAndBrokerModel ContractListByRegionAndBrokerModel { get; set; }
        public ContractListByRegionAndServiceModel ContractListByRegionAndServiceModel { get; set; }
        public IEnumerable<FrameworkAgreementNumberIdModel> FrameworkAgreementList { get; set; }

        public static ContractListWrapperModel GetContractListByRegionAndBroker(CurrentOrLatestFrameworkAgreement frameworkAgreement, IEnumerable<PriceInformationBrokerFee> brokerFeePrices, DateTimeOffset swedenNow, List<Ranking> rankings)
        {
            brokerFeePrices = brokerFeePrices.CurrentOrLastActiveBrokerFeesForAgreement(frameworkAgreement, swedenNow.Date);
            var brokers = rankings.Select(r => r.Broker).Distinct().OrderBy(b => b.Name).ToList();
            var regions = rankings.Select(r => r.Region).Distinct().OrderBy(r => r.Name).ToList();
            return new ContractListWrapperModel
            {
                ListType = BrokerFeeCalculationType.ByRegionAndBroker,
                ContractListByRegionAndBrokerModel = new ContractListByRegionAndBrokerModel
                {
                    ItemsPerBroker = brokers.Select(b => new ContractBrokerListItemModel
                    {
                        Broker = b.Name,
                        RegionRankings = rankings.Where(r => r.BrokerId == b.BrokerId)
                    .OrderBy(r => r.Region.Name)
                        .Select(ra => new BrokerFeeByRankingModel
                        {
                            RegionName = ra.Region.Name,
                            BrokerFeePercentage = ra.BrokerFee.Value,
                            Rank = ra.Rank,
                            BrokerFeesPerCompetenceLevel = brokerFeePrices.Where(p => p.RankingId == ra.RankingId)
                                      .OrderBy(p => p.PriceToUse)
                                .Select(p => p.PriceToUse.ToSwedishString("#,0.00")).ToList(),
                            CompetenceDescriptions = brokerFeePrices.Where(p => p.RankingId == ra.RankingId)
                                      .OrderBy(p => p.CompetenceLevel)
                                .Select(p => p.CompetenceLevel.GetShortDescription()).ToList()
                        }).ToList()
                    }),
                    ItemsPerRegion = regions.Select(r => new ContractRegionListItemModel
                    {
                        Region = r.Name,
                        Brokers = rankings.Where(ra => ra.RegionId == r.RegionId)
                        .OrderBy(ra => ra.Rank).Select(ra => new BrokerFeeByRankingModel
                        {
                            BrokerName = ra.Broker.Name,
                            BrokerFeePercentage = ra.BrokerFee.Value,
                            Rank = ra.Rank,
                            BrokerFeesPerCompetenceLevel = brokerFeePrices.Where(p => p.RankingId == ra.RankingId)
                                    .Select(p => p.PriceToUse.ToSwedishString("#,0.00")).ToList(),
                            CompetenceDescriptions = brokerFeePrices.Where(p => p.RankingId == ra.RankingId)
                                          .OrderBy(p => p.CompetenceLevel)
                                    .Select(p => p.CompetenceLevel.GetShortDescription()).ToList()
                        }).ToList()
                    }),
                    ConnectedFrameworkAgreement = frameworkAgreement
                }
            };
        }

        public static ContractListWrapperModel CreateWrapperModelByRegionAndServiceType(CurrentOrLatestFrameworkAgreement frameworkAgreement, IEnumerable<BrokerFeeByRegionAndServiceType> brokerFeePrices, DateTimeOffset swedenNow, List<Ranking> rankings)
        {
            var distanceBrokerFeePrices = brokerFeePrices.CurrentOrLastActiveDistanceBrokerFeesForAgreement(frameworkAgreement, swedenNow.Date);
            var onSiteBrokerFeePrices = brokerFeePrices.CurrentOrLastActiveOnSiteBrokerFeesForAgreement(frameworkAgreement, swedenNow.Date);
            var brokers = rankings.Select(r => r.Broker).Distinct().OrderBy(b => b.Name).ToList();
            var regions = rankings.Select(r => r.Region).Distinct().OrderBy(r => r.Name).ToList();

            return new ContractListWrapperModel
            {

                ListType = BrokerFeeCalculationType.ByRegionAndServiceType,
                ContractListByRegionAndServiceModel = new ContractListByRegionAndServiceModel
                {
                    ConnectedFrameworkAgreement = frameworkAgreement,
                 
                    ItemsPerBroker = brokers.Select(b => new ContractBrokerListItemModel
                    {
                        Broker = b.Name,
                        RegionRankings = rankings.Where(r => r.BrokerId == b.BrokerId)
                            .OrderBy(r => r.Region.Name)
                            .Select(ra => new BrokerFeePerServiceTypeModel
                            {
                                RegionName = ra.Region.Name,
                                Rank = ra.Rank,
                                DistanceBrokerFeesPerCompetence = distanceBrokerFeePrices.Where(p => p.RegionId == ra.RegionId)
                                    .OrderBy(p => p.CompetenceLevel)
                                    .GroupBy(p => new { p.BrokerFee, p.CompetenceLevel })
                                    .Select(g => new ContractBrokerFeeCompetenceItemModel(g.First())).ToList(),
                                OnSiteBrokerFeesPerCompetence = onSiteBrokerFeePrices.Where(p => p.RegionId == ra.RegionId)
                                    .OrderBy(p => p.CompetenceLevel)
                                    .GroupBy(p => new { p.BrokerFee, p.CompetenceLevel })
                                    .Select(g => new ContractBrokerFeeCompetenceItemModel(g.First())).ToList(),       
                            }).ToList()
                    }),
                    ItemsPerRegion = regions.Select(r => new ContractRegionListItemModel
                    {
                        Region = r.Name,                                          
                        Brokers = rankings.Where(ra => ra.RegionId == r.RegionId)
                          .OrderBy(ra => ra.Rank)
                          .Select(ra => new BrokerFeePerServiceTypeModel
                          {
                              BrokerName = ra.Broker.Name,
                              Rank = ra.Rank,
                              DistanceBrokerFeesPerCompetence = distanceBrokerFeePrices.Where(p => p.RegionId == r.RegionId)
                                    .GroupBy(p => new { p.BrokerFee, p.CompetenceLevel })
                                    .Select(g => new ContractBrokerFeeCompetenceItemModel(g.First())).ToList(),                                
                              OnSiteBrokerFeesPerCompetence = onSiteBrokerFeePrices.Where(p => p.RegionId == r.RegionId)
                                    .GroupBy(p => new { p.BrokerFee, p.CompetenceLevel })
                                    .Select(g => new ContractBrokerFeeCompetenceItemModel(g.First())).ToList(),                                   
                          }).ToList()
                    })
                }
            };
        }
    }
}