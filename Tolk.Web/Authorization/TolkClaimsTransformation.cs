using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Models.TwoFactor;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Helpers;

namespace Tolk.Web.Authorization
{
    public class TolkClaimsTransformation : IClaimsTransformation
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserService _userService;
        private readonly TolkOptions _options;
        public TolkClaimsTransformation(IHttpContextAccessor haccess, UserService userService, IOptions<TolkOptions> options)
        {
            _httpContextAccessor = haccess;
            _userService = userService;
            _options = options.Value;
        }
        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.Identity.IsAuthenticated)
            {
                if (!_options.TwoFactor.Enabled)
                {
                    AddTwoFactorStateClaim(principal, TwoFactorState.Confirmed);
                    return principal;
                }
                var userName = principal.Identity.Name;
                var cookieName = $"TwoFactor|{userName}";
                var confirmedCookieName = $"Confirmed_{cookieName}";
                var twoFactorCookie = _httpContextAccessor.HttpContext.Request.Cookies[cookieName];
                if (twoFactorCookie != null)
                {
                    var twoFactorConfirmCookie = _httpContextAccessor.HttpContext.Request.Cookies[confirmedCookieName];
                    if (twoFactorConfirmCookie != null && _userService.ValidateConfirmHash(twoFactorCookie, twoFactorConfirmCookie))
                    {
                        AddTwoFactorStateClaim(principal, TwoFactorState.Confirmed);
                        return principal;
                    }
                }

                (string id, TwoFactorState state) = await _userService.GetCurrentTwoFactorClaim(userName, twoFactorCookie);
                switch (state)
                {
                    case TwoFactorState.NeedTwoFactorEmail:
                        await _userService.InitiateTwoFactorValidation(userName, id);
                        break;
                    case TwoFactorState.Confirmed:
                        //Need to set a temporary cookie that states twofactor confirmed for this session.
                        // it gets too heavy otherwize.
                        _httpContextAccessor.HttpContext.Response.Cookies.Append(confirmedCookieName, _userService.GenerateConfirmHash(twoFactorCookie), new CookieOptions
                        {
                            Expires = DateTime.UtcNow.AddMinutes(10),
                            IsEssential = true
                        });
                        break;
                    default:
                        break;
                }
                if (string.IsNullOrEmpty(twoFactorCookie))
                {
                    //Save a cookie with the guid that connect this device to the current user.
                    _httpContextAccessor.HttpContext.Response.Cookies.Append(cookieName, id, new CookieOptions
                    {

                        Expires = DateTime.UtcNow.AddYears(1),
                        IsEssential = true
                    });
                }
                AddTwoFactorStateClaim(principal, state);
            }
            return principal;
        }

        private static void AddTwoFactorStateClaim(ClaimsPrincipal principal, TwoFactorState state)
        {
            if (!principal.HasClaim(claim => claim.Type == TolkClaimTypes.TwoFactorState))
            {
                ClaimsIdentity claimsIdentity = new();
                claimsIdentity.AddClaim(new Claim(TolkClaimTypes.TwoFactorState, state.ToString()));
                principal.AddIdentity(claimsIdentity);
            }
        }
    }
}
