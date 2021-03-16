using CustomerMock.Hubs;
using CustomerMock.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
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
        #region actions from home page 
        public async Task<JsonResult> CreateSeveralOrders(int numberOfOrders, int delay = 1000)
        {
            return Json(new { Message = await _apiService.CreateSeveralOrders(numberOfOrders, delay) });
        }

        public async Task<JsonResult> Create(string description)
        {
            return Json(new { Message = await _apiService.CreateOrder(description) });
        }

        public async Task<JsonResult> ApproveAnswer(string orderNumber, string brokerKey)
        {
            return Json(new { Message = await _apiService.ApproveAnswer(orderNumber, brokerKey) });
        }

        public async Task<JsonResult> DenyAnswer(string orderNumber, string brokerKey)
        {
            return Json(new { Message = await _apiService.DenyAnswer(orderNumber, brokerKey) });
        }
        public async Task<JsonResult> ConfirmNoAnswer(string orderNumber)
        {
            return Json(new { Message = await _apiService.ConfirmNoAnswer(orderNumber) });
        }
        public async Task<JsonResult> ConfirmCancellation(string orderNumber)
        {
            return Json(new { Message = await _apiService.ConfirmCancellation(orderNumber) });
        }
        public async Task<JsonResult> Cancel(string orderNumber, string message)
        {
            return Json(new { Message = await _apiService.CancelOrder(orderNumber, message) });
        }

        #endregion 
        #region incomming web hooks

        [HttpPost]
        public async Task<JsonResult> OrderAccepted([FromBody] OrderAcceptedModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Order: {payload.OrderNumber} har blivit accepterad av {await _apiService.GetBrokerName(payload.BrokerKey)}");
            }
            return new JsonResult("Success");
        }
        [HttpPost]
        public async Task<JsonResult> OrderAnswered([FromBody] OrderAnsweredModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Order: {payload.OrderNumber} har blivit besvarad av {await _apiService.GetBrokerName(payload.BrokerKey)}");
            }
            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> OrderDeclined([FromBody] OrderDeclinedModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Order: {payload.OrderNumber} förmedling {await _apiService.GetBrokerName(payload.BrokerKey)} tackade nej, med detta meddelande: {payload.Message} ");
            }
            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> OrderCancelled([FromBody] OrderCancelledModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Order: {payload.OrderNumber} förmedling {await _apiService.GetBrokerName(payload.BrokerKey)} avbokade, med detta meddelande: {payload.Message} ");
            }
            return new JsonResult("Success");
        }

        #endregion

    }
}
