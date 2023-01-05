using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    public class ContractController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly CacheService _cacheService;

        public ContractController(
            TolkDbContext dbContext,
            ISwedishClock clock,
            CacheService cacheService)
        {
            _dbContext = dbContext;
            _clock = clock;
            _cacheService = cacheService;
        }

        public IActionResult Index()
        {
            var currentOrLatestFrameworkAgreement = _cacheService.CurrentOrLatestFrameworkAgreement;
            if (currentOrLatestFrameworkAgreement.IsActive && currentOrLatestFrameworkAgreement.FrameworkAgreementResponseRuleset.GetContractDefinitionAttribute() == null)
            {
                return Forbid();
            }
            return View(new DisplayContractModel
            {
                AgreementNumber = currentOrLatestFrameworkAgreement.AgreementNumber,
                Description = currentOrLatestFrameworkAgreement.Description,
                FirstValidDate = currentOrLatestFrameworkAgreement.FirstValidDate,
                OriginalLastValidDate = currentOrLatestFrameworkAgreement.OriginalLastValidDate,
                PossibleAgreementExtensionsInMonths = currentOrLatestFrameworkAgreement.PossibleAgreementExtensionsInMonths,
                ContractDefinition = currentOrLatestFrameworkAgreement.FrameworkAgreementResponseRuleset.GetContractDefinitionAttribute().ContractDefinition,
                IsActive = currentOrLatestFrameworkAgreement.IsActive
            });
        }

        [Authorize(Roles = Roles.AppOrSysAdmin)]
        public async Task<IActionResult> List()
        {
            var currentOrLatestFrameworkAgreement = _cacheService.CurrentOrLatestFrameworkAgreement;
            if (!currentOrLatestFrameworkAgreement.IsActive && currentOrLatestFrameworkAgreement.FrameworkAgreementResponseRuleset.GetContractDefinitionAttribute() == null)
            {
                // No Active FrameworkAgreement, Show previous? or Forbid?
                return Forbid();
            }
            var contractListWrapperModel = new ContractListWrapperModel();
            switch (currentOrLatestFrameworkAgreement.BrokerFeeCalculationType)
            {
                case BrokerFeeCalculationType.ByRegionAndBroker:
                    contractListWrapperModel = await GetContractListByRegionAndBroker(currentOrLatestFrameworkAgreement);
                    break;
                case BrokerFeeCalculationType.ByRegionGroupAndServiceType:
                    contractListWrapperModel = await GetContractListByRegionGroupAndServiceType(currentOrLatestFrameworkAgreement);
                    break;
                default:
                    break;
            }

            return View(contractListWrapperModel);
        }

        private async Task<ContractListWrapperModel> GetContractListByRegionGroupAndServiceType(CurrentOrLatestFrameworkAgreement frameworkAgreement)
        {
            var brokerFeePrices = _cacheService.BrokerFeeByRegionGroupAndServiceTypePriceList;
            var rankings = await _dbContext.Rankings.GetLatestRankingsForFrameworkAgreement(frameworkAgreement.FrameworkAgreementId).ToListAsync();
            var brokers = rankings.Select(r => r.Broker).Distinct().OrderBy(b => b.Name).ToList();
            var regions = rankings.Select(r => r.Region).Distinct().OrderBy(r => r.Name).ToList();
            return new ContractListWrapperModel
            {
                ListType = BrokerFeeCalculationType.ByRegionGroupAndServiceType,
                ContractListByRegionGroupAndServiceModel = new ContractListByRegionGroupAndServiceModel
                {
                    ConnectedFrameworkAgreement = frameworkAgreement,

                    ItemsDistanceInterpretationPerCompetence = brokerFeePrices
                        .Where(p => (p.InterpreterLocation == InterpreterLocation.OffSiteVideo || 
                                    p.InterpreterLocation == InterpreterLocation.OffSitePhone) &&
                                    p.StartDate <= _clock.SwedenNow && p.EndDate > _clock.SwedenNow)
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
                            BrokerFeePerCompentence = brokerFeePrices
                                .Where(p => p.RegionId == r.First().RegionId && 
                                       p.InterpreterLocation != InterpreterLocation.OffSiteVideo &&
                                       p.InterpreterLocation != InterpreterLocation.OffSitePhone &&
                                       p.StartDate <= _clock.SwedenNow && p.EndDate > _clock.SwedenNow)
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

        private async Task<ContractListWrapperModel> GetContractListByRegionAndBroker(CurrentOrLatestFrameworkAgreement frameworkAgreement)
        {
            var brokerFeePrices = _cacheService.BrokerFeeByRegionAndBrokerPriceList;
            var rankings = await _dbContext.Rankings.GetLatestRankingsForFrameworkAgreement(frameworkAgreement.FrameworkAgreementId).ToListAsync();
            var brokers = rankings.Select(r => r.Broker).Distinct().OrderBy(b => b.Name).ToList();
            var regions = rankings.Select(r => r.Region).Distinct().OrderBy(r => r.Name).ToList();

            return new ContractListWrapperModel
            {
                ListType = BrokerFeeCalculationType.ByRegionAndBroker,
                ContractListByRegionAndBrokerModel =
              new ContractListByRegionAndBrokerModel
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
                          BrokerFeesPerCompetenceLevel = brokerFeePrices.Where(p => p.RankingId == ra.RankingId &&
                                ((frameworkAgreement.IsActive && p.StartDate <= _clock.SwedenNow && p.EndDate > _clock.SwedenNow) ||
                                (!frameworkAgreement.IsActive && p.EndDatePriceList == frameworkAgreement.LastValidDate)))
                                    .OrderBy(p => p.PriceToUse)
                              .Select(p => p.PriceToUse.ToSwedishString("#,0.00")).ToList(),
                          CompetenceDescriptions = brokerFeePrices.Where(p => p.RankingId == ra.RankingId &&
                                ((frameworkAgreement.IsActive && p.StartDate <= _clock.SwedenNow && p.EndDate > _clock.SwedenNow) ||
                                (!frameworkAgreement.IsActive && p.EndDatePriceList == frameworkAgreement.LastValidDate)))
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
                          BrokerFeesPerCompetenceLevel = brokerFeePrices.Where(p => p.RankingId == ra.RankingId &&
                                    ((frameworkAgreement.IsActive && p.StartDate <= _clock.SwedenNow && p.EndDate > _clock.SwedenNow) ||
                                    (!frameworkAgreement.IsActive && p.EndDatePriceList == frameworkAgreement.LastValidDate)))
                                  .Select(p => p.PriceToUse.ToSwedishString("#,0.00")).ToList(),
                          CompetenceDescriptions = brokerFeePrices.Where(p => p.RankingId == ra.RankingId &&
                                  p.StartDate <= _clock.SwedenNow && p.EndDatePriceList > _clock.SwedenNow).OrderBy(p => p.CompetenceLevel)
                                  .Select(p => p.CompetenceLevel.GetShortDescription()).ToList()
                      }).ToList()
                  }),
                  ConnectedFrameworkAgreement = frameworkAgreement
              }
            };
        }
    }
}
