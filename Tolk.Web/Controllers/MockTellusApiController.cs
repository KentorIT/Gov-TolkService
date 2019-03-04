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
        public JsonResult Get(int id)
        {
            dynamic result = _tellusService.GetInterpreter(id);
            if (result == null)
            {
                // Actual API returns an empty array if no result was found
                result = new int[] { };
            }
            return Json(result);
        }
    }
}
