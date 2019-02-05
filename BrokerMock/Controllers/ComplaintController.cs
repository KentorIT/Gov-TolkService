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
    public class ComplaintController : Controller
    {
        private readonly IHubContext<WebHooksHub> _hubContext;
        private readonly BrokerMockOptions _options;
        private readonly ApiCallService _apiService;
        private readonly IMemoryCache _cache;

        public ComplaintController(IHubContext<WebHooksHub> hubContext, IOptions<BrokerMockOptions> options, ApiCallService apiService, IMemoryCache cache)
        {
            _hubContext = hubContext;
            _options = options.Value;
            _apiService = apiService;
            _cache = cache;
        }
        #region incomming

        [HttpPost]
        public async Task<JsonResult> Created([FromBody] ComplaintMessageModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Boknings-ID: {payload.OrderNumber} har fått reklamation registrerad.");
            }
            if (_cache.Get<List<ListItemResponse>>("LocationTypes") == null)
            {
                await _apiService.GetAllLists();
            }
            var extraInstructions = GetExtraInstructions(payload.Message);

            if (extraInstructions.Contains("DISPUTE"))
            {
                //dispute
                await Dispute(payload.OrderNumber, "Han var inte alls full!");
            }
            else
            {
                //accept
                await Accept(payload.OrderNumber);
            }
            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> ComplaintDisputedAccepted([FromBody] ComplaintMessageModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Boknings-ID: {payload.OrderNumber} reklamationens bestridning har accepterats.");
            }
            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> ComplaintDisputePendingTrial([FromBody] ComplaintMessageModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Boknings-ID: {payload.OrderNumber} reklamationens bestridning godtogs inte. Inväntar extern process.");
            }
            var extraInstructions = GetExtraInstructions(payload.Message);

            if (extraInstructions.Contains("DETAILS"))
            {
                //get details
                var complaint = await GetComplaint(payload.OrderNumber);
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Detaljer för Reklamation (Boknings-ID: {payload.OrderNumber}) har status {complaint.Status}");
            }

            return new JsonResult("Success");
        }

        #endregion

        #region api callers

        private async Task<bool> Accept(string orderNumber)
        {
            using (var client = GetHttpClient())
            {
                var payload = new ComplaintAcceptModel
                {
                    OrderNumber = orderNumber,
                    CallingUser = "regular-user@formedling1.se"
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_options.TolkApiBaseUrl}/Complaint/Accept", content);
                if (response.Content.ReadAsAsync<ResponseBase>().Result.Success)
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/Accept]:: Boknings-ID: {orderNumber} accepterat reklamation");
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/Accept] FAILED:: Boknings-ID: {orderNumber} accepterat reklamation");
                }
            }

            return true;
        }

        private async Task<bool> Dispute(string orderNumber, string message)
        {
            using (var client = GetHttpClient())
            {
                var payload = new ComplaintDisputeModel
                {
                    OrderNumber = orderNumber,
                    CallingUser = "regular-user@formedling1.se",
                    Message = message
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_options.TolkApiBaseUrl}/Complaint/Dispute", content);
                if (response.Content.ReadAsAsync<ResponseBase>().Result.Success)
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Complaint/Dispute]:: Boknings-ID: {orderNumber} Bestrider reklamation!");
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Complaint/Dispute] FAILED:: Boknings-ID: {orderNumber}Bestrider reklamation!");
                }
            }

            return true;
        }

        private async Task<ComplaintDetailsResponse> GetComplaint(string orderNumber)
        {
            using (var client = GetHttpClient())
            {
                var response = await client.GetAsync($"{_options.TolkApiBaseUrl}/Complaint/View?orderNumber=" + orderNumber);
                if ((await response.Content.ReadAsAsync<ResponseBase>()).Success)
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Complaint/View]:: Reklamation för Boknings-ID: {orderNumber} lyckad hämtning!");
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Complaint/View] FAILED::  Reklamation för Boknings-ID: {orderNumber} ErrorMessage: {errorResponse.ErrorMessage}");
                }
                return JsonConvert.DeserializeObject<ComplaintDetailsResponse>(await response.Content.ReadAsStringAsync());
            }
        }

        #endregion

        #region COMMON STUFF

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

        private IEnumerable<string> GetExtraInstructions(string description)
        {
            if (string.IsNullOrEmpty(description))
            {
                return Enumerable.Empty<string>();
            }
            return description.ToUpper().Split(";", StringSplitOptions.RemoveEmptyEntries).AsEnumerable();
        }

        #endregion
    }
}
