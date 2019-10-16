using Microsoft.Extensions.Logging;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class InterpreterService
    {
        private readonly TolkDbContext _dbContext;

        public InterpreterService(TolkDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public bool IsUniqueOfficialInterpreterId(string officialInterpreterId, int brokerId, int? interpreterBrokerId = null)
        {
            return string.IsNullOrWhiteSpace(officialInterpreterId) ? true : !_dbContext.InterpreterBrokers.Any(i => i.BrokerId == brokerId && i.OfficialInterpreterId.ToSwedishUpper() == officialInterpreterId.ToSwedishUpper() && i.InterpreterBrokerId != interpreterBrokerId);
        }
    }
}
