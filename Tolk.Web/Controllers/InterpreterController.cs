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

    [Authorize(Policy = Policies.Interpreter)]
    public class InterpreterController : Controller
    {
        private TolkDbContext _dbContext;
        private ILogger<InterpreterController> _logger;

        public InterpreterController(
            TolkDbContext dbContext,
            ILogger<InterpreterController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> AcceptBroker(int id)
        {
            var interpreterBroker = await _dbContext.InterpreterBrokers
                .SingleAsync(ib => ib.InterpreterId == User.GetInterpreterId()
                && ib.BrokerId == id);

            interpreterBroker.AcceptedByInterpreter = true;

            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }
    }
}
