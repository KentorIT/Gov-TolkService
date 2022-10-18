using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic;
using Tolk.BusinessLogic.Data;
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
            return View();
        }

        [Authorize(Roles = Roles.AppOrSysAdmin)]
        public async Task<IActionResult> List()
        {
            var brokerFeePrices = _cacheService.BrokerFeeByRegionAndBrokerPriceList;

            var rankings = await _dbContext.Rankings.GetActiveRankings(_clock.SwedenNow.DateTime).ToListAsync();
            var brokers = rankings.Select(r => r.Broker).Distinct().OrderBy(b => b.Name).ToList();
            var regions = rankings.Select(r => r.Region).Distinct().OrderBy(r => r.Name).ToList();

            return View(new ContractListModel
            {
                ItemsPerBroker = brokers.Select(b => new ContractBrokerListItemModel
                {
                    Broker = b.Name,
                    RegionRankings = rankings.Where(r => r.BrokerId == b.BrokerId)
                    .OrderBy(r => r.Region.Name)
                        .Select(ra => new BrokerRankModel
                        {
                            RegionName = ra.Region.Name,
                            BrokerFeePercentage = ra.BrokerFee,
                            Rank = ra.Rank,
                            BrokerFeesPerCompetenceLevel = brokerFeePrices.Where(p => p.RankingId == ra.RankingId &&
                                p.StartDate <= _clock.SwedenNow && p.EndDate > _clock.SwedenNow).OrderBy(p => p.PriceToUse)
                                .Select(p => p.PriceToUse.ToSwedishString("#,0.00")).ToList(),
                            CompetenceDescriptions = brokerFeePrices.Where(p => p.RankingId == ra.RankingId &&
                               p.StartDate <= _clock.SwedenNow && p.EndDate > _clock.SwedenNow).OrderBy(p => p.CompetenceLevel)
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
                        BrokerFeePercentage = ra.BrokerFee,
                        Rank = ra.Rank,
                        BrokerFeesPerCompetenceLevel = brokerFeePrices.Where(p => p.RankingId == ra.RankingId &&
                                p.StartDate <= _clock.SwedenNow && p.EndDate > _clock.SwedenNow).OrderBy(p => p.CompetenceLevel)
                                .Select(p => p.PriceToUse.ToSwedishString("#,0.00")).ToList(),
                        CompetenceDescriptions = brokerFeePrices.Where(p => p.RankingId == ra.RankingId &&
                                p.StartDate <= _clock.SwedenNow && p.EndDate > _clock.SwedenNow).OrderBy(p => p.CompetenceLevel)
                                .Select(p => p.CompetenceLevel.GetShortDescription()).ToList()
                    }).ToList()
                }),
                ContractNumber = Constants.ContractNumber
            });
        }
    }
}
