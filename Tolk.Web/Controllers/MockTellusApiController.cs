using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Tolk.BusinessLogic.Helpers;
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
            TellusModel model;
            var result = _tellusService.GetInterpreter(id);
            if (result != null)
            {
                model = new TellusModel
                {
                    Status = 200,
                    TotalMatching = 1,
                    Result = new List<ITellusResultModel> { result }
                };
            }
            else
            {
                model = new TellusModel { Status = 200, TotalMatching = 0, Result = new List<ITellusResultModel> { } };
            }
            return Json(model);
        }
    }
}
