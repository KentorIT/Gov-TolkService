using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Api.Services;

namespace Tolk.Web.Api.Authorization
{
    public class CustomAuthHandler : AuthenticationHandler<CustomAuthOptions>
    {
        public const string SchemeName = "Custom Scheme";
        private readonly ApiUserService _apiUserService;
        public CustomAuthHandler(IOptionsMonitor<CustomAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, ApiUserService apiUserService)
            : base(options, logger, encoder, clock)
        {
            _apiUserService = apiUserService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-UserName", out var userName);
            Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-ApiKey", out var key);
            var user = await _apiUserService.GetApiUser(Request.HttpContext.Connection.ClientCertificate, userName, key);
            if (user != null && user.IsActive && user.IsApiUser)
            {
                var claims = new List<Claim> {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToSwedishString())
                };
                if (user.BrokerId.HasValue)
                {
                    claims.Add(new Claim(TolkClaimTypes.BrokerId, user.BrokerId.ToString()));
                }
                if (user.CustomerOrganisationId.HasValue)
                {
                    claims.Add(new Claim(TolkClaimTypes.CustomerOrganisationId, user.CustomerOrganisationId.ToString()));
                }
                var identity = new ClaimsIdentity(claims, SchemeName);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, SchemeName);
                return AuthenticateResult.Success(ticket);
            }
            return AuthenticateResult.NoResult();
        }
    }
}
