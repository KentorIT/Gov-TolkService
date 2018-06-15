using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tolk.BusinessLogic;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;
using Tolk.Web.Models.AccountViewModels;
using Tolk.Web.Services;

namespace Tolk.Web.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<AspNetUser> _userManager;
        private readonly SignInManager<AspNetUser> _signInManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly ILogger _logger;
        private readonly TolkDbContext _dbContext;
        private readonly IUserClaimsPrincipalFactory<AspNetUser> _claimsFactory;
        private readonly UserService _userService;
        private readonly TolkOptions _options;
        private readonly ISwedishClock _clock;

        public AccountController(
            UserManager<AspNetUser> userManager,
            SignInManager<AspNetUser> signInManager,
            RoleManager<IdentityRole<int>> roleManager,
            ILogger<AccountController> logger,
            TolkDbContext dbContext,
            IUserClaimsPrincipalFactory<AspNetUser> claimsFactory,
            UserService userService,
            IOptions<TolkOptions> options,
            ISwedishClock clock)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
            _dbContext = dbContext;
            _claimsFactory = claimsFactory;
            _userService = userService;
            _options = options.Value;
            _clock = clock;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var model = new ManageModel
            {
                HasPassword = await _userManager.HasPasswordAsync(user)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ManageModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var hasPassword = await _userManager.HasPasswordAsync(user);

            if (hasPassword)
            {
                // Check here for modelstate, if no password - there are no password entries.
                if (ModelState.IsValid)
                {
                    var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

                    if (result.Succeeded)
                    {
                        return RedirectToAction(nameof(ResetPasswordConfirmation));
                    }
                    AddErrors(result);
                }
                return View();
            }

            return await SendPasswordResetLink(user);
        }

        // Common logic for setting of password and forgot password flows.
        private async Task<IActionResult> SendPasswordResetLink(AspNetUser user)
        {
            if (!(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Here we'll just throw. Calling action might check this as well and do better error handling.
                throw new InvalidOperationException($"Cannot send password reset to user {user.Id}/{user.Email} because email is not confirmed.");
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.ResetPasswordCallbackLink(user.Id.ToString(), code);

            var body =
$@"Hej!

För att återställa ditt lösenord i {Constants.SystemName}, använd följande länk.

{resetLink}

Om du inte har begärt en återställning av ditt lösenord kan du radera det här
meddelandet. Om du får flera meddelanden som du inte har begärt, kontakta
supporten på {_options.SupportEmail}";

            _dbContext.Add(new OutboundEmail(
                user.Email,
                $"Återställning lösenord {Constants.SystemName}",
                body,
                _clock.SwedenNow));
            _dbContext.SaveChanges();

            _logger.LogInformation("Password reset link sent to {email} for {userId}",
                user.Email, user.Id);

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: true, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {userName} logged in.", model.Email);
                    return RedirectToLocal(returnUrl);
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account {userName} locked out.", model.Email);
                    return RedirectToAction(nameof(Lockout));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            _logger.LogDebug("Requesting password reset for {email}", model.Email);
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    _logger.LogInformation("Tried to reset password for {email}, but found no such user.",
                        model.Email);
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }
                if (!(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    _logger.LogInformation("Cannot reset password for {email}/{userId}, because email is not verified.",
                        user.Email, user.Id);
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                return await SendPasswordResetLink(user);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string userId, string code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
            {
                throw new ApplicationException($"Account confirmation failed for {userId} with code {code}");
            }

            var model = new ResetPasswordViewModel { Code = code };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                // Lie a bit to not reveal difference between incorrect user id and
                // incorrect/missing token, to avoid a user enumeration issue.
                ModelState.AddModelError(string.Empty, "Invalid token.");
            }
            else
            {
                var result = await _userManager.ResetPasswordAsync(user, model.Code, model.NewPassword);
                if (result.Succeeded)
                {
                    if (!User.Identity.IsAuthenticated)
                    {
                        await _signInManager.SignInAsync(user, true);
                    }
                    return RedirectToAction(nameof(ResetPasswordConfirmation));
                }
                AddErrors(result);
            }
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }


        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult CreateInitialUser()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInitialUser(CreateInitialUserModel model)
        {
            if (ModelState.IsValid)
            {
                // Explicit transaction to ensure both check and all updates
                // are done atomically. The user and role managers call SaveChanges
                // multiple times internally.
                using (var transaction = _dbContext.Database.BeginTransaction())
                {
                    if (_dbContext.IsUserStoreInitialized)
                    {
                        _logger.LogWarning("Tried to CreateInitialUser even though users/roles exist in the database.");
                        ModelState.AddModelError("", "Det finns redan anävndare/roller i databasen, den här operationen är inte tillgänglig.");
                    }
                    else
                    {
                        var user = new AspNetUser(model.Email)
                        {
                            EmailConfirmed = true
                        };

                        var result = await _userManager.CreateAsync(user, model.NewPassword);
                        if (result.Succeeded)
                        {
                            _logger.LogInformation("Created initial user account {0}", user.UserName);

                            // Gör array av roller, loopa över, kolla result för varje.

                            var roles = new IdentityRole<int>[]
                            {
                                new IdentityRole<int>(Roles.Admin),
                                new IdentityRole<int>(Roles.Impersonator),
                            };

                            foreach (var role in roles)
                            {
                                result = await _roleManager.CreateAsync(role);
                                if (!result.Succeeded)
                                {
                                    break;
                                }
                                _logger.LogInformation("Created role {0}.", role.Name);
                            }

                            if (result.Succeeded)
                            {
                                result = await _userManager.AddToRolesAsync(user, new[] { Roles.Admin, Roles.Impersonator });
                                if (result.Succeeded)
                                {
                                    _logger.LogInformation("Added {0} to Admin and Impersonator roles", user.UserName);
                                    transaction.Commit();
                                    return RedirectToAction("Index", "Home");
                                }
                            }
                        }
                        AddErrors(result);
                    }
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Impersonator)]
        public async Task<IActionResult> Impersonate(ImpersonationViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);

            var newPrincipal = await _claimsFactory.CreateAsync(user);

            if (model.UserId != User.FindFirstValue(TolkClaimTypes.ImpersonatingUserId))
            {
                if (newPrincipal.IsInRole(Roles.Admin))
                {
                    throw new InvalidOperationException("Cannot impersonate an admin user");
                }

                var newIdentity = newPrincipal.Identities.Single();

                ImpersonationHelper.SetupImpersonationClaims(User, newIdentity);
            }

            await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, newPrincipal);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmAccount(string userId, string code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
            {
                throw new ApplicationException($"Account confirmation failed for {userId} with code {code}");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new ApplicationException($"Can't find user {userId}");
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);

            if (result.Succeeded)
            {
                _logger.LogInformation("Confirmed e-mail for user {userId} ({email})", userId, user.Email);
                // Resetting the security stamp invalidates the code so operation cannot be redone.
                await _userManager.UpdateSecurityStampAsync(user);
                await _signInManager.SignInAsync(user, true);
                return RedirectToAction(nameof(ConfirmAccountConfirmation));
            }

            var model = new ConfirmAccountModel { UserId = userId };

            return View("ConfirmAccountFailed", model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmAccount(ConfirmAccountModel model)
        {
            if (!string.IsNullOrEmpty(model.UserId))
            {
                var user = await _userManager.FindByIdAsync(model.UserId);

                if (user != null)
                {
                    _logger.LogInformation("Sent account confirmation link to {userId} ({email})", model.UserId, user.Email);
                    await _userService.SendInviteAsync(user);
                }
                else
                {
                    _logger.LogWarning("An account confirmation link was requested for non-existing user id {userId}", model.UserId);
                }
            }
            return RedirectToAction(nameof(ConfirmAccountLinkSent));
        }

        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (var trn = await _dbContext.Database.BeginTransactionAsync())
                {
                    var domain = model.Email.Split('@')[1];

                    var organization = await _dbContext.CustomerOrganisations
                        .SingleOrDefaultAsync(o => o.EmailDomain == domain);

                    if (organization != null)
                    {
                        var user = new AspNetUser(model.Email, organization);

                        var result = await _userManager.CreateAsync(user);

                        if (result.Succeeded)
                        {
                            await _userService.SendInviteAsync(user);

                            trn.Commit();
                            return RedirectToAction(nameof(ConfirmAccountLinkSent));
                        }
                        AddErrors(result);
                        return View(model);
                    }

                    // TODO: Registration on broker.

                    ModelState.AddModelError(nameof(model.Email),
                        $"Maildomänen {domain} är inte registrerad på någon organisation i tjänsten.");
                }
            }

            return View(model);
        }

        [AllowAnonymous]
        public IActionResult ConfirmAccountLinkSent()
        {
            return View();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        #endregion
    }
}
