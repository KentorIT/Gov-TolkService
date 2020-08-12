using CustomerMock.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Responses;
using Tolk.BusinessLogic.Utilities;

namespace CustomerMock.Services
{
    public class ApiCallService
    {
        private readonly CustomerMockOptions _options;
        private readonly IMemoryCache _cache;
        private readonly static HttpClient client = new HttpClient();

        public ApiCallService(IOptions<CustomerMockOptions> options, IMemoryCache cache)
        {
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

        public async Task<string> CreateOrder()
        {
            var payload = new CreateOrderModel
            {
            };
            using var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("Order/Create"), content);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return "Anropet saknar autentisering";
            }
            if (JsonConvert.DeserializeObject<ResponseBase>(await response.Content.ReadAsStringAsync()).Success)
            {
                return $"Order skapad: {JsonConvert.DeserializeObject<CreateOrderResponse>(await response.Content.ReadAsStringAsync()).OrderNumber}";
            }
            else
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                return $"Order skapades INTE. Felmeddelande: {errorResponse.ErrorMessage}";
            }

        }
    }
}
