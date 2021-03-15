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
using System.Threading.Tasks;
using Tolk.Api.Payloads.ApiPayloads;
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
            else if (!extraInstructions.Contains("LEAVEUNHANDLED"))
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
            var payload = new ComplaintAcceptModel
            {
                OrderNumber = orderNumber,
                CallingUser = "regular-user@formedling1.se"
            };
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                var response = await _apiService.ApiClient.PostAsync(_options.TolkApiBaseUrl.BuildUri("Complaint/Accept"), content);
                if (JsonConvert.DeserializeObject<ResponseBase>(await response.Content.ReadAsStringAsync()).Success)
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/Accept]:: Boknings-ID: {orderNumber} accepterat reklamation");
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/Accept] FAILED:: Boknings-ID: {orderNumber} accepterat reklamation");
                }

                return true;
            }
        }

        private async Task<bool> Dispute(string orderNumber, string message)
        {
            var payload = new ComplaintDisputeModel
            {
                OrderNumber = orderNumber,
                CallingUser = "regular-user@formedling1.se",
                Message = message
            };
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                var response = await _apiService.ApiClient.PostAsync(_options.TolkApiBaseUrl.BuildUri("Complaint/Dispute"), content);
                if (JsonConvert.DeserializeObject<ResponseBase>(await response.Content.ReadAsStringAsync()).Success)
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Complaint/Dispute]:: Boknings-ID: {orderNumber} Bestrider reklamation!");
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Complaint/Dispute] FAILED:: Boknings-ID: {orderNumber}Bestrider reklamation!");
                }

                return true;
            }
        }

        private async Task<ComplaintDetailsResponse> GetComplaint(string orderNumber)
        {
            var response = await _apiService.ApiClient.GetAsync(_options.TolkApiBaseUrl.BuildUri("Complaint/View", $"orderNumber={orderNumber}"));
            if (JsonConvert.DeserializeObject<ResponseBase>(await response.Content.ReadAsStringAsync()).Success)
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

        #endregion

        #region COMMON STUFF

        private static IEnumerable<string> GetExtraInstructions(string description)
        {
            if (string.IsNullOrEmpty(description))
            {
                return Enumerable.Empty<string>();
            }
            return description.ToSwedishUpper().Split(";", StringSplitOptions.RemoveEmptyEntries).AsEnumerable();
        }

        #endregion
    }
}
