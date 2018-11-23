using BrokerMock.Helpers;
using BrokerMock.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Tolk.Api.Payloads.Responses;

namespace BrokerMock.Services
{
    public class ApiCallService
    {
        private readonly IHubContext<WebHooksHub> _hubContext;
        private readonly BrokerMockOptions _options;
        private readonly IMemoryCache _cache;
        public ApiCallService(IHubContext<WebHooksHub> hubContext, IOptions<BrokerMockOptions> options, IMemoryCache cache)
        {
            _hubContext = hubContext;
            _options = options.Value;
            _cache = cache;
        }

        public async Task GetAllLists()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                var response = await client.GetAsync($"{_options.TolkApiBaseUrl}/List/AssignmentTypes/");
                var items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get assignment types: {items.Count}");
                _cache.Set("AssignmentTypes", items);

                response = await client.GetAsync($"{_options.TolkApiBaseUrl}/List/LocationTypes/");
                items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get location types: {items.Count}");
                _cache.Set("LocationTypes", items);

                response = await client.GetAsync($"{_options.TolkApiBaseUrl}/List/CompetenceLevels/");
                items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get competence levels: {items.Count}");
                _cache.Set("CompetenceLevels", items);

                response = await client.GetAsync($"{_options.TolkApiBaseUrl}/List/Languages/");
                items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get languages: {items.Count}");
                _cache.Set("Languages", items);

                response = await client.GetAsync($"{_options.TolkApiBaseUrl}/List/Regions/");
                items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get regions: {items.Count}");
                _cache.Set("Regions", items);

                response = await client.GetAsync($"{_options.TolkApiBaseUrl}/List/PriceListTypes/");
                items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get price list types: {items.Count}");
                _cache.Set("PriceListTypes", items);

                response = await client.GetAsync($"{_options.TolkApiBaseUrl}/List/Customers/");
                items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get customers: {items.Count}");
                _cache.Set("Customers", items);
            }
        }

    }
}
