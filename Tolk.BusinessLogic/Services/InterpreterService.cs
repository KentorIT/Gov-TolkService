using Microsoft.Extensions.Logging;
using System.Linq;
using Tolk.BusinessLogic.Data;

namespace Tolk.BusinessLogic.Services
{
    public class InterpreterService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger _logger;

        public InterpreterService(
            TolkDbContext dbContext,
            ILogger<UserService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public bool IsUniqueOfficialInterpreterId(string officialInterpreterId, int brokerId, int? interpreterBrokerId = null)
        {
            return string.IsNullOrWhiteSpace(officialInterpreterId) ? true : !_dbContext.InterpreterBrokers.Any(i => i.BrokerId == brokerId && i.OfficialInterpreterId.ToUpper() == officialInterpreterId.ToUpper() && i.InterpreterBrokerId != interpreterBrokerId);
        }
    }
}
