﻿using BrokerMock.Helpers;
using BrokerMock.Hubs;
using BrokerMock.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Tolk.Api.Payloads;
using Tolk.Api.Payloads.WebHookPayloads;

namespace BrokerMock.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHubContext<WebHooksHub> _hubContext;
        private readonly BrokerMockOptions _options;
        private readonly ApiCallService _apiService;
        private readonly IMemoryCache _cache;
        public HomeController(IHubContext<WebHooksHub> hubContext, IOptions<BrokerMockOptions> options, ApiCallService apiService, IMemoryCache cache)
        {
            _hubContext = hubContext;
            _options = options.Value;
            _apiService = apiService;
            _cache = cache;
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
    }
}
