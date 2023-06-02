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
        public ContractListByRegionGroupAndServiceModel ContractListByRegionGroupAndServiceModel { get; set; }
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
                        .Select(ra => new BrokerRankModel
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
                        .OrderBy(ra => ra.Rank).Select(ra => new BrokerRankModel
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

        public static ContractListWrapperModel CreateWrapperModelByRegionGroupAndServiceType(CurrentOrLatestFrameworkAgreement frameworkAgreement, IEnumerable<BrokerFeeByRegionAndServiceType> brokerFeePrices, DateTimeOffset swedenNow, List<Ranking> rankings) 
        {
            var distanceBrokerFeePrices = brokerFeePrices.CurrentOrLastActiveDistanceBrokerFeesForAgreement(frameworkAgreement, swedenNow.Date);
            var onSiteBrokerFeePrices = brokerFeePrices.CurrentOrLastActiveOnSiteBrokerFeesForAgreement(frameworkAgreement, swedenNow.Date);            
            var brokers = rankings.Select(r => r.Broker).Distinct().OrderBy(b => b.Name).ToList();
            var regions = rankings.Select(r => r.Region).Distinct().OrderBy(r => r.Name).ToList();
            return new ContractListWrapperModel
            {

                ListType = BrokerFeeCalculationType.ByRegionGroupAndServiceType,
                ContractListByRegionGroupAndServiceModel = new ContractListByRegionGroupAndServiceModel
                {
                    ConnectedFrameworkAgreement = frameworkAgreement,

                    ItemsDistanceInterpretationPerCompetence = distanceBrokerFeePrices
                        .GroupBy(p => new { p.BrokerFee, p.CompetenceLevel })
                        .Select(g => new ContractBrokerFeeCompetenceItemModel
                        {
                            CompetenceDescription = g.First().CompetenceLevel.GetShortDescription(),
                            BrokerFee = g.First().BrokerFee.ToSwedishString("#,0.00")
                        }).ToList(),

                    ItemsPerRegionGroup = regions.GroupBy(r => r.RegionGroupId).Select(r =>
                        new ContractPricePerRegionGroupListItemModel
                        {
                            RegionGroupName = r.First().RegionGroup.Name,
                            BrokerFeePerCompentence = onSiteBrokerFeePrices
                                .Where(p => p.RegionId == r.First().RegionId)
                                .GroupBy(p => new { p.BrokerFee, p.CompetenceLevel })
                                .OrderBy(g => g.First().CompetenceLevel)
                                .Select(g => new ContractBrokerFeeCompetenceItemModel
                                {
                                    CompetenceDescription = g.First().CompetenceLevel.GetShortDescription(),
                                    BrokerFee = g.First().BrokerFee.ToSwedishString("#,0.00")
                                }).ToList()
                        }).OrderBy(cp => cp.RegionGroupName).ToList(),


                    ItemsPerBroker = brokers.Select(b => new ContractBrokerListItemModel
                    {
                        Broker = b.Name,
                        RegionRankings = rankings.Where(r => r.BrokerId == b.BrokerId)
                            .OrderBy(r => r.Region.Name)
                            .Select(ra => new BrokerRankModel
                            {
                                RegionName = ra.Region.Name,
                                Rank = ra.Rank,
                                RegionGroup = ra.Region.RegionGroup.Name
                            }).ToList()
                    }),
                    ItemsPerRegion = regions.Select(r => new ContractRegionListItemModel
                    {
                        Region = r.Name,
                        RegionGroup = r.RegionGroup.Name,
                        Brokers = rankings.Where(ra => ra.RegionId == r.RegionId)
                          .OrderBy(ra => ra.Rank)
                          .Select(ra => new BrokerRankModel
                          {
                              BrokerName = ra.Broker.Name,
                              Rank = ra.Rank,
                          }).ToList()
                    }),
                }
            };
        }
    }
}