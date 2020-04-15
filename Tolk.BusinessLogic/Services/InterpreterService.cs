using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Helpers;
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

        public async Task<InterpreterBroker> GetInterpreter(int interpreterId, InterpreterInformation interpreterInformation, int brokerId)
        {
            NullCheckHelper.ArgumentCheckNull(interpreterInformation, nameof(GetInterpreter), nameof(OrderService));
            if (interpreterId == Constants.NewInterpreterId)
            {
                if (!IsUniqueOfficialInterpreterId(interpreterInformation.OfficialInterpreterId, brokerId))
                {
                    throw new ArgumentException("Er förmedling har redan registrerat en tolk med detta tolknummer (Kammarkollegiets) i tjänsten.", nameof(interpreterId));
                }
                var interpreter = new InterpreterBroker(
                    interpreterInformation.FirstName,
                    interpreterInformation.LastName,
                    brokerId,
                    interpreterInformation.Email,
                    interpreterInformation.PhoneNumber,
                    interpreterInformation.OfficialInterpreterId
                );
                await _dbContext.AddAsync(interpreter);
                return interpreter;
            }
#warning move include
            return await _dbContext.InterpreterBrokers.Include(ib => ib.Interpreter).SingleAsync(i => i.InterpreterBrokerId == interpreterId);
        }

    }
}
