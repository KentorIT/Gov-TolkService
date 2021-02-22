using CustomerMock.Helpers;
using CustomerMock.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Responses;
using Tolk.Api.Payloads.WebHookPayloads;
using Tolk.BusinessLogic.Utilities;

namespace CustomerMock.Services
{
    public class ApiCallService
    {
        private readonly IHubContext<WebHooksHub> _hubContext;
        private readonly CustomerMockOptions _options;
        private readonly IMemoryCache _cache;
        private readonly static HttpClient client = new HttpClient();

        public ApiCallService(IHubContext<WebHooksHub> hubContext, IOptions<CustomerMockOptions> options, IMemoryCache cache)
        {
            _hubContext = hubContext;
            _options = options?.Value;
            _cache = cache;
            client.DefaultRequestHeaders.Accept.Clear();
            lock (this)
            {
                if (_options.UseApiKey)
                {
                    if (!client.DefaultRequestHeaders.Any(h => h.Key == "X-Kammarkollegiet-InterpreterService-UserName"))
                    {
                        client.DefaultRequestHeaders.Add("X-Kammarkollegiet-InterpreterService-UserName", _options.ApiUserName);
                    }
                    if (!client.DefaultRequestHeaders.Any(h => h.Key == "X-Kammarkollegiet-InterpreterService-ApiKey"))
                    {
                        client.DefaultRequestHeaders.Add("X-Kammarkollegiet-InterpreterService-ApiKey", _options.ApiKey);
                    }
                }
            }
        }

        public async Task GetAllLists()
        {
            var response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/AssignmentTypes/"));
            var items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            _cache.Set("AssignmentTypes", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/LocationTypes/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            _cache.Set("LocationTypes", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/CompetenceLevels/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            _cache.Set("CompetenceLevels", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/Languages/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            _cache.Set("Languages", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/Regions/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            _cache.Set("Regions", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/PriceRowTypes/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            _cache.Set("PriceRowTypes", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/Customers/"));
            var customers = JsonConvert.DeserializeObject<List<CustomerItemResponse>>(await response.Content.ReadAsStringAsync());
            _cache.Set("Customers", customers);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/RequirementTypes/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            _cache.Set("RequirementTypes", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/InterpreterInformationTypes/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            _cache.Set("InterpreterInformationTypes", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/ComplaintTypes/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            _cache.Set("ComplaintTypes", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/ComplaintStatuses/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            _cache.Set("ComplaintStatuses", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/RequestStatuses/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            _cache.Set("RequestStatuses", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/RequisitionStatuses/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            _cache.Set("RequisitionStatuses", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/TaxCardTypes/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            _cache.Set("TaxCardTypes", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/AllowExceedingTravelCostTypes/"));
            items = JsonConvert.DeserializeObject<List<ListItemResponse>>(await response.Content.ReadAsStringAsync());
            _cache.Set("AllowExceedingTravelCostTypes", items);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/ErrorCodes/"));
            var errors = JsonConvert.DeserializeObject<List<ErrorResponse>>(await response.Content.ReadAsStringAsync());
            _cache.Set("ErrorCodes", errors);

            response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("List/Brokers/"));
            var brokers = JsonConvert.DeserializeObject<List<BrokerItemResponse>>(await response.Content.ReadAsStringAsync());
            _cache.Set("Brokers", brokers);
            await _hubContext.Clients.All.SendAsync("OutgoingCall", "All lists retrieved!");

        }

        public async Task<string> CreateSeveralOrders(int numberOfOrders, int delay = 1000)
        {
            for (int i = 0; i < numberOfOrders; ++i)
            {
                await CreateOrder((i % 2 == 0) ? "ADDTRAVELCOSTS" : string.Empty);
                Thread.Sleep(1000);
            }
            return $"Nu har {numberOfOrders} ordrar skickats";

        }

        public async Task<string> CreateOrder(string description)
        {
            if (_cache.Get<List<ListItemResponse>>("AssignmentTypes") == null)
            {
                await GetAllLists();
            }
            var extraInstructions = GetExtraInstructions(description);
            var startAt = DateTimeOffset.Now.AddDays(4);
            var allowExeedingTravelCosts = "no";
            if (extraInstructions.Contains("ADDTRAVELCOSTS"))
            {
                allowExeedingTravelCosts = "yes_require_approval";
            }
            var payload = new CreateOrderModel
            {
                CallingUser = "patrik@polisen.se",
                Region = _cache.Get<List<ListItemResponse>>("Regions").First().Key,
                Language = "eng",
                CreatorIsInterpreterUser = true,
                StartAt = startAt,
                EndAt = startAt.AddHours(2),
                AllowExceedingTravelCost = allowExeedingTravelCosts,
                CompetenceLevelsAreRequired = false,
                InvoiceReference = "asd",
                AssignmentType = _cache.Get<List<ListItemResponse>>("AssignmentTypes").First().Key,
                Locations = new[] { new LocationModel { Key = "on_site", Street = "Storgatan 1", City = "Hubberville", Rank = 1 } },
                Description = description,
                //CompetenceLevels,
                //Requirements = new[] { new RequirementRequestModel { IsRequired } }
                //Attachments = new[] { new AttachmentModel { FileName = "xo.docx", FileBase64 = "" } }
            };
            await _hubContext.Clients.All.SendAsync("OutgoingCall", "Order skapad!");

            using var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("Order/Create"), content);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return "Anropet saknar autentisering";
            }
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var message = JsonConvert.DeserializeObject<ValidationProblemDetails>(await response.Content.ReadAsStringAsync());
                return $"Order skapades INTE. Problem med data-valideringen: {message.Title}";
            }
            if (JsonConvert.DeserializeObject<ResponseBase>(await response.Content.ReadAsStringAsync()).Success)
            {
                var info = JsonConvert.DeserializeObject<CreateOrderResponse>(await response.Content.ReadAsStringAsync());
                return $"Order skapad: {info.OrderNumber}, beräknat pris: {info.PriceInformation.TotalPrice.ToSwedishString()}";
            }
            else
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                return $"Order skapades INTE. Felmeddelande: {errorResponse.ErrorMessage}";
            }
        }

        public async Task<string> ApproveAnswer(string orderNumber, string brokerKey)
        {
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"Godkänn ordersvar från {await GetBrokerName(brokerKey)}!");
            var payload = new ApproveAnswerModel
            {
                CallingUser = "patrik@polisen.se",
                OrderNumber = orderNumber,
                BrokerIdentifier = brokerKey,
            };

            using var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("Order/ApproveAnswer"), content);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return "Anropet saknar autentisering";
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var message = JsonConvert.DeserializeObject<ValidationProblemDetails>(await response.Content.ReadAsStringAsync());
                return $"Svar kunde INTE godkännas. Skickade till fel adress";
            }
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var message = JsonConvert.DeserializeObject<ValidationProblemDetails>(await response.Content.ReadAsStringAsync());
                return $"Svar kunde INTE godkännas. Problem med data-valideringen: {message.Title}";
            }
            if (JsonConvert.DeserializeObject<ResponseBase>(await response.Content.ReadAsStringAsync()).Success)
            {
                var info = JsonConvert.DeserializeObject<CreateOrderResponse>(await response.Content.ReadAsStringAsync());
                return $"Svar har godkänts för order: {info.OrderNumber}";
            }
            else
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                return $"Order skapades INTE. Felmeddelande: {errorResponse.ErrorMessage}";
            }
        }

        public async Task<string> DenyAnswer(string orderNumber, string brokerKey, string denyMessage = "API Said NO!")
        {
            await _hubContext.Clients.All.SendAsync("OutgoingCall", $"AVslå ordersvar från {await GetBrokerName(brokerKey)}!");
            var payload = new DenyAnswerModel
            {
                CallingUser = "patrik@polisen.se",
                OrderNumber = orderNumber,
                BrokerIdentifier = brokerKey,
                Message = denyMessage
            };

            using var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("Order/DenyAnswer"), content);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return "Anropet saknar autentisering";
            }
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var message = JsonConvert.DeserializeObject<ValidationProblemDetails>(await response.Content.ReadAsStringAsync());
                return $"Svar kunde INTE avslås. Problem med data-valideringen: {message.Title}";
            }
            if (JsonConvert.DeserializeObject<ResponseBase>(await response.Content.ReadAsStringAsync()).Success)
            {
                var info = JsonConvert.DeserializeObject<CreateOrderResponse>(await response.Content.ReadAsStringAsync());
                return $"Svar har avslagits för order: {info.OrderNumber}";
            }
            else
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                return $"Order skapades INTE. Felmeddelande: {errorResponse.ErrorMessage}";
            }
        }

        public async Task<string> GetBrokerName(string key)
        {
            if (_cache.Get<List<BrokerItemResponse>>("Brokers") == null)
            {
                await GetAllLists();
            }

            return _cache.Get<List<BrokerItemResponse>>("Brokers").SingleOrDefault(b => b.Key == key)?.Name;
        }

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
