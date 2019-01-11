﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Transactions;
using Tolk.BusinessLogic;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
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
        private readonly IdentityErrorDescriber _identityErrorDescriber;
        private readonly LoginLinkTokenProvider _loginLinkTokenProvider;
        private readonly NotificationService  _notificationService;

        public AccountController(
            UserManager<AspNetUser> userManager,
            SignInManager<AspNetUser> signInManager,
            RoleManager<IdentityRole<int>> roleManager,
            ILogger<AccountController> logger,
            TolkDbContext dbContext,
            IUserClaimsPrincipalFactory<AspNetUser> claimsFactory,
            UserService userService,
            IOptions<TolkOptions> options,
            ISwedishClock clock,
            IdentityErrorDescriber identityErrorDescriber,
            LoginLinkTokenProvider loginLinkTokenProvider,
            NotificationService notificationService) 
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
            _identityErrorDescriber = identityErrorDescriber;
            _loginLinkTokenProvider = loginLinkTokenProvider;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var model = new AccountViewModel
            {
                NameFull = user.FullName ?? "-",
                Email = user.Email ?? "-",
                PhoneWork = user.PhoneNumber ?? "-",
                PhoneCellphone = user.PhoneNumberCellphone ?? "-",
            };

            return View(model);
        }

        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            var hasPassword = await _userManager.HasPasswordAsync(user);

            var model = new ManageModel
            {
                Email = user.Email,
                HasPassword = hasPassword,
                NameFirst = user.NameFirst,
                NameFamily = user.NameFamily,
                PhoneWork = user.PhoneNumber,
                PhoneCellphone = user.PhoneNumberCellphone,
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ManageModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var hasPassword = await _userManager.HasPasswordAsync(user);

            if (!hasPassword)
            {
                return await SendPasswordResetLink(user);
            }

            if (ModelState.IsValid)
            {
                using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (user != null)
                    {
                        // Check if user is authorized to change account
                        if (!await _userManager.CheckPasswordAsync(user, model.CurrentPassword))
                        {
                            ModelState.AddModelError(nameof(model.CurrentPassword), "Lösenordet som angivits är felaktigt.");
                            return View(model);
                        }
                        user.NameFirst = model.NameFirst;
                        user.NameFamily = model.NameFamily;
                        user.PhoneNumber = model.PhoneWork;
                        user.PhoneNumberCellphone = model.PhoneCellphone;

                        var result = await _userManager.UpdateAsync(user);
                        if (result.Succeeded)
                        {
                            _logger.LogInformation("Successfully created new user {userId}", user.Id);
                            //when user is updated refresh sign in to get possible updated claims
                            if (!User.IsImpersonated())
                            {
                                await _signInManager.RefreshSignInAsync(user);
                            }
                            transaction.Complete();
                            return RedirectToAction(nameof(Index));
                        }
                    }
                    else
                    {
                        throw new ApplicationException($"Can't find user {user.Id}");
                    }
                }
            }
            return View(model);
        }

        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);

            var model = new ChangePasswordModel
            {
                HasPassword = await _userManager.HasPasswordAsync(user)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
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
                return View(model);
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
            
            var bodyPlain =
$@"Återställning av lösenord för {Constants.SystemName}

Om du har begärt att lösenordet ska återställas för '{user.FullName}' klicka eller klistra in länken nedan i webbläsaren.

{resetLink}

{(user.IsActive ? string.Empty : @"Notera att din användare är inaktiverad. 
Du kommer fortfarande få byta lösenord, men du behöver kontakta din lokala administratör för att få användaren aktiverad.")}
Om du inte har begärt en återställning av ditt lösenord kan du radera det här
meddelandet. Om du får flera meddelanden som du inte har begärt, kontakta
supporten på {_options.SupportEmail}.

{NotificationService.NoReplyText}";

            var bodyHtml =
$@"<h2>Återställning av lösenord för {Constants.SystemName}</h2>

<div>Om du har begärt att lösenordet ska återställas för '{user.FullName}' klicka eller klistra in länken nedan i webbläsaren.</div>

<div>{HtmlHelper.GetButtonDefaultLargeTag(resetLink, "Återställ lösenord")}</div>

<div>{(user.IsActive ? string.Empty : @"Notera att din användare är inaktiverad. 
Du kommer fortfarande få byta lösenord, men du behöver kontakta din lokala administratör för att få användaren aktiverad.")}
Om du inte har begärt en återställning av ditt lösenord kan du radera det här
meddelandet. Om du får flera meddelanden som du inte har begärt, kontakta
supporten på {_options.SupportEmail}.</div>

<div>{NotificationService.NoReplyText}</div>";

            _notificationService.CreateEmail(
                user.Email,
                $"Återställning lösenord {Constants.SystemName}",
                bodyPlain,
                bodyHtml);
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
                return await PasswordLogin(model, returnUrl);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        private async Task<IActionResult> PasswordLogin(LoginViewModel model, string returnUrl)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: true, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(model.Email);
                if (!user.IsActive)
                {
                    //I want this to be done in two steps, first validating the user, then if valid user but inactive log out again, with proper message.
                    await _signInManager.SignOutAsync();
                    _logger.LogInformation("Inactivated User {userName} tried to log in.", model.Email);
                    ModelState.AddModelError(string.Empty, "Användaren är inaktiverad. Kontakta din lokala administratör för mer information.");
                    return View(model);
                }
                user.LastLoginAt = _clock.SwedenNow;
                await _userManager.UpdateAsync(user);
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
                ModelState.AddModelError(string.Empty, "Felaktigt användarnamn eller lösenord.");
                return View(model);
            }
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
                ModelState.AddModelError(string.Empty, _identityErrorDescriber.InvalidToken().Description);
            }
            else
            {
                var result = await _userManager.ResetPasswordAsync(user, model.Code, model.NewPassword);
                if (result.Succeeded)
                {
                    if (!User.Identity.IsAuthenticated && user.IsActive)
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
                using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (_dbContext.IsUserStoreInitialized)
                    {
                        _logger.LogWarning("Tried to CreateInitialUser even though users/roles exist in the database.");
                        ModelState.AddModelError("", "Det finns redan användare/roller i databasen, den här operationen är inte tillgänglig.");
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

                            result = await _userManager.AddToRolesAsync(user, new[] { Roles.Admin, Roles.Impersonator });
                            if (result.Succeeded)
                            {
                                _logger.LogInformation("Added {0} to Admin and Impersonator roles", user.UserName);
                                transaction.Complete();
                                return RedirectToAction("Index", "Home");
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
            if (user.EmailConfirmed)
            {
                return View("ConfirmAccountAlreadyDone");
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);

            if (result.Succeeded)
            {
                _logger.LogInformation("Confirmed e-mail for user {userId} ({email})", userId, user.Email);
                // Resetting the security stamp invalidates the code so operation cannot be redone.
                await _userManager.UpdateSecurityStampAsync(user);
                await _signInManager.SignInAsync(user, true);
                var pToken = await _userManager.GeneratePasswordResetTokenAsync(user);

                return RedirectToAction(nameof(RegisterNewAccount), new { userId, pToken });
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
        public IActionResult Register()
        {
            return View();
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
                        var user = new AspNetUser(model.Email, organization)
                        {
                            IsActive = true
                        };
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

                    var broker = await _dbContext.Brokers
                        .SingleOrDefaultAsync(b => b.EmailDomain == domain);

                    if (broker != null)
                    {
                        var user = new AspNetUser(model.Email, broker)
                        {
                            IsActive = true
                        };
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

        [HttpGet]
        public async Task<IActionResult> RegisterNewAccount(string userId, string pToken)
        {
            //Get the user, and fill the model with the data that already exists...
            var user = await _userManager.FindByIdAsync(userId);
            return View(new RegisterNewAccountViewModel
            {
                UserId = userId,
                PasswordToken = pToken,
                NameFirst = user.NameFirst,
                NameFamily = user.NameFamily,
                PhoneCellphone = user.PhoneNumberCellphone,
                PhoneWork = user.PhoneNumber, 
            });
        }

        [HttpPost]
        public async Task<IActionResult> RegisterNewAccount(RegisterNewAccountViewModel model)
        {
            if (string.IsNullOrEmpty(model.UserId))
            {
                throw new ApplicationException($"No UserId provided for registration");
            }

            if (ModelState.IsValid)
            {
                using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    var user = await _userManager.FindByIdAsync(model.UserId);

                    if (user != null)
                    {
                        var result = await _userManager.ResetPasswordAsync(user, model.PasswordToken, model.NewPassword);
                        if (result.Succeeded)
                        {
                            // Resetting the security stamp invalidates the password token so operation cannot be redone.
                            await _userManager.UpdateSecurityStampAsync(user);
                            await _signInManager.SignInAsync(user, true);

                            user.NameFirst = model.NameFirst;
                            user.NameFamily = model.NameFamily;
                            user.PhoneNumber = model.PhoneWork;
                            user.PhoneNumberCellphone = model.PhoneCellphone;

                            result = await _userManager.UpdateAsync(user);
                            if (result.Succeeded)
                            {
                                _logger.LogInformation("Successfully created new user {userId}", user.Id);
                                //when user is updated refresh sign in to get possible updated claims
                                if (!User.IsImpersonated())
                                {
                                    await _signInManager.RefreshSignInAsync(user);
                                }
                                transaction.Complete();
                                return View(nameof(RegisterNewAccountConfirmation), model);
                            }
                        }
                    }
                    else
                    {
                        throw new ApplicationException($"Found no user with id {model.UserId}");
                    }
                }
            }

            return View(model);
        }

        public IActionResult RegisterNewAccountConfirmation(RegisterNewAccountViewModel model)
        {
            return View(model);
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
