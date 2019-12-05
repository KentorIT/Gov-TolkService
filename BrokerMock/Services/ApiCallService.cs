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
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Responses;
using Tolk.BusinessLogic.Utilities;

namespace BrokerMock.Services
{
    public class ApiCallService
    {
        private readonly IHubContext<WebHooksHub> _hubContext;
        private readonly BrokerMockOptions _options;
        private readonly IMemoryCache _cache;
        private readonly static HttpClient client = new HttpClient(GetCertHandler());

        public ApiCallService(IHubContext<WebHooksHub> hubContext, IOptions<BrokerMockOptions> options, IMemoryCache cache)
        {
            _hubContext = hubContext;
            _options = options?.Value;
            _cache = cache;
            client.DefaultRequestHeaders.Accept.Clear();
            if (_options.UseApiKey && !client.DefaultRequestHeaders.Any(h=> h.Key == "X-Kammarkollegiet-InterpreterService-UserName"))
            {
                client.DefaultRequestHeaders.Add("X-Kammarkollegiet-InterpreterService-UserName", _options.ApiUserName);
                client.DefaultRequestHeaders.Add("X-Kammarkollegiet-InterpreterService-ApiKey", _options.ApiKey);
            }
        }

        public async Task GetAllLists()
        {
            var response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/AssignmentTypes/"));
            var items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get assignment types: {items.Count}");
            _cache.Set("AssignmentTypes", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/LocationTypes/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get location types: {items.Count}");
            _cache.Set("LocationTypes", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/CompetenceLevels/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get competence levels: {items.Count}");
            _cache.Set("CompetenceLevels", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/Languages/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get languages: {items.Count}");
            _cache.Set("Languages", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/Regions/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get regions: {items.Count}");
            _cache.Set("Regions", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/PriceListTypes/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get price list types: {items.Count}");
            _cache.Set("PriceListTypes", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/TravelCostAgreementTypes/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get travel cost agreement types: {items.Count}");
            _cache.Set("TravelCostAgreementTypes", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/PriceRowTypes/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get price row types: {items.Count}");
            _cache.Set("PriceRowTypes", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/Customers/"));
            var customers = JsonConvert.DeserializeObject<List<CustomerItemResponse>>(await response.Content.ReadAsStringAsync());
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get customers: {customers.Count}");
            _cache.Set("Customers", customers);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/RequirementTypes/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get requirement types: {items.Count}");
            _cache.Set("RequirementTypes", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/InterpreterInformationTypes/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get interpreter information types: {items.Count}");
            _cache.Set("InterpreterInformationTypes", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/ComplaintTypes/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get complaint types: {items.Count}");
            _cache.Set("ComplaintTypes", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/ComplaintStatuses/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get complaint statuses: {items.Count}");
            _cache.Set("ComplaintStatuses", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/RequestStatuses/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get request statuses: {items.Count}");
            _cache.Set("RequestStatuses", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/RequisitionStatuses/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get requisition statuses: {items.Count}");
            _cache.Set("RequisitionStatuses", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/TaxCardTypes/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get tax card types: {items.Count}");
            _cache.Set("TaxCardTypes", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/ErrorCodes/"));
            var errors = JsonConvert.DeserializeObject<List<ErrorResponse>>(await response.Content.ReadAsStringAsync());
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get error codes: {errors.Count}");
            _cache.Set("ErrorCodes", errors);
            await GetInterpreters();
        }

        public async Task GetInterpreters()
        {
            var response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/BrokerInterpreters/"));
            var responseString = await response.Content.ReadAsStringAsync();
            if (JsonConvert.DeserializeObject<ResponseBase>(responseString).Success)
            {
                var interpreterResponse = JsonConvert.DeserializeObject<BrokerInterpretersResponse>(responseString);
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get existing interpreters: {interpreterResponse.Interpreters.Count()}");
                _cache.Set("BrokerInterpreters", interpreterResponse.Interpreters);
            }
            else
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseString);
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Get existing interpreters FAILED:: ErrorMessage: {errorResponse.ErrorMessage}");
            }
        }

        #region SAME AS REQUESTCONTROLLER, SHOULD BE MOVED

        public async Task<RequestDetailsResponse> GetOrderRequest(string orderNumber)
        {
            var response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("Request/View", $"orderNumber={orderNumber}"));
            if ((await response.Content.ReadAsAsync<ResponseBase>()).Success)
            {
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/View]:: Boknings-ID: {orderNumber}");
            }
            else
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/View] FAILED:: Boknings-ID: {orderNumber} ErrorMessage: {errorResponse.ErrorMessage}");
            }
            return JsonConvert.DeserializeObject<RequestDetailsResponse>(await response.Content.ReadAsStringAsync());
        }

        public async Task<RequestGroupDetailsResponse> GetOrderGroupRequest(string orderGroupNumber)
        {
            var response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("RequestGroup/View", $"orderGroupNumber={orderGroupNumber}"));
            if ((await response.Content.ReadAsAsync<ResponseBase>()).Success)
            {
                var viewResponse = JsonConvert.DeserializeObject<RequestGroupDetailsResponse>(await response.Content.ReadAsStringAsync());
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[RequestGroup/View]:: Boknings-ID: {viewResponse.OrderGroupNumber}");
            }
            else
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[RequestGroup/View] FAILED:: Boknings-ID: {orderGroupNumber} ErrorMessage: {errorResponse.ErrorMessage}");
            }
            return JsonConvert.DeserializeObject<RequestGroupDetailsResponse>(await response.Content.ReadAsStringAsync());
        }

        public async Task<RequestDetailsResponse> GetOrderRequisition(string orderNumber)
        {
            var response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("Requisition/View", $"orderNumber={orderNumber}&IncludePreviousRequisitions=false"));
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

        public async Task<bool> CreateRequisition(string orderNumber)
        {
            var request = await GetOrderRequest(orderNumber);

            var payload = new RequisitionModel
            {
                OrderNumber = orderNumber,
                AcctualStartedAt = request.StartAt,
                AcctualEndedAt = request.EndAt,
                CallingUser = "regular-user@formedling1.se",
                TaxCard = _cache.Get<List<ListItemResponse>>("TaxCardTypes").First().Key,
                Message = "Testar att skicka en ny rekvisition, då",
                MealBreaks = Enumerable.Empty<MealBreakModel>()
            };
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("Requisition/Create"), content);
                if ((await response.Content.ReadAsAsync<ResponseBase>()).Success)
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Requisition/Create]:: Rekvisition skapad för Boknings-ID: {orderNumber}");
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Requisition/Create] FAILED:: Rekvisition skulle skapas för Boknings-ID: {orderNumber} ErrorMessage: {errorResponse.ErrorMessage}");
                }

                return true;
            }
        }

        public async Task<RequestDetailsResponse> GetInterpreter(string officialInterpreterId)
        {
            var response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("Interpreter/View", $"officialInterpreterId={officialInterpreterId}"));
            ViewInterpreterResponse responseInterpreter = response.Content.ReadAsAsync<ViewInterpreterResponse>().Result;
            if (responseInterpreter.Success)
            {
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Interpreter/View]:: Hämtade Tolk {responseInterpreter.Interpreter.Email} med hjälp av kamkid: {officialInterpreterId}");
            }
            else
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Interpreter/View] FAILED:: Tolk med kamkid: {officialInterpreterId} kunde inte hämtas. ErrorMessage: {errorResponse.ErrorMessage}");
            }
            return JsonConvert.DeserializeObject<RequestDetailsResponse>(await response.Content.ReadAsStringAsync());
        }

        public async Task<RequestDetailsResponse> GetInterpreter(int interpreterId)
        {
            var response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("Interpreter/View", $"interpreterId={interpreterId}"));
            ViewInterpreterResponse responseInterpreter = response.Content.ReadAsAsync<ViewInterpreterResponse>().Result;
            if (responseInterpreter.Success)
            {
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Interpreter/View]:: Hämtade Tolk {responseInterpreter.Interpreter.Email} med hjälp av id: {interpreterId}");
            }
            else
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Interpreter/View] FAILED:: Tolk med id: {interpreterId} kunde inte hämtas. ErrorMessage: {errorResponse.ErrorMessage}");
            }
            return JsonConvert.DeserializeObject<RequestDetailsResponse>(await response.Content.ReadAsStringAsync());
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
