using BrokerMock.Hubs;
using BrokerMock.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Tolk.Api.Payloads.WebHookPayloads;

namespace BrokerMock.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHubContext<WebHooksHub> _hubContext;
        private readonly ApiCallService _apiService;
        public HomeController(IHubContext<WebHooksHub> hubContext, ApiCallService apiService)
        {
            _hubContext = hubContext;
            _apiService = apiService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<JsonResult> GetLists()
        {
            //Also add cert to call
            await _apiService.GetAllLists();
            return Json(new { Success = true });
        }
        public async Task<JsonResult> ErrorMessage([FromBody] ErrorMessageModel payload)
        {
            //Also add cert to call
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Failure for this callid:{payload.CallId} when trying to using this type:{payload.NotificationType}");
            }
            return new JsonResult("Success");
        }
        public async Task<JsonResult> CustomerCreated([FromBody] CustomerCreatedModel payload)
        {
            //Also add cert to call
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: New Customer created:{payload.Name} with this prefix:{payload.Key} and org-no {payload.OrganisationNumber}");
            }
            return new JsonResult("Success");
        }
    }
}
