using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Api.Helpers;

namespace Tolk.Web.Api.Controllers
{
    public class ListController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly TolkApiOptions _options;

        public ListController(TolkDbContext tolkDbContext, IOptions<TolkApiOptions> options)
        {
            _dbContext = tolkDbContext;
            _options = options.Value;
        }

        [HttpGet]
        public JsonResult AssignmentTypes()
        {
            return Json(EnumHelper.GetAllFullDescriptions<AssignmentType>());
        }
    }
}
