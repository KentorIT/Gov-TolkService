using BrokerMock.Helpers;
using BrokerMock.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
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
        private readonly BrokerMockOptions _options;
        public RequestController(IHubContext<WebHooksHub> hubContext, IOptions<BrokerMockOptions> options)
        {
            _hubContext = hubContext;
            _options = options.Value;
        }

        [HttpPost]
        public async Task<JsonResult> Created([FromBody] RequestModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Avrops-ID: {payload.OrderNumber} skapad av {payload.Customer} i {payload.Region}");
            }
            await GetLists();
            await AssignInterpreter(payload.OrderNumber, "ara@tolk.se");
            //Get the headers:
            //X-Kammarkollegiet-InterpreterService-Delivery
            return new JsonResult("Success");
        }

        private async Task<bool> AssignInterpreter(string orderNumber, string interpreter)
        {
            //Need app settings: UseCertFile, Cert.FilePath, CertPublicKey
            using (var client = new HttpClient(GetCertHandler()))
            {
                client.DefaultRequestHeaders.Accept.Clear();
                if (_options.UseSecret)
                {
                    client.DefaultRequestHeaders.Add("X-Kammarkollegiet-InterpreterService-CallerSecret", _options.Secret);
                }
                //Also add cert to call
                var payload = new RequestAssignModel
                {
                    OrderNumber = orderNumber,
                    Interpreter = interpreter,
                    //Location = 
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_options.TolkApiBaseUrl}/Request/AssignInterpreter", content);
                var resultText = await response.Content.ReadAsStringAsync();
            }
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[AssignInterpreter]:: Avrops-ID: {orderNumber} skickad tolk: {interpreter}");
            return true;
        }

        private static HttpClientHandler GetCertHandler()
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                SslProtocols = SslProtocols.Tls12
            };
            handler.ClientCertificates.Add(new X509Certificate2("cert.crt"));
            return handler;
        }

        private async Task<bool> GetLists()
        {
            using (var client = new HttpClient(GetCertHandler()))
            {
                client.DefaultRequestHeaders.Accept.Clear();
                //Also add cert to call
                var response = await client.GetAsync($"{_options.TolkApiBaseUrl}/List/AssignmentTypes/");
                var resultText = await response.Content.ReadAsStringAsync();
            }
            return true;
        }
    }
}
