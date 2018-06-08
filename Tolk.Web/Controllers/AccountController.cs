using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
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
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly TolkDbContext _dbContext;
        private readonly IUserClaimsPrincipalFactory<AspNetUser> _claimsFactory;

        public AccountController(
            UserManager<AspNetUser> userManager,
            SignInManager<AspNetUser> signInManager,
            RoleManager<IdentityRole<int>> roleManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger,
            TolkDbContext dbContext,
            IUserClaimsPrincipalFactory<AspNetUser> claimsFactory)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _logger = logger;
            _dbContext = dbContext;
            _claimsFactory = claimsFactory;
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
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return RedirectToLocal(returnUrl);
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
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
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.ResetPasswordCallbackLink(user.Id.ToString(), code, Request.Scheme);
                await _emailSender.SendEmailAsync(model.Email, "Reset Password",
                   $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
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
        public IActionResult ResetPassword(string code = null)
        {
            if (code == null)
            {
                throw new ApplicationException("A code must be supplied for password reset.");
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
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            AddErrors(result);
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
                        var user = new AspNetUser { UserName = model.Email, Email = model.Email };
                        var result = await _userManager.CreateAsync(user, model.Password);
                        if (result.Succeeded)
                        {
                            _logger.LogInformation("Created initial user account {0}", user.UserName);

                            // Gör array av roller, loopa över, kolla result för varje.

                            var roles = new IdentityRole<int>[]
                            {
                                new IdentityRole<int>(Roles.Admin),
                                new IdentityRole<int>(Roles.Impersonator),
                            };

                            foreach(var role in roles)
                            {
                                result = await _roleManager.CreateAsync(role);
                                if (!result.Succeeded)
                                {
                                    break;
                                }
                                _logger.LogInformation("Created role {0}.", role.Name);
                            }

                            if(result.Succeeded)
                            {
                                result = await _userManager.AddToRolesAsync(user, new[] { Roles.Admin, Roles.Impersonator });
                                if(result.Succeeded)
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
            
            if(model.UserId != User.FindFirstValue(TolkClaimTypes.ImpersonatingUserId))
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
