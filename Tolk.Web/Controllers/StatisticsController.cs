using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.Web.Authorization;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{

    [Authorize(Roles = Roles.AdminRoles)]
    public class StatisticsController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<StatisticsController> _logger;

        public StatisticsController(
            TolkDbContext dbContext,
            ILogger<StatisticsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [Authorize(Roles = Roles.Admin)]
        public ActionResult List()
        {
            return View();
        }

        [Authorize(Roles = Roles.SuperUser)]
        [Authorize(Policy = Policies.Broker)]
        public ActionResult ListLanguages()
        {
            return View(
                _dbContext.Languages.Where(l => l.Active == true)
                .OrderBy(l => l.Name).Select(l => new LanguageListItem
                {
                    ISO_639_Code = l.ISO_639_Code,
                    Name = l.Name,
                    TellusName = l.TellusName
                }));
        }
    }
}
