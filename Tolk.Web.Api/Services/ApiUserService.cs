using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Enums;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Api.Exceptions;
using Tolk.Web.Api.Helpers;

namespace Tolk.Web.Api.Services
{
    public class ApiUserService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly InterpreterService _interpreterService;

        private const string brokerFeesCacheKey = nameof(brokerFeesCacheKey);

        public ApiUserService(TolkDbContext dbContext,
            ILogger<ApiUserService> logger,
            InterpreterService interpreterService
            )
        {
            _logger = logger;
            _dbContext = dbContext;
            _interpreterService = interpreterService;
        }

        public async Task<AspNetUser> GetApiUser(X509Certificate2 clientCertInRequest, string userName, string key)
        {
            //First check by cert, then by unamne/key
            return await GetApiUserByCertificate(clientCertInRequest) ??
                await GetApiUserByApiKey(userName, key) ?? 
                throw new InvalidApiCallException(ErrorCodes.Unauthorized);
        }

        public async Task<AspNetUser> GetApiUserByCertificate(X509Certificate2 clientCertInRequest)
        {
            if (clientCertInRequest == null)
            {
                return null;
            }
            _logger.LogInformation("User retrieved using certificate");
            return await _dbContext.Users.SingleOrDefaultAsync(u =>
                u.Claims.Any(c => c.ClaimType == "UseCertificateAuthentication") &&
                u.Claims.Any(c => c.ClaimType == "CertificateSerialNumber" && c.ClaimValue == clientCertInRequest.SerialNumber));
        }

        public async Task<AspNetUser> GetApiUserByApiKey(string userName, string key)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(key))
            {
                return null;
            }
            _logger.LogInformation("User retrieved using user/apiKey");
            //Need a lot more security here
            var user = await _dbContext.Users
                .Include(u => u.Claims)
                .SingleOrDefaultAsync(u =>
                u.NormalizedUserName == userName.ToSwedishUpper() &&
                u.Claims.Any(c => c.ClaimType == "UseApiKeyAuthentication"));
            var secret = user?.Claims.SingleOrDefault(c => c.ClaimType == "Secret")?.ClaimValue;
            var salt = user?.Claims.SingleOrDefault(c => c.ClaimType == "Salt")?.ClaimValue;
            if (secret != null && salt != null && HashHelper.AreEqual(key, secret, salt))
            {
                return user;
            }
            return null;
        }

        public async Task<AspNetUser> GetBrokerUser(string caller, int? brokerId)
        {
            return !string.IsNullOrWhiteSpace(caller) ?
                await _dbContext.Users.SingleOrDefaultAsync(u => u.NormalizedEmail == caller.ToSwedishUpper() && u.BrokerId == brokerId) :
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
                    //check if unique officialInterpreterId for broker 
                    if (_interpreterService.IsUniqueOfficialInterpreterId(interpreterModel.OfficialInterpreterId, brokerId))
                    {
                        //Create the new interpreter, connected to the provided broker
                        return new InterpreterBroker(
                            interpreterModel.FirstName,
                            interpreterModel.LastName,
                            brokerId,
                            interpreterModel.Email,
                            interpreterModel.PhoneNumber,
                            interpreterModel.OfficialInterpreterId
                        );
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
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
