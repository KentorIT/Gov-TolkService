using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Authorization;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    public class AdministrationController : Controller
    {
        private readonly CacheService _cacheService;
        private readonly TolkDbContext _dbContext;
        private readonly TolkOptions _options;
        public AdministrationController(CacheService cacheService, TolkDbContext dbContext, IOptions<TolkOptions> options)
        {
            _cacheService = cacheService;
            _dbContext = dbContext;
            _options = options.Value;
        }

        [Authorize(Roles = Roles.ApplicationAdministrator)]
        public async Task<IActionResult> FlushCaches()
        {
            await _cacheService.FlushAll();
            return RedirectToAction("Index", "Home", new { Message = "Cachen har rensats" });
        }

        [Authorize(Roles = Roles.ApplicationAdministrator)]
        public IActionResult ListOptions()
        {
            return View(AdministrationOptionsModel.GetModelFromTolkOptions(_options));
        }

        [Authorize(Roles = Roles.Impersonator)]
        [ValidateAntiForgeryToken]
        public IActionResult ListUsersToImpersonate(string search, int page)
        {
            int pageSize = 10;
            int skip = pageSize * (page - 1);
            var impersonatedUserId = !string.IsNullOrEmpty(User.FindFirstValue(TolkClaimTypes.ImpersonatingUserId)) ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            IEnumerable<AjaxSelectListItemModel> userList = page == 1 ? new List<AjaxSelectListItemModel>() { LoggedInUser } : new List<AjaxSelectListItemModel>();
            var users = _dbContext.Users
                   .Where(u => u.IsActive && !u.IsApiUser &&
                   (u.InterpreterId.HasValue || u.BrokerId.HasValue || u.CustomerOrganisationId.HasValue));
            users = !string.IsNullOrWhiteSpace(search)
               ? users.Where(u => u.NameFirst.Contains(search) || u.NameFamily.Contains(search) || u.CustomerOrganisation.Name.Contains(search) || u.Broker.Name.Contains(search))
               : users;
            int count = users.Count() - skip;
            return Json(new
            {
                results = userList.Union(users
                    .OrderBy(u => u.NameFamily)
                    .ThenBy(u => u.NameFirst)
                    .Select(u => new AjaxSelectListItemModel
                    {
                        Text = !string.IsNullOrWhiteSpace(u.NameFamily) ? $"{u.NameFamily}, {u.NameFirst} ({u.CustomerOrganisation.Name ?? u.Broker.Name ?? (u.InterpreterId != null ? "Tolk" : "N/A")})" : u.UserName,
                        Id = u.Id.ToString()
                    })
                    .Skip(pageSize * (page - 1))
                    .Take(pageSize)),
                pagination = new { more = count > pageSize }
            });
        }

        [Authorize(Roles = Roles.Impersonator)]
        [ValidateAntiForgeryToken]
        public IActionResult GetCurrentUser()
        {
            var impersonatedUserId = !string.IsNullOrEmpty(User.FindFirstValue(TolkClaimTypes.ImpersonatingUserId)) ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

            return Json(impersonatedUserId == null ?
                LoggedInUser :
                  _dbContext.Users
                    .Include(u => u.CustomerOrganisation)
                    .Include(u => u.Broker)
                    .Where(u => u.Id == int.Parse(impersonatedUserId))
                    .Select(u => new AjaxSelectListItemModel
                    {
                        Text = $"{u.NameFamily}, {u.NameFirst} ({u.CustomerOrganisation.Name ?? u.Broker.Name ?? (u.InterpreterId != null ? "Tolk" : "N/A")})",
                        Id = impersonatedUserId
                    })
                    .Single()
            );
        }

        private AjaxSelectListItemModel LoggedInUser => new AjaxSelectListItemModel
        {
            Text = User.FindFirstValue(TolkClaimTypes.ImpersonatingUserName) ?? $"{User.FindFirstValue(TolkClaimTypes.PersonalName)} (Inloggad)",
            Id = User.FindFirstValue(TolkClaimTypes.ImpersonatingUserId) ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        };
    }
}
