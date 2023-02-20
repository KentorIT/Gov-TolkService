using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class ContractService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ContractService> _logger;

        public ContractService(TolkDbContext dbContext, ISwedishClock clock, INotificationService notificationService, ILogger<ContractService> logger)
        {
            _dbContext = dbContext;
            _clock = clock;
            _notificationService = notificationService;
            _logger = logger;
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

        


    }
}
