using BrokerMock.Helpers;
using BrokerMock.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
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

                response = await client.GetAsync($"{_options.TolkApiBaseUrl}/List/PriceRowTypes/");
                items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get price row types: {items.Count}");
                _cache.Set("PriceRowTypes", items);

                response = await client.GetAsync($"{_options.TolkApiBaseUrl}/List/Customers/");
                items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get customers: {items.Count}");
                _cache.Set("Customers", items);

                response = await client.GetAsync($"{_options.TolkApiBaseUrl}/List/RequirementTypes/");
                items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get requirement types: {items.Count}");
                _cache.Set("RequirementTypes", items);

                response = await client.GetAsync($"{_options.TolkApiBaseUrl}/List/InterpreterInformationTypes/");
                items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get interpreter information types: {items.Count}");
                _cache.Set("InterpreterInformationTypes", items);

                response = await client.GetAsync($"{_options.TolkApiBaseUrl}/List/ComplaintTypes/");
                items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get complaint types: {items.Count}");
                _cache.Set("ComplaintTypes", items);

                response = await client.GetAsync($"{_options.TolkApiBaseUrl}/List/ComplaintStatuses/");
                items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get complaint statuses: {items.Count}");
                _cache.Set("ComplaintStatuses", items);

                response = await client.GetAsync($"{_options.TolkApiBaseUrl}/List/RequestStatuses/");
                items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get request statuses: {items.Count}");
                _cache.Set("RequestStatuses", items);
            }
            using (var client = GetHttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                var response = await client.GetAsync($"{_options.TolkApiBaseUrl}/List/BrokerInterpreters/");
                var responseString = await response.Content.ReadAsStringAsync();
                if (JsonConvert.DeserializeObject<ResponseBase>(responseString).Success)
                {
                    var interpreterResponse = JsonConvert.DeserializeObject<BrokerInterpretersResponse>(responseString);
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get existing interpreters: {interpreterResponse.Interpreters.Count}");
                    _cache.Set("BrokerInterpreters", interpreterResponse.Interpreters);
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseString);
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get existing interpreters FAILED:: ErrorMessage: {errorResponse.ErrorMessage}");
                }
            }
        }

        #region SAME AS REQUESTCONTROLLER, SHOULD BE MOVED

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

        #endregion
    }
}
