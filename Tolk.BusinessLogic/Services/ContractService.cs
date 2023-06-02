using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class ContractService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;        

        public ContractService(TolkDbContext dbContext, ISwedishClock clock)
        {
            _dbContext = dbContext;
            _clock = clock;  
        }

        public async Task<CurrentOrLatestFrameworkAgreement> GetFrameworkAgreementById(int frameworkAgreementId)
        {
            var agreement = await _dbContext.FrameworkAgreements.FirstOrDefaultAsync(fa => fa.FrameworkAgreementId == frameworkAgreementId);
            var now = _clock.SwedenNow;
            return agreement != null ?
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
                           IsActive = agreement.LastValidDate.Date >= now.Date && now.Date >= agreement.FirstValidDate.Date,
                       } :
                       new CurrentOrLatestFrameworkAgreement { IsActive = false };
        }

        public async Task<List<Ranking>> GetLatestRankingForFrameworkAgreement(CurrentOrLatestFrameworkAgreement frameworkAgreement)
        {
            return await _dbContext.Rankings.GetLatestRankingsForFrameworkAgreement(frameworkAgreement.FrameworkAgreementId, frameworkAgreement.IsActive ? _clock.SwedenNow.DateTime : frameworkAgreement.LastValidDate).ToListAsync();
        }


    }
}
