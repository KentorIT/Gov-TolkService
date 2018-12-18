using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;

namespace Tolk.Web.Controllers
{

    [Authorize(Policy = Policies.Interpreter)]
    public class InterpreterController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<InterpreterController> _logger;

        public InterpreterController(
            TolkDbContext dbContext,
            ILogger<InterpreterController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }
    }
}
