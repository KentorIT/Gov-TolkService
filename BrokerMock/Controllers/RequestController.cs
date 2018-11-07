using BrokerMock.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Tolk.Api.Payloads;

namespace BrokerMock.Controllers
{
    public class RequestController : Controller
    {
        private readonly IHubContext<WebHooksHub> _hubContext;
        public RequestController(IHubContext<WebHooksHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<JsonResult> Created([FromBody] RequestCreatedModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterperterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Avrops-ID: {payload.OrderNumber} skapad av {payload.Customer} i {payload.Region}");
            }
            //Get the headers:
            //X-Kammarkollegiet-InterperterService-Delivery
            return new JsonResult("Success");
        }
    }
}
