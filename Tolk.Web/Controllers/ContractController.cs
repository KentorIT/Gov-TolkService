using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    public class ContractController : Controller
    {        
        private readonly ISwedishClock _clock;
        private readonly CacheService _cacheService;
        private readonly ContractService _contractService;

        public ContractController(
            ISwedishClock clock,
            CacheService cacheService,
            ContractService contractService)
        {            
            _clock = clock;
            _cacheService = cacheService;
            _contractService = contractService;
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
                LastValidDate = currentOrLatestFrameworkAgreement.LastValidDate,                
                ContractDefinition = currentOrLatestFrameworkAgreement.FrameworkAgreementResponseRuleset.GetContractDefinitionAttribute().ContractDefinition,
                IsActive = currentOrLatestFrameworkAgreement.IsActive,
                FrameworkAgreementResponseRuleset = currentOrLatestFrameworkAgreement.FrameworkAgreementResponseRuleset
            });
        }

        [Authorize(Roles = Roles.AppOrSysAdmin)]
        public async Task<IActionResult> List(int frameworkAgreementId = -1)
        {
            var currentOrLatestFrameworkAgreement = await GetFrameworkAgreement(frameworkAgreementId);
            var frameworkAgreementList = _cacheService.FrameworkAgreementList;
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
            contractListWrapperModel.FrameworkAgreementList = frameworkAgreementList;
            return View(contractListWrapperModel);
        }

        private async Task<CurrentOrLatestFrameworkAgreement> GetFrameworkAgreement(int frameworkAgreementId)
        {            
            var cachedAgreement = _cacheService.CurrentOrLatestFrameworkAgreement;
            if(cachedAgreement.FrameworkAgreementId == frameworkAgreementId || frameworkAgreementId == -1)
            {
                return cachedAgreement;
            }
            else
            {
                return await _contractService.GetFrameworkAgreementById(frameworkAgreementId);
            }
        }       

        private async Task<ContractListWrapperModel> GetContractListByRegionGroupAndServiceType(CurrentOrLatestFrameworkAgreement frameworkAgreement)
        {
            var brokerFeePrices = _cacheService.BrokerFeeByRegionGroupAndServiceTypePriceList;
            var rankings = await _contractService.GetLatestRankingForFrameworkAgreement(frameworkAgreement);
            return ContractListWrapperModel.CreateWrapperModelByRegionGroupAndServiceType(frameworkAgreement, brokerFeePrices, _clock.SwedenNow, rankings);                     
        }

        private async Task<ContractListWrapperModel> GetContractListByRegionAndBroker(CurrentOrLatestFrameworkAgreement frameworkAgreement)
        {
            var brokerFeePrices = _cacheService.BrokerFeeByRegionAndBrokerPriceList;            
            var rankings = await _contractService.GetLatestRankingForFrameworkAgreement(frameworkAgreement);              
            return ContractListWrapperModel.GetContractListByRegionAndBroker(frameworkAgreement, brokerFeePrices, _clock.SwedenNow, rankings);         
        }
    }
}
