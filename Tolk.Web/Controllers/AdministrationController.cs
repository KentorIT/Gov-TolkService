using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Authorization;

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
            return RedirectToAction("Index", "Home", new { Message = "Cachen har rensats" });
        }
    }
}
