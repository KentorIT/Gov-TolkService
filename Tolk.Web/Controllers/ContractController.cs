using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.BusinessLogic.Data;
using Tolk.Web.Authorization;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    public class ContractController : Controller
    {
        private const string ContractNumber = "23.3-9066-16";
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly PriceCalculationService _priceCalculationService;

        public ContractController(
            TolkDbContext dbContext,
            ISwedishClock clock,
            PriceCalculationService priceCalculationService)
        {
            _dbContext = dbContext;
            _clock = clock;
            _priceCalculationService = priceCalculationService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = Roles.AppOrSysAdmin)]
        public IActionResult List()
        {
            var brokerFeePrices = _priceCalculationService.BrokerFeePriceList;
            return View(new ContractListModel
            {
                ItemsPerBroker = _dbContext.Brokers.Include(b => b.Rankings)
                .ThenInclude(r => r.Region)
                .Select(b => new ContractBrokerListItemModel
                {
                    Broker = b.Name,
                    RegionRankings = b.Rankings.Where(ra => ra.FirstValidDate <= _clock.SwedenNow && ra.LastValidDate > _clock.SwedenNow)
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
                ItemsPerRegion = _dbContext.Regions.Include(b => b.Rankings)
                .ThenInclude(r => r.Broker)
                .OrderBy(r => r.Name)
                .Select(r => new ContractRegionListItemModel
                {
                    Region = r.Name,
                    Brokers = r.Rankings.Where(ra => ra.FirstValidDate <= _clock.SwedenNow && ra.LastValidDate > _clock.SwedenNow)
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
                ContractNumber = ContractNumber
            });
        }
    }
}
