using System.Threading.Tasks;
using CustomerMock.Hubs;
using CustomerMock.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Tolk.Api.Payloads.WebHookPayloads;

namespace CustomerMock.Controllers
{
    public class OrderController : Controller
    {
        private readonly IHubContext<WebHooksHub> _hubContext;
        private readonly ApiCallService _apiService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IHubContext<WebHooksHub> hubContext, ApiCallService apiService, ILogger<OrderController> logger)
        {
            _hubContext = hubContext;
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<JsonResult> Create(string description)
        {
            return Json(new { Message = await _apiService.CreateOrder(description) });
        }

        [HttpPost]
        public async Task<JsonResult> OrderAccepted([FromBody] OrderAcceptedModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Order: {payload.OrderNumber} har blivit accepterad av...");
            }
            return new JsonResult("Success");
        }
        [HttpPost]
        public async Task<JsonResult> OrderAnswered([FromBody] OrderAnsweredModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Order: {payload.OrderNumber} har blivit besvarad av...");
            }
            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> OrderDeclined([FromBody] OrderDeclinedModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Order: {payload.OrderNumber} förmedling {payload.BrokerName} tackade nej, med detta meddelande: {payload.Message} ");
            }
            return new JsonResult("Success");
        }


    }
}
