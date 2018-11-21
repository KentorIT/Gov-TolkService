using BrokerMock.Helpers;
using BrokerMock.Hubs;
using BrokerMock.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Tolk.Api.Payloads;
using Tolk.Api.Payloads.Responses;

namespace BrokerMock.Controllers
{
    public class RequestController : Controller
    {
        private readonly IHubContext<WebHooksHub> _hubContext;
        private readonly BrokerMockOptions _options;
        private readonly ApiCallService _apiService;
        private readonly IMemoryCache _cache;
        public RequestController(IHubContext<WebHooksHub> hubContext, IOptions<BrokerMockOptions> options, ApiCallService apiService, IMemoryCache cache)
        {
            _hubContext = hubContext;
            _options = options.Value;
            _apiService = apiService;
            _cache = cache;
        }

        [HttpPost]
        public async Task<JsonResult> Created([FromBody] RequestModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Avrops-ID: {payload.OrderNumber} skapad av {payload.Customer} i {payload.Region}");
            }
            if (_cache.Get<List<ListItemModel>>("LocationTypes") == null)
            {
                await _apiService.GetAllLists();
            }
            await AssignInterpreter(
                payload.OrderNumber,
                "ara@tolk.se",
                payload.Locations.First().Key,
                payload.CompetenceLevels.OrderBy(c => c.Rank).FirstOrDefault()?.Key ?? _cache.Get<List<ListItemModel>>("CompetenceLevels").First().Key
            );
            //Get the headers:
            //X-Kammarkollegiet-InterpreterService-Delivery
            return new JsonResult("Success");
        }

        private async Task<bool> AssignInterpreter(string orderNumber, string interpreter, string location, string competenceLevel)
        {
            //Need app settings: UseCertFile, Cert.FilePath, CertPublicKey
            using (var client = new HttpClient(GetCertHandler()))
            {
                client.DefaultRequestHeaders.Accept.Clear();
                if (_options.UseSecret)
                {
                    client.DefaultRequestHeaders.Add("X-Kammarkollegiet-InterpreterService-CallerSecret", _options.Secret);
                }
                var payload = new RequestAssignModel
                {
                    OrderNumber = orderNumber,
                    Interpreter = interpreter,
                    Location = location,
                    CompetenceLevel = competenceLevel,
                    ExpectedTravelCosts = 0,
                    CallingUser = "regular-user@formedling1.se"
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_options.TolkApiBaseUrl}/Request/Accept", content);
                if (response.Content.ReadAsAsync<ResponseBase>().Result.Success)
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/Accept]:: Avrops-ID: {orderNumber} skickad tolk: {interpreter}");
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall FAILED", $"[Request/Accept]:: Avrops-ID: {orderNumber} skickad tolk: {interpreter}");
                }
            }

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
    }
}
