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
    public class StatisticsController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<InterpreterController> _logger;

        public StatisticsController(
            TolkDbContext dbContext,
            ILogger<InterpreterController> logger)
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
