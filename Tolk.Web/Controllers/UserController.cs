using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class UserController : Controller
    {
        private readonly UserManager<AspNetUser> _userManager;
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<InterpreterController> _logger;
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        public UserController(
            UserManager<AspNetUser> userManager,
            TolkDbContext dbContext,
            ILogger<InterpreterController> logger,
            RoleManager<IdentityRole<int>> roleManager
)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _logger = logger;
            _roleManager = roleManager;
        }

        public ActionResult List(UserFilterModel model)
        {
            var users = _dbContext.Users.Select(u => u);
            if (model != null)
            {
                users = model.Apply(users, _roleManager.Roles.Select(r => new RoleMap { Id = r.Id, Name = r.Name }).ToList());
            }
            return View(new UserListModel
            {
                Items = users.Select(u => new UserListItemModel
                {
                    UserId = u.Id,
                    Email = u.Email,
                    Name = u.FullName,
                    Organisation = u.CustomerOrganisation.Name ?? u.Broker.Name ?? "-",
                    LastLoginAt = string.Format("{0:yyyy-MM-dd}", u.LastLoginAt) ?? "-",
                    IsActive = u.IsActive
                }),
                FilterModel = model
            });
        }

        public ActionResult View(int id)
        {
            var user = _userManager.Users.Include(u => u.Roles).SingleOrDefault(u => u.Id == id);
            int superUserId = _roleManager.Roles.Single(r => r.Name == Roles.SuperUser).Id;
            var model = new UserModel
            {
                Id = id,
                NameFirst = user.NameFirst,
                NameFamily = user.NameFamily,
                Email = user.Email,
                PhoneWork = user.PhoneNumber ?? "-",
                PhoneCellphone = user.PhoneNumberCellphone ?? "-",
                IsSuperUser = user.Roles.Any(r => r.RoleId == superUserId),
                LastLoginAt = string.Format("{0:yyyy-MM-dd}", user.LastLoginAt) ?? "-",
                IsActive = user.IsActive
            };

            return View(model);
        }

        public ActionResult Edit(int id)
        {
            int superUserId = _roleManager.Roles.Single(r => r.Name == Roles.SuperUser).Id;
            var user = _userManager.Users.Include(u => u.Roles).SingleOrDefault(u => u.Id == id);
            var model = new UserModel
            {
                Id = user.Id,
                Email = user.Email,
                NameFirst = user.NameFirst,
                NameFamily = user.NameFamily,
                PhoneWork = user.PhoneNumber,
                PhoneCellphone = user.PhoneNumberCellphone,
                IsSuperUser = user.Roles.Any(r => r.RoleId == superUserId)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(UserModel model)
        {
            int superUserId = _roleManager.Roles.Single(r => r.Name == Roles.SuperUser).Id;
            var user = _userManager.Users.Include(u => u.Roles).SingleOrDefault(u => u.Id == model.Id);
            user.NameFirst = model.NameFirst;
            user.NameFamily = model.NameFamily;
            user.PhoneNumber = model.PhoneWork;
            user.PhoneNumberCellphone = model.PhoneCellphone;
            user.IsActive = model.IsActive;
            if (model.IsSuperUser && !user.Roles.Any(r => r.RoleId == superUserId))
            {
                await _userManager.AddToRoleAsync(user, Roles.SuperUser);
            }
            else if (!model.IsSuperUser && user.Roles.Any(r => r.RoleId == superUserId))
            {
                await _userManager.RemoveFromRoleAsync(user, Roles.SuperUser);
            }
            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(View), model);
        }
    }
}
