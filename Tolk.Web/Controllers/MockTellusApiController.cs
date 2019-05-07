using Microsoft.AspNetCore.Mvc;
using Tolk.Web.Services;

namespace Tolk.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MockTellusApiController : Controller
    {
        private readonly MockTellusApiService _tellusService;

        public MockTellusApiController(MockTellusApiService tellusService)
        {
            _tellusService = tellusService;
        }

        [HttpGet]
        [Route("Get")]
        public JsonResult Get(string id)
        {
            return Json(_tellusService.GetInterpreter(id));
        }
    }
}
