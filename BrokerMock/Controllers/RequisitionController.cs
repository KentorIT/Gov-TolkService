using BrokerMock.Helpers;
using BrokerMock.Hubs;
using BrokerMock.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Enums;
using Tolk.Api.Payloads.Responses;
using Tolk.Api.Payloads.WebHookPayloads;
using Tolk.BusinessLogic.Utilities;

namespace BrokerMock.Controllers
{
    public class RequisitionController : Controller
    {
        private readonly IHubContext<WebHooksHub> _hubContext;
        private readonly BrokerMockOptions _options;
        private readonly ApiCallService _apiService;
        private readonly IMemoryCache _cache;
        public RequisitionController(IHubContext<WebHooksHub> hubContext, IOptions<BrokerMockOptions> options, ApiCallService apiService, IMemoryCache cache)
        {
            _hubContext = hubContext;
            _options = options.Value;
            _apiService = apiService;
            _cache = cache;
        }

        #region incomming

        [HttpPost]
        public async Task<JsonResult> Reviewed([FromBody] RequisitionReviewedModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Rekvisition för Boknings-ID: {payload.OrderNumber} har blivit graskad");
            }

            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> Commented([FromBody] RequisitionCommentedModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Svaret på Boknings-ID: {payload.OrderNumber} har nekats, med meddelande: '{payload.Message}'");
            }
            if (_cache.Get<List<ListItemResponse>>("LocationTypes") == null)
            {
                await _apiService.GetAllLists();
            }
            var extraInstructions = GetExtraInstructions(payload.Message);

            if (extraInstructions.Contains("GETCURRENTREQUISITION"))
            {
                await GetOrderRequisition(payload.OrderNumber);
            }

            if (extraInstructions.Contains("MAKENEWREQUISITION"))
            {
               await CreateRequisition(payload.OrderNumber);
            }
            return new JsonResult("Success");
        }

        #endregion

        private IEnumerable<string> GetExtraInstructions(string description)
        {
            if (string.IsNullOrEmpty(description))
            {
                return Enumerable.Empty<string>();
            }
            return description.ToUpper().Split(";", StringSplitOptions.RemoveEmptyEntries).AsEnumerable();
        }

        private async Task<RequestDetailsResponse> GetOrderRequisition(string orderNumber)
        {
            using (var client = GetHttpClient())
            {
                var response = await client.GetAsync($"{_options.TolkApiBaseUrl}/Requisition/View?orderNumber={orderNumber}&IncludePreviousRequisitions=false");
                if ((await response.Content.ReadAsAsync<ResponseBase>()).Success)
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Requisition/View]:: Boknings-ID: {orderNumber}");
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Requisition/View] FAILED:: Boknings-ID: {orderNumber} ErrorMessage: {errorResponse.ErrorMessage}");
                }
                return JsonConvert.DeserializeObject<RequestDetailsResponse>(await response.Content.ReadAsStringAsync());
            }
        }

        private async Task<bool> CreateRequisition(string orderNumber)
        {
            using (var client = GetHttpClient())
            {
                var payload = new RequisitionModel
                {
                    OrderNumber = orderNumber,
                    CallingUser = "regular-user@formedling1.se",
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_options.TolkApiBaseUrl}/Requisition/Create", content);
                if ((await response.Content.ReadAsAsync<ResponseBase>()).Success)
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Requisition/Create]:: Rekvisition skapad för Boknings-ID: {orderNumber}");
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Requisition/Create] FAILED:: Rekvisition skulle skapas för Boknings-ID: {orderNumber} ErrorMessage: {errorResponse.ErrorMessage}");
                }
            }

            return true;
        }

        private async Task<bool> GetFile(string orderNumber, int attachmentId)
        {
            using (var client = GetHttpClient())
            {
                var response = await client.GetAsync($"{_options.TolkApiBaseUrl}/Request/File?OrderNumber={orderNumber}&AttachmentId={attachmentId}&callingUser=regular-user@formedling1.se");
                var file = response.Content.ReadAsAsync<FileResponse>().Result;
                if (file.Success)
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/File]:: Boknings-ID: {orderNumber} fil hämtad. Base64 stäng var {file.FileBase64.Length} tecken lång");
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/File] FAILED:: Boknings-ID: {orderNumber} accat mottagande");
                }
            }

            return true;
        }

        private HttpClient GetHttpClient()
        {
            var client = new HttpClient(GetCertHandler());
            client.DefaultRequestHeaders.Accept.Clear();
            if (_options.UseApiKey)
            {
                client.DefaultRequestHeaders.Add("X-Kammarkollegiet-InterpreterService-UserName", _options.ApiUserName);
                client.DefaultRequestHeaders.Add("X-Kammarkollegiet-InterpreterService-ApiKey", _options.ApiKey);
            }
            return client;
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
