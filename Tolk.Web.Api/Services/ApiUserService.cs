using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.BusinessLogic.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;

namespace Tolk.Web.Api.Services
{
    public class ApiUserService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly TolkOptions _options;
        private readonly IMemoryCache _cache;

        private const string brokerFeesCacheKey = nameof(brokerFeesCacheKey);

        public ApiUserService(TolkDbContext dbContext,
            ILogger<ApiUserService> logger,
            IOptions<TolkOptions> options,
            IMemoryCache cache = null
            )
        {
            _logger = logger;
            _dbContext = dbContext;
            _options = options.Value;
            _cache = cache;
        }

        public AspNetUser GetApiUser(X509Certificate2 clientCertInRequest, string secret)
        {
            if (!string.IsNullOrEmpty(secret))
            {
                _logger.LogInformation("User retrieved using secret");
                //Need a lot more security here
                return _dbContext.Users.SingleOrDefault(u => u.Claims.Any(c => c.ClaimType == "Secret" && c.ClaimValue == secret));
            }
            else
            {
                _logger.LogInformation("User retrieved using certificate");
                return _dbContext.Users.SingleOrDefault(u => u.Claims.Any(c => c.ClaimType == "CertSerialNumber" && c.ClaimValue == clientCertInRequest.SerialNumber));
            }
        }

        public AspNetUser GetBrokerUser(string caller, int? brokerId)
        {
            return !string.IsNullOrWhiteSpace(caller) ?
                _dbContext.Users.SingleOrDefault(u => u.NormalizedEmail == caller.ToUpper() && u.BrokerId == brokerId) :
                null;
        }

        public Interpreter GetInterpreter(string interpreter)
        {
            return !string.IsNullOrWhiteSpace(interpreter) ?
                _dbContext.Users.Include(u => u.Interpreter).SingleOrDefault(u => u.NormalizedEmail == interpreter.ToUpper())?.Interpreter :
                null;
        }
    }
}
