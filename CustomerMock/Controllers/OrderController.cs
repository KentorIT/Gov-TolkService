using System.Threading.Tasks;
using CustomerMock.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CustomerMock.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApiCallService _apiService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(ApiCallService apiService, ILogger<OrderController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<JsonResult> Create()
        {
            return Json(new { Message = await _apiService.CreateOrder() });
        }
    }
}
