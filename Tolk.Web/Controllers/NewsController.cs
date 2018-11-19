using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;

namespace Tolk.Web.Controllers
{

    [Authorize(Roles = Roles.Admin)]
    public class NewsController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<NewsController> _logger;

        public NewsController(
            TolkDbContext dbContext,
            ILogger<NewsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public ActionResult List()
        {
            return View();
        }
    }
}
