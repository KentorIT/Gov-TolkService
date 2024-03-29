﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Transactions;
using Tolk.BusinessLogic;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;
using Tolk.Web.Models.AccountViewModels;

namespace Tolk.Web.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<AspNetUser> _userManager;
        private readonly SignInManager<AspNetUser> _signInManager;
        private readonly ILogger _logger;
        private readonly TolkDbContext _dbContext;
        private readonly IUserClaimsPrincipalFactory<AspNetUser> _claimsFactory;
        private readonly UserService _userService;
        private readonly TolkOptions _options;
        private readonly ISwedishClock _clock;
        private readonly IdentityErrorDescriber _identityErrorDescriber;
        private readonly INotificationService _notificationService;
        private readonly CacheService _cacheService;

        public AccountController(
            UserManager<AspNetUser> userManager,
            SignInManager<AspNetUser> signInManager,
            ILogger<AccountController> logger,
            TolkDbContext dbContext,
            IUserClaimsPrincipalFactory<AspNetUser> claimsFactory,
            UserService userService,
            IOptions<TolkOptions> options,
            ISwedishClock clock,
            IdentityErrorDescriber identityErrorDescriber,
            INotificationService notificationService,
            CacheService cacheService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _dbContext = dbContext;
            _claimsFactory = claimsFactory;
            _userService = userService;
            _options = options.Value;
            _clock = clock;
            _identityErrorDescriber = identityErrorDescriber;
            _notificationService = notificationService;
            _cacheService = cacheService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var customerOrganisationId = User.TryGetCustomerOrganisationId();

            IEnumerable<CustomerUnitUser> customerUnitsUsers = null;
            if (customerOrganisationId.HasValue)
            {
                customerUnitsUsers = await _dbContext.CustomerUnitUsers.GetCustomerUnitsWithCustomerUnitForUser(user.Id).ToListAsync();
            }

            var model = new AccountViewModel
            {
                NameFirst = user.NameFirst,
                NameFamily = user.NameFamily,
                UserName = user.UserName,
                NameFull = user.FullName ?? "-",
                Email = user.Email ?? "-",
                PhoneWork = user.PhoneNumber ?? "-",
                PhoneCellphone = user.PhoneNumberCellphone ?? "-",
                AllowDefaultSettings = customerOrganisationId.HasValue,
                CustomerUnits = customerUnitsUsers?.Select(cu => new UnitUserModel
                {
                    IsActive = cu.CustomerUnit.IsActive,
                    Name = cu.CustomerUnit.Name,
                    IsLocalAdmin = cu.IsLocalAdmin
                })
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
                using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                if (user != null)
                {
                    // Check if user is authorized to change account
                    if (!await _userManager.CheckPasswordAsync(user, model.CurrentPassword))
                    {
                        ModelState.AddModelError(nameof(model.CurrentPassword), "Lösenordet som angivits är felaktigt.");
                        return View(model);
                    }
                    await _userService.LogOnUpdateAsync(user.Id, impersonatingUpdatedById: User.TryGetImpersonatorId());
                    user.NameFirst = model.NameFirst.Trim();
                    user.NameFamily = model.NameFamily.Trim();
                    user.PhoneNumber = model.PhoneWork?.Trim();
                    user.PhoneNumberCellphone = model.PhoneCellphone?.Trim();

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
            return View(model);
        }

        public async Task<IActionResult> ChangeEmail()
        {
            if (!await _userManager.HasPasswordAsync(await _userManager.GetUserAsync(User)))
            {
                return Forbid();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmail(ChangeEmailModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (await _userManager.HasPasswordAsync(user))
            {
                if (ModelState.IsValid)
                {
                    if (!await _userManager.CheckPasswordAsync(user, model.CurrentPassword))
                    {
                        ModelState.AddModelError(nameof(model.CurrentPassword), "Lösenordet som angivits är felaktigt.");
                        return View(model);
                    }
                    if (_userService.IsUniqueEmail(model.NewEmailAddress, user.Id))
                    {
                        await _userService.SetTemporaryEmail(user, model.NewEmailAddress, User.GetUserId(), User.TryGetImpersonatorId());
                        return await SendChangedEmailLink(user, model.NewEmailAddress);
                    }
                    ModelState.AddModelError(nameof(model.NewEmailAddress), "Denna adress används redan");
                }
            }
            return View(model);
        }

        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);

            var model = new ChangePasswordModel
            {
                HasPassword = await _userManager.HasPasswordAsync(user),
                Email = user.Email
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
                        await _userService.LogUpdatePasswordAsync(user.Id, User.TryGetImpersonatorId());
                        return RedirectToAction(nameof(ResetPasswordConfirmation));
                    }
                    AddErrors(result, nameof(model.CurrentPassword));
                }
                return View(model);
            }
            await _userService.SendInviteAsync(user);
            return RedirectToAction(nameof(ConfirmAccountLinkSent));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(Uri returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, Uri returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                return await PasswordLogin(model, returnUrl);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        private async Task<IActionResult> PasswordLogin(LoginViewModel model, Uri returnUrl)
        {
            var user = await _userManager.FindByEmailAsync(model.UserName) ?? await _userManager.FindByNameAsync(model.UserName);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: true, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    if (!user.IsActive)
                    {
                        //I want this to be done in two steps, first validating the user, then if valid user but inactive log out again, with proper message.
                        await _signInManager.SignOutAsync();
                        _logger.LogInformation("Inactivated User {userName} tried to log in.", model.UserName.ToLoggableFormat());
                        ModelState.AddModelError(nameof(model.UserName), "Ditt konto är tillfälligt inaktiverat, vänligen kontakta tolkar.avropa@kammarkollegiet.se för mer information.");
                        return View(model);
                    }
                    user.LastLoginAt = _clock.SwedenNow;
                    await _userManager.UpdateAsync(user);
                    await _dbContext.SaveChangesAsync();
                    await _userService.LogLoginAsync(user.Id);

                    _logger.LogInformation("User {userName} logged in.", model.UserName.ToLoggableFormat());
                    return RedirectToLocal(returnUrl);
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account {userName} locked out.", model.UserName.ToLoggableFormat());
                    return RedirectToAction(nameof(Lockout));
                }
            }
            //No user found, or wrong password is handled the same
            ModelState.AddModelError(nameof(model.UserName), "Felaktigt användarnamn eller lösenord.");
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
            _logger.LogDebug("Requesting password reset for {email}", model.Email.ToLoggableFormat());
            if (ModelState.IsValid)
            {
                var user = await _dbContext.Users.SingleOrDefaultAsync(u => !u.IsApiUser && u.IsActive && u.NormalizedEmail == model.Email.ToUpper());
                if (user == null)
                {
                    _logger.LogInformation("Tried to reset password for {email}, but found no such user.", model.Email.ToLoggableFormat());
                    // Don't reveal that the user does not exist
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }
                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    _logger.LogInformation("Cannot reset password for {email}/{userId}, because email is not verified.",
                        user.Email, user.Id);
                    //Send new invite if email is not confirmed and user is active and not apiUser (user has lost invite email)
                    if (user.IsActive && !user.IsApiUser && !user.LastLoginAt.HasValue)
                    {
                        return await ConfirmAccount(new ConfirmAccountModel { UserId = user.Id.ToSwedishString() });
                    }
                    // Don't reveal that user is not allowed to get password link
                    else
                    {
                        return RedirectToAction(nameof(ForgotPasswordConfirmation));
                    }
                }
                if (user.IsApiUser)
                {
                    //ApiUser cannot get password!
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

        [HttpGet]
        public async Task<IActionResult> ChangeEmailCallback(string userId, string code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
            {
                throw new ApplicationException($"Email confirmation for {userId} with code {code} failed");
            }
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var user = await _dbContext.Users.GetUserByIdWithTemporaryEmail(User.GetUserId());
                var newEmail = user.TemporaryChangedEmailEntry?.EmailAddress;
                if (!string.IsNullOrEmpty(newEmail))
                {
                    await _userService.LogUpdateEmailAsync(user.Id, user.TemporaryChangedEmailEntry?.UpdatedByUserId, user.TemporaryChangedEmailEntry?.ImpersonatingUpdatedByUserId);
                    var result = await _userManager.ChangeEmailAsync(user, newEmail, code);
                    if (result.Succeeded)
                    {
                        user.TemporaryChangedEmailEntry = null;
                        await _dbContext.SaveChangesAsync();
                        transaction.Complete();
                        return RedirectToAction(nameof(HomeController.Index), "Home", new { Message = "E-postadress uppdaterad" });
                    }
                    else
                    {
                        transaction.Dispose();
                    }
                }
            }
            return RedirectToAction(nameof(HomeController.Index), "Home", new { ErrorMessage = "E-postadress kunde inte uppdateras" });
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
                ModelState.AddModelError(nameof(model.ConfirmPassword), _identityErrorDescriber.InvalidToken().Description);
            }
            else
            {
                var result = await _userManager.ResetPasswordAsync(user, model.Code, model.NewPassword);
                if (result.Succeeded)
                {
                    await _userService.LogUpdatePasswordAsync(user.Id);
                    if ((!User.Identity.IsAuthenticated && user.IsActive) ||
                        (User.Identity.IsAuthenticated && !User.HasClaim(c => c.Type == TolkClaimTypes.IsPasswordSet)))
                    {
                        await _signInManager.SignInAsync(user, true);
                        user.LastLoginAt = _clock.SwedenNow;
                        await _userManager.UpdateAsync(user);
                        await _userService.LogLoginAsync(user.Id);
                    }
                    return RedirectToAction(nameof(ResetPasswordConfirmation));
                }
                AddErrors(result, nameof(model.ConfirmPassword));
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
                using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                if (_dbContext.IsUserStoreInitialized)
                {
                    _logger.LogWarning("Tried to CreateInitialUser even though users/roles exist in the database.");
                    ModelState.AddModelError(model.Email, "Det finns redan användare/roller i databasen, den här operationen är inte tillgänglig.");
                }
                else
                {
                    var user = new AspNetUser(model.Email, _userService.GenerateUserName(model.FirstName.Trim(), model.LastName.Trim(), string.Empty), model.FirstName.Trim(), model.LastName.Trim())
                    {
                        EmailConfirmed = true
                    };

                    var result = await _userManager.CreateAsync(user, model.NewPassword);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Created initial user account {0}", user.UserName);

                        result = await _userManager.AddToRolesAsync(user, new[] { Roles.SystemAdministrator, Roles.Impersonator, Roles.ApplicationAdministrator });
                        if (result.Succeeded)
                        {
                            _logger.LogInformation("Added {0} to System administrator and Impersonator roles", user.UserName);
                            transaction.Complete();
                            return RedirectToAction("Index", "Home");
                        }
                        await _userService.LogCreateAsync(user.Id);
                    }
                    AddErrors(result);
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
                if (newPrincipal.IsInRole(Roles.SystemAdministrator))
                {
                    throw new InvalidOperationException("Cannot impersonate a system administrator user");
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
                _logger.LogInformation("Confirmed e-mail for user {userId} ({email})", userId.ToLoggableFormat(), user.Email.ToLoggableFormat());
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
                    await _userService.SendInviteAsync(user);
                }
                else
                {
                    _logger.LogWarning("An account confirmation link was requested for non-existing user id {userId}", model.UserId.ToLoggableFormat());
                }
            }
            return RedirectToAction(nameof(ConfirmAccountLinkSent));
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            if (!_options.EnableRegisterUser)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!_options.EnableRegisterUser)
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                bool serversideValid = true;
                if (string.IsNullOrWhiteSpace(model.FirstName))
                {
                    serversideValid = false;
                    ModelState.AddModelError(nameof(model.FirstName), $"Kan inte bara innehålla mellanslag");
                }
                if (string.IsNullOrWhiteSpace(model.LastName))
                {
                    serversideValid = false;
                    ModelState.AddModelError(nameof(model.LastName), $"Kan inte bara innehålla mellanslag");
                }
                if (!_userService.IsUniqueEmail(model.Email))
                {
                    serversideValid = false;
                    ModelState.AddModelError(nameof(model.Email), $"Denna e-postadress används redan i tjänsten");
                }

                if (serversideValid)
                {
                    using var trn = await _dbContext.Database.BeginTransactionAsync();
                    var domain = model.Email.Split('@')[1];

                    var organisation = await _dbContext.CustomerOrganisations.GetParentOrganisationsByDomain(domain);

                    if (organisation != null)
                    {
                        organisation.SubCustomerOrganisations = await _dbContext.CustomerOrganisations.GetSubOrganisationsByParent(organisation.CustomerOrganisationId).ToListAsync();
                        //if organization has SubCustomerOrganisations check that one is choosed, else display the list 
                        if (organisation.SubCustomerOrganisations.Any() && string.IsNullOrEmpty(model.OrganisationIdentifier))
                        {
                            model.ParentOrganisationId = organisation.CustomerOrganisationId;
                            ModelState.AddModelError(nameof(model.Email), $"E-postdomänen {domain} har flera organisationer kopplade till sig. Välj vilken organisation du tillhör i listan nedan. Hittar du inte din organisation så kontakta {_options.Support.UserAccountEmail}.");
                            return View(model);
                        }
                        else
                        {
                            //check if user has changed parent domain to another valid parent domain
                            if (!string.IsNullOrEmpty(model.OrganisationIdentifier))
                            {
                                var selectedOrganisationId = model.OrganisationIdentifier.ToSwedishInt();
                                organisation = await _dbContext.CustomerOrganisations
                                    .Where(c => (c.CustomerOrganisationId == selectedOrganisationId && c.ParentCustomerOrganisationId == organisation.CustomerOrganisationId)
                                    || (c.CustomerOrganisationId == organisation.CustomerOrganisationId && c.CustomerOrganisationId == selectedOrganisationId))
                                    .SingleOrDefaultAsync();
                                if (organisation == null)
                                {
                                    ModelState.AddModelError(nameof(model.Email), $"Organisationen som valdes tillhörde inte domänen {domain}. Försök igen.");
                                    return View(model);
                                }
                            }
                            var user = new AspNetUser(model.Email,
                                _userService.GenerateUserName(model.FirstName.Trim(), model.LastName.Trim(), organisation.OrganisationPrefix),
                                model.FirstName.Trim(),
                                model.LastName.Trim(),
                                organisation);
                            var result = await _userManager.CreateAsync(user);

                            if (result.Succeeded)
                            {
                                await _userService.SendInviteAsync(user);
                                await _userService.LogCreateAsync(user.Id);

                                trn.Commit();
                                return RedirectToAction(nameof(ConfirmAccountLinkSent));
                            }
                            AddErrors(result);
                            return View(model);
                        }
                    }

                    var broker = await _dbContext.Brokers
                        .SingleOrDefaultAsync(b => b.EmailDomain == domain);

                    if (broker != null)
                    {
                        var user = new AspNetUser(model.Email,
                                _userService.GenerateUserName(model.FirstName.Trim(), model.LastName.Trim(), broker.OrganizationPrefix),
                                model.FirstName.Trim(),
                                model.LastName.Trim(),
                                broker);
                        var result = await _userManager.CreateAsync(user);

                        if (result.Succeeded)
                        {
                            await _userService.SendInviteAsync(user);
                            await _userService.LogCreateAsync(user.Id);

                            trn.Commit();
                            return RedirectToAction(nameof(ConfirmAccountLinkSent));
                        }
                        AddErrors(result);
                        return View(model);
                    }
                    ModelState.AddModelError(nameof(model.Email),
                        $"Myndighet med e-postdomänen {domain} finns ännu inte registrerad i tjänsten. Kontakta {_options.Support.UserAccountEmail}.");
                }
            }

            return View(model);
        }

        [Authorize(Policies.Customer)]
        public async Task<ActionResult> ViewDefaultSettings(string message)
        {
            var customerSpecificProperties = _cacheService.ActiveCustomerSpecificProperties.Where(csp => csp.CustomerOrganisationId == User.TryGetCustomerOrganisationId());
            var defaultSettingsModel = DefaultSettingsViewModel.GetModel(await GetUserForDefaultSettings(), Region.Regions, message);
            SetCustomerSpecficViewProperties(defaultSettingsModel);
            return View(defaultSettingsModel);
        }


        [Authorize(Policy = Policies.Customer)]
        public async Task<ActionResult> EditDefaultSettings(bool isFirstTimeUser = false)
        {
            var customerSpecificProperties = _cacheService.ActiveCustomerSpecificProperties.Where(csp => csp.CustomerOrganisationId == User.TryGetCustomerOrganisationId());
            var defaultSettingsModel = DefaultSettingsModel.GetModel(await GetUserForDefaultSettings(), _cacheService.CurrentOrLatestFrameworkAgreement.FrameworkAgreementResponseRuleset, isFirstTimeUser);
            SetCustomerSpecficInputProperties(defaultSettingsModel);
            return View(defaultSettingsModel);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Policy = Policies.Customer)]
        public async Task<ActionResult> EditDefaultSettings(DefaultSettingsModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            RevalidateCustomerSpecificProperties(model);
            if (ModelState.IsValid)
            {
                if (user != null)
                {
                    await _userService.LogDefaultSettingsUpdateAsync(user.Id, impersonatorUpdatedById: User.TryGetImpersonatorId());
                    UpdateDefaultSetting(user, DefaultSettingsType.Region, model.RegionId?.ToSwedishString());
                    UpdateDefaultSetting(user, DefaultSettingsType.CustomerUnit, model.CustomerUnitId?.ToSwedishString());

                    UpdateDefaultSetting(user, DefaultSettingsType.InterpreterLocationPrimary, ((int?)model.RankedInterpreterLocationFirst)?.ToSwedishString());
                    UpdateDefaultSetting(user, DefaultSettingsType.InterpreterLocationSecondary, ((int?)model.RankedInterpreterLocationSecond)?.ToSwedishString());
                    UpdateDefaultSetting(user, DefaultSettingsType.InterpreterLocationThird, ((int?)model.RankedInterpreterLocationThird)?.ToSwedishString());

                    UpdateDefaultSetting(user, DefaultSettingsType.OnSiteStreet, model.OnSiteLocationStreet);
                    UpdateDefaultSetting(user, DefaultSettingsType.OnSiteCity, model.OnSiteLocationCity);
                    UpdateDefaultSetting(user, DefaultSettingsType.OffSiteDesignatedLocationStreet, model.OffSiteDesignatedLocationStreet);
                    UpdateDefaultSetting(user, DefaultSettingsType.OffSiteDesignatedLocationCity, model.OffSiteDesignatedLocationCity);
                    UpdateDefaultSetting(user, DefaultSettingsType.OffSitePhoneContactInformation, model.OffSitePhoneContactInformation);
                    UpdateDefaultSetting(user, DefaultSettingsType.OffSiteVideoContactInformation, model.OffSiteVideoContactInformation);
                    UpdateDefaultSetting(user, DefaultSettingsType.AllowExceedingTravelCost, ((int?)model.AllowExceedingTravelCost)?.ToSwedishString());
                    UpdateDefaultSetting(user, DefaultSettingsType.InvoiceReference, model.InvoiceReference);
                    UpdateDefaultSetting(user, DefaultSettingsType.CreatorIsInterpreterUser, model.CreatorIsInterpreterUser.HasValue ? model.CreatorIsInterpreterUser.ToString() : null);

                    List<UserDefaultSettingOrderRequirement> updatedOrderRequirements = new List<UserDefaultSettingOrderRequirement>();

                    if (model.OrderRequirements != null)
                    {
                        // add all extra requirements
                        foreach (var req in model.OrderRequirements)
                        {
                            UserDefaultSettingOrderRequirement requirement = new UserDefaultSettingOrderRequirement
                            {
                                UserDefaultSettingOrderRequirementId = req.UserDefaultSettingOrderRequirementId ?? 0,
                                RequirementType = req.RequirementType.Value,
                                IsRequired = true,
                                Description = req.RequirementDescription
                            };
                            updatedOrderRequirements.Add(requirement);
                        }
                    }
                    if (model.OrderDesiredRequirements != null)
                    {
                        // add all extra desired requirements
                        foreach (var req in model.OrderDesiredRequirements)
                        {
                            UserDefaultSettingOrderRequirement requirement = new UserDefaultSettingOrderRequirement
                            {
                                UserDefaultSettingOrderRequirementId = req.UserDefaultSettingOrderRequirementId ?? 0,
                                RequirementType = req.DesiredRequirementType.Value,
                                IsRequired = false,
                                Description = req.DesiredRequirementDescription
                            };
                            updatedOrderRequirements.Add(requirement);
                        }
                    }
                    UpdateDefaultSettingOrderRequirement(user, updatedOrderRequirements);

                    await _dbContext.SaveChangesAsync();

                    return model.IsFirstTimeUser ?
                        RedirectToAction(nameof(Index), "Home", new { message = "Bokningsinställningar sparade. Du kan nu börja använda tjänsten!" }) :
                        RedirectToAction(nameof(ViewDefaultSettings), new { message = "Ändringar sparade" });
                }
                return Forbid();
            }
            return View(model);
        }

        private async Task<AspNetUser> GetUserForDefaultSettings()
        {
            return await _userService.GetUserWithDefaultSettings(User.GetUserId());
        }

        private void UpdateDefaultSetting(AspNetUser user, DefaultSettingsType type, string value)
        {
            var setting = user.DefaultSettings.SingleOrDefault(s => s.DefaultSettingType == type);
            if (setting == null && !string.IsNullOrEmpty(value))
            {
                user.DefaultSettings.Add(new UserDefaultSetting
                {
                    DefaultSettingType = type,
                    Value = value,
                });
            }
            else if (setting != null && !string.IsNullOrEmpty(value))
            {
                setting.Value = value;
            }
            else if (setting != null && string.IsNullOrEmpty(value))
            {
                _dbContext.UserDefaultSettings.Remove(setting);
            }
        }

        private void UpdateDefaultSettingOrderRequirement(AspNetUser user, List<UserDefaultSettingOrderRequirement> orderRequirements)
        {
            //kolla om de är ändrade??
            //eftersom de inte går att editera så borde man bara kunna jämföra med UserDefaultSettingOrderRequirementId?
            //om den har id är det samma om den inte har det så är det en ny

            //also save history
            var requiredOrderSettings = user.DefaultSettingOrderRequirements;
            var idsToRemove = requiredOrderSettings.Select(r => r.UserDefaultSettingOrderRequirementId).Except(orderRequirements.Select(ur => ur.UserDefaultSettingOrderRequirementId));
            var itemsToRemove = requiredOrderSettings.Where(r => idsToRemove.Contains(r.UserDefaultSettingOrderRequirementId));
            if (orderRequirements.Any())
            {
                foreach (UserDefaultSettingOrderRequirement req in orderRequirements)
                {
                    if (req.UserDefaultSettingOrderRequirementId == 0)
                    {
                        user.DefaultSettingOrderRequirements.Add(req);
                    }
                }
                if (itemsToRemove.Any())
                {
                    _dbContext.UserDefaultSettingOrderRequirements.RemoveRange(itemsToRemove);
                }
            }
            else
            {

                if (requiredOrderSettings.Any())
                {
                    _dbContext.UserDefaultSettingOrderRequirements.RemoveRange(requiredOrderSettings);
                }
            }
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
                Email = user.Email
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterNewAccount(RegisterNewAccountViewModel model)
        {
            if (string.IsNullOrEmpty(model.UserId))
            {
                throw new ApplicationException($"No UserId provided for registration");
            }

            if (ModelState.IsValid)
            {
                using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                var user = await _userManager.FindByIdAsync(model.UserId);

                if (user != null)
                {
                    await _userService.LogUpdatePasswordAsync(user.Id);
                    var result = await _userManager.ResetPasswordAsync(user, model.PasswordToken, model.NewPassword);
                    if (result.Succeeded)
                    {
                        await _userService.LogOnUpdateAsync(user.Id);
                        // Resetting the security stamp invalidates the password token so operation cannot be redone.
                        await _userManager.UpdateSecurityStampAsync(user);
                        await _signInManager.SignInAsync(user, true);

                        user.NameFirst = model.NameFirst.Trim();
                        user.NameFamily = model.NameFamily.Trim();
                        user.PhoneNumber = model.PhoneWork?.Trim();
                        user.PhoneNumberCellphone = model.PhoneCellphone?.Trim();
                        user.LastLoginAt = _clock.SwedenNow;
                        result = await _userManager.UpdateAsync(user);
                        await _userService.LogLoginAsync(user.Id);
                        model.IsCustomer = user.CustomerOrganisationId.HasValue;
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

            return View(model);
        }

        public IActionResult RegisterNewAccountConfirmation(RegisterNewAccountViewModel model)
        {
            return View(model);
        }

        #region Helpers

        // Common logic for setting of password and forgot password flows.
        private async Task<IActionResult> SendPasswordResetLink(AspNetUser user)
        {
            if (!(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Here we'll just throw. Calling action might check this as well and do better error handling.
                throw new InvalidOperationException($"Cannot send password reset to user {user.Id}/{user.Email} because email is not confirmed.");
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.ResetPasswordCallbackLink(user.Id.ToSwedishString(), code);

            var bodyPlain =
        $@"Återställning av lösenord för {Constants.SystemName}

Om du har begärt att lösenordet ska återställas för '{user.FullName}' klicka eller klistra in länken nedan i webbläsaren.

{resetLink}

{(user.IsActive ? string.Empty : @"Notera att din användare är inaktiverad. 
Du kommer fortfarande få byta lösenord, men du behöver kontakta tolkar.avropa@kammarkollegiet.se för att få mer information om aktivering av konto.")}
Om du inte har begärt en återställning av ditt lösenord kan du radera det här
meddelandet. Om du får flera meddelanden som du inte har begärt, kontakta
supporten på {_options.Support.FirstLineEmail}.";

            var bodyHtml =
        $@"<h2>Återställning av lösenord för {Constants.SystemName}</h2>

<div>Om du har begärt att lösenordet ska återställas för '{user.FullName}' klicka på länken nedan:<br /><br /></div>

<div>{HtmlHelper.GetButtonDefaultLargeTag(resetLink.AsUri(), "Återställ lösenord")}<br /><br /></div>

<div>Om det inte fungerar att klicka på länken så klistra in länken nedan i en webbläsare:<br /><br /></div>

<div>{resetLink}<br /><br /></div>

<div>{(user.IsActive ? string.Empty : @"Notera att din användare är inaktiverad. 
Du kommer fortfarande få byta lösenord, men du behöver kontakta tolkar.avropa@kammarkollegiet.se för att få mer information om aktivering av konto.")}
Om du inte har begärt en återställning av ditt lösenord kan du radera det här
meddelandet. Om du får flera meddelanden som du inte har begärt, kontakta
supporten på {_options.Support.FirstLineEmail}.</div>";

            _notificationService.CreateEmail(
                user.Email,
                $"Återställning lösenord {Constants.SystemName}",
                bodyPlain,
                bodyHtml,
                NotificationType.PasswordReset,
                isBrokerMail: false,
                addContractInfo: false);
            _dbContext.SaveChanges();

            _logger.LogInformation("Password reset link sent to {email} for {userId}",
                user.Email, user.Id);

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        private async Task<IActionResult> SendChangedEmailLink(AspNetUser user, string newEmailAddress)
        {
            var code = await _userManager.GenerateChangeEmailTokenAsync(user, newEmailAddress);
            await _userService.SendChangedEmailLink(user, newEmailAddress, Url.ChangeEmailCallbackLink(user.Id.ToSwedishString(), code));
            return RedirectToAction(nameof(HomeController.Index), "Home", new { Message = "För att slutföra bytet av e-postadress, klicka på den länk som skickats till den angivna e-postadressen." });
        }

        private void AddErrors(IdentityResult result, string controlKey = "")
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(controlKey, error.Description);
            }
        }

        private IActionResult RedirectToLocal(Uri returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl?.ToString()))
            {
                return Redirect(returnUrl?.ToString());
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
        private DefaultSettingsModel SetCustomerSpecficInputProperties(DefaultSettingsModel model)
        {
            var customerSpecificProperties = _cacheService.ActiveCustomerSpecificProperties.Where(csp => csp.CustomerOrganisationId == User.TryGetCustomerOrganisationId()).ToList();
            foreach (var property in customerSpecificProperties)
            {
                switch (property.PropertyToReplace)
                {
                    case PropertyType.InvoiceReference:
                        property.Value = model.InvoiceReference;
                        model.CustomerSpecificInvoiceReference = property;
                        model.CustomerSpecificInvoiceReference.Required = false;
                        break;
                    default:
                        break;
                }
            }
            return model;
        }
        private DefaultSettingsViewModel SetCustomerSpecficViewProperties(DefaultSettingsViewModel model)
        {
            var customerSpecificProperties = _cacheService.ActiveCustomerSpecificProperties.Where(csp => csp.CustomerOrganisationId == User.TryGetCustomerOrganisationId()).ToList();
            foreach (var property in customerSpecificProperties)
            {
                switch (property.PropertyToReplace)
                {
                    case PropertyType.InvoiceReference:
                        property.Value = model.InvoiceReference;
                        model.CustomerSpecificInvoiceReference = property;
                        break;
                    default:
                        break;
                }
            }
            return model;
        }
        private void RevalidateCustomerSpecificProperties(DefaultSettingsModel model)
        {
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("CustomerSpecific")).ToArray())
            {
                ModelState.Remove(key);
            }            
            SetCustomerSpecficInputProperties(model);
            TryValidateModel(model);
        }
        #endregion
    }
}
