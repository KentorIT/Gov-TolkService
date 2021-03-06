﻿using BrokerMock.Hubs;
using BrokerMock.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Api.Payloads.Responses;
using Tolk.Api.Payloads.WebHookPayloads;
using Tolk.BusinessLogic.Utilities;

namespace BrokerMock.Controllers
{
    public class RequisitionController : Controller
    {
        private readonly IHubContext<WebHooksHub> _hubContext;
        private readonly ApiCallService _apiService;
        private readonly IMemoryCache _cache;

        public RequisitionController(IHubContext<WebHooksHub> hubContext, ApiCallService apiService, IMemoryCache cache)
        {
            _hubContext = hubContext;
            _apiService = apiService;
            _cache = cache;
        }

        #region incomming

        [HttpPost]
        public async Task<JsonResult> Reviewed([FromBody] RequisitionReviewedModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type}]:: Rekvisition för Boknings-ID: {payload.OrderNumber} har blivit graskad");
            }

            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> Commented([FromBody] RequisitionCommentedModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type}]:: Svaret på Boknings-ID: {payload.OrderNumber} har nekats, med meddelande: '{payload.Message}'");
            }
            if (_cache.Get<List<ListItemResponse>>("LocationTypes") == null)
            {
                await _apiService.GetAllLists();
            }
            var extraInstructions = GetExtraInstructions(payload.Message);

            if (extraInstructions.Contains("GETCURRENTREQUISITION"))
            {
                await _apiService.GetOrderRequisition(payload.OrderNumber);
            }

            if (extraInstructions.Contains("MAKENEWREQUISITION"))
            {
                await _apiService.CreateRequisition(payload.OrderNumber);
            }
            return new JsonResult("Success");
        }

        #endregion

        private static IEnumerable<string> GetExtraInstructions(string description)
        {
            if (string.IsNullOrEmpty(description))
            {
                return Enumerable.Empty<string>();
            }
            return description.ToSwedishUpper().Split(";", StringSplitOptions.RemoveEmptyEntries).AsEnumerable();
        }
    }
}
