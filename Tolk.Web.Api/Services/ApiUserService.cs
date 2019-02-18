using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Enums;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Api.Services
{
    public class ApiUserService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly TolkOptions _options;
        private readonly IMemoryCache _cache;
        private readonly HashService _hashService;

        private const string brokerFeesCacheKey = nameof(brokerFeesCacheKey);

        public ApiUserService(TolkDbContext dbContext,
            ILogger<ApiUserService> logger,
            IOptions<TolkOptions> options,
            HashService hashService,
            IMemoryCache cache = null
            )
        {
            _logger = logger;
            _dbContext = dbContext;
            _options = options.Value;
            _cache = cache;
            _hashService = hashService;
        }

        public AspNetUser GetApiUserByCertificate(X509Certificate2 clientCertInRequest)
        {
            if (clientCertInRequest == null)
            {
                return null;
            }
            _logger.LogInformation("User retrieved using certificate");
            return _dbContext.Users.SingleOrDefault(u => 
                u.Claims.Any(c => c.ClaimType == "UseCertificateAuthentication") && 
                u.Claims.Any(c => c.ClaimType == "CertificateSerialNumber" && c.ClaimValue == clientCertInRequest.SerialNumber));
        }

        public AspNetUser GetApiUserByApiKey(string userName, string key)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(key))
            {
                return null;
            }
            _logger.LogInformation("User retrieved using user/apiKey");
            //Need a lot more security here
            var user = _dbContext.Users
                .Include(u => u.Claims)
                .SingleOrDefault(u => 
                u.NormalizedUserName == userName.ToUpper() &&
                u.Claims.Any(c => c.ClaimType == "UseApiKeyAuthentication"));
            var secret = user?.Claims.SingleOrDefault(c => c.ClaimType == "Secret")?.ClaimValue;
            var salt = user?.Claims.SingleOrDefault(c => c.ClaimType == "Salt")?.ClaimValue;
            if (secret != null && salt != null && _hashService.AreEqual(key, secret, salt))
            {
                return user;
            }
            return null;
        }

        public AspNetUser GetBrokerUser(string caller, int? brokerId)
        {
            return !string.IsNullOrWhiteSpace(caller) ?
                _dbContext.Users.SingleOrDefault(u => u.NormalizedEmail == caller.ToUpper() && u.BrokerId == brokerId) :
                null;
        }

        public InterpreterBroker GetInterpreter(InterpreterModel interpreterModel, int brokerId, bool updateInformation = true)
        {
            InterpreterBroker interpreter = null;
            switch (EnumHelper.GetEnumByCustomName<InterpreterInformationType>(interpreterModel.InterpreterInformationType))
            {
                case InterpreterInformationType.ExistingInterpreter:
                    interpreter = _dbContext.InterpreterBrokers
                        .SingleOrDefault(i => i.InterpreterBrokerId == interpreterModel.InterpreterId && i.BrokerId == brokerId);
                    break;
                case InterpreterInformationType.AuthorizedInterpreterId:
                    interpreter = _dbContext.InterpreterBrokers
                        .SingleOrDefault(i => i.OfficialInterpreterId == interpreterModel.OfficialInterpreterId && i.BrokerId == brokerId);
                    break;
                case InterpreterInformationType.NewInterpreter:
                    //Create the new interpreter, connected to the provided broker
                    return new InterpreterBroker(
                        interpreterModel.FirstName,
                        interpreterModel.LastName,
                        brokerId,
                        interpreterModel.Email,
                        interpreterModel.PhoneNumber,
                        interpreterModel.OfficialInterpreterId
                    );
                default:
                    return null;
            }
            if (updateInformation)
            {
                if (string.IsNullOrWhiteSpace(interpreterModel.FirstName))
                {
                    interpreter.FirstName = interpreterModel.FirstName;
                }
                if (string.IsNullOrWhiteSpace(interpreterModel.LastName))
                {
                    interpreter.LastName = interpreterModel.LastName;
                }
                if (string.IsNullOrWhiteSpace(interpreterModel.Email))
                {
                    interpreter.Email = interpreterModel.Email;
                }
                if (string.IsNullOrWhiteSpace(interpreterModel.PhoneNumber))
                {
                    interpreter.PhoneNumber = interpreterModel.PhoneNumber;
                }
                if (string.IsNullOrWhiteSpace(interpreterModel.OfficialInterpreterId))
                {
                    interpreter.OfficialInterpreterId = interpreterModel.OfficialInterpreterId;
                }
            }
            return interpreter;
        }
    }
}
