using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Enums;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

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

        public AspNetUser GetApiUserByCertificate(X509Certificate2 clientCertInRequest)
        {
            if (clientCertInRequest == null)
            {
                return null;
            }
            _logger.LogInformation("User retrieved using certificate");
            return _dbContext.Users.SingleOrDefault(u => u.Claims.Any(c => c.ClaimType == "CertSerialNumber" && c.ClaimValue == clientCertInRequest.SerialNumber));
        }

        public AspNetUser GetApiUserByApiKey(string userName, string key)
        {
            _logger.LogInformation("User retrieved using use/apiKey");
            //Need a lot more security here
            return _dbContext.Users.SingleOrDefault(u => u.NormalizedUserName == userName.ToUpper() && u.Claims.Any(c => c.ClaimType == "Secret" && c.ClaimValue == key));
        }

        public AspNetUser GetBrokerUser(string caller, int? brokerId)
        {
            return !string.IsNullOrWhiteSpace(caller) ?
                _dbContext.Users.SingleOrDefault(u => u.NormalizedEmail == caller.ToUpper() && u.BrokerId == brokerId) :
                null;
        }

        public InterpreterBroker GetInterpreter(InterpreterModel interpreter, int brokerId)
        {
            switch (EnumHelper.GetEnumByCustomName<InterpreterInformationType>(interpreter.InterpreterInformationType))
            {
                case InterpreterInformationType.ExistingInterpreter:
                    return _dbContext.InterpreterBrokers.SingleOrDefault(i => i.InterpreterBrokerId == interpreter.InterpreterId);
                case InterpreterInformationType.AuthorizedInterpreterId:
                    return _dbContext.InterpreterBrokers.SingleOrDefault(i => i.OfficialInterpreterId == interpreter.OfficialInterpreterId);
                case InterpreterInformationType.NewInterpreter:
                    //Create the new interpreter, connected to the provided broker
                    return null;
                default:
                    return null;
            }
        }
    }
}
