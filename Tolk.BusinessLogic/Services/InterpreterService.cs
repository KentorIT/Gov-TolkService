using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;

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

        public bool IsUniqueOfficialInterpreterId(string officialInterpreterId, int brokerId)
        {
            return !_dbContext.InterpreterBrokers.Any(i => i.BrokerId == brokerId && i.OfficialInterpreterId.ToUpper() == officialInterpreterId.ToUpper());
        }
    }
}
