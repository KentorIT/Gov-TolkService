using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;
using Tolk.Web.Services;

namespace Tolk.Web.Controllers
{
    public class AdministrationController : Controller
    {
        private readonly CacheService _cacheService;
        public AdministrationController(CacheService cacheService)
        {
            _cacheService = cacheService;
        }

        [Authorize(Roles = Roles.ApplicationAdministrator)]
        public async Task<IActionResult> FlushCaches()
        {
            await _cacheService.FlushAll();
            return RedirectToAction("Index", "Home", new {Message = "Cachen har rensats" });
        }
    }
}
