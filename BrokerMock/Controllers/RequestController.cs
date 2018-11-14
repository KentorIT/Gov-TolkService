using BrokerMock.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
        public async Task<JsonResult> Created([FromBody] RequestModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterperterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Avrops-ID: {payload.OrderNumber} skapad av {payload.Customer} i {payload.Region}");
            }

            await AssignInterpreter(payload.OrderNumber, "ara@tolk.se");
            //Get the headers:
            //X-Kammarkollegiet-InterperterService-Delivery
            return new JsonResult("Success");
        }

        private async Task<bool> AssignInterpreter(string orderNumber, string interpreter)
        {
            //Need app settings: UseCertFile, Cert.FilePath, CertPublicKey
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                SslProtocols = SslProtocols.Tls12
            };
            handler.ClientCertificates.Add(new X509Certificate2("cert.crt"));
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Accept.Clear();
                //Also add cert to call
                var payload = new RequestAssignModel
                {
                    OrderNumber = orderNumber,
                    Interpreter = interpreter,
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://localhost:5656/Request/Assign", content);
            }
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[AssignInterpreter]:: Avrops-ID: {orderNumber} skickad tolk: {interpreter}");
            return true;
        }
    }
}
