﻿using BrokerMock.Helpers;
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
    public class RequestController : Controller
    {
        private readonly IHubContext<WebHooksHub> _hubContext;
        private readonly BrokerMockOptions _options;
        private readonly ApiCallService _apiService;
        private readonly IMemoryCache _cache;
        public RequestController(IHubContext<WebHooksHub> hubContext, IOptions<BrokerMockOptions> options, ApiCallService apiService, IMemoryCache cache)
        {
            _hubContext = hubContext;
            _options = options.Value;
            _apiService = apiService;
            _cache = cache;
        }

        [HttpPost]
        public async Task<JsonResult> Created([FromBody] RequestModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Boknings-ID: {payload.OrderNumber} skapad av {payload.Customer} i {payload.Region}");
            }
            if (_cache.Get<List<ListItemResponse>>("LocationTypes") == null)
            {
                await _apiService.GetAllLists();
            }
            var extraInstructions = GetExtraInstructions(payload.Description);

            if (!extraInstructions.Contains("LEAVEUNACKNOWLEDGED"))
            {
                if (extraInstructions.Contains("ACKNOWLEDGE") || extraInstructions.Contains("ONLYACKNOWLEDGE"))
                {
                    await Acknowledge(payload.OrderNumber);
                }
                if (extraInstructions.Contains("DECLINE"))
                {
                    await Decline(payload.OrderNumber, "Vill inte, kan inte bör inte...");

                }
                else
                {
                    if (!extraInstructions.Contains("ONLYACKNOWLEDGE"))
                    {
                        var interpreter = _cache.Get<List<InterpreterModel>>("BrokerInterpreters")?.FirstOrDefault();

                        if (interpreter == null || extraInstructions.Contains("NEWINTERPRETER"))
                        {
                            interpreter = new InterpreterModel
                            {
                                Email = "newguy@new.guy",
                                FirstName = "New",
                                LastName = "Goy",
                                PhoneNumber = "12121345",
                                InterpreterInformationType = EnumHelper.GetCustomName(InterpreterInformationType.NewInterpreter)
                            };
                        }
                        await AssignInterpreter(
                            payload.OrderNumber,
                            interpreter,
                            payload.Locations.First().Key,
                            payload.CompetenceLevels.OrderBy(c => c.Rank).FirstOrDefault()?.Key ?? _cache.Get<List<ListItemResponse>>("CompetenceLevels").First(c => c.Key != "no_interpreter").Key,
                            payload.Requirements.Select(r => new RequirementAnswerModel
                            {
                                Answer = "Japp",
                                CanMeetRequirement = true,
                                RequirementId = r.RequirementId
                            })
                        );
                    }
                    if (extraInstructions.Contains("CHANGEINTERPRETERONCREATE"))
                    {

                        Thread.Sleep(3000);
                        await ChangeInterpreter(
                            payload.OrderNumber,
                            _cache.Get<List<InterpreterModel>>("BrokerInterpreters").Last(),
                            payload.Locations.Last().Key,
                            payload.CompetenceLevels.OrderBy(c => c.Rank).FirstOrDefault()?.Key ?? _cache.Get<List<ListItemResponse>>("CompetenceLevels").First(c => c.Key != "no_interpreter").Key,
                            payload.Requirements.Select(r => new RequirementAnswerModel
                            {
                                Answer = "Japp",
                                CanMeetRequirement = true,
                                RequirementId = r.RequirementId
                            })
                        );
                    }
                }
                //Get the headers:
                //X-Kammarkollegiet-InterpreterService-Delivery
            }

            if (extraInstructions.Contains("GETFILE"))
            {
                await GetFile(payload.OrderNumber, payload.Attachments.First().AttachmentId);
            }
            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> Approved([FromBody] RequestAnswerApprovedModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Boknings-ID: {payload.OrderNumber} har blivit godkänd");
            }

            var request = await GetOrderRequest(payload.OrderNumber);

            var extraInstructions = GetExtraInstructions(request.Description);
            if (extraInstructions.Contains("CHANGEINTERPRETERONAPPROVE"))
            {
                await ChangeInterpreter(
                    payload.OrderNumber,
                    _cache.Get<List<InterpreterModel>>("BrokerInterpreters").Last(),
                    request.Locations.Last().Key,
                    request.CompetenceLevels.OrderBy(c => c.Rank).FirstOrDefault()?.Key ?? _cache.Get<List<ListItemResponse>>("CompetenceLevels").First(c => c.Key != "no_interpreter").Key,
                    request.Requirements.Select(r => new RequirementAnswerModel
                    {
                        Answer = "Japp",
                        CanMeetRequirement = true,
                        RequirementId = r.RequirementId
                    })
                );
            }

            return new JsonResult("Success");
        }

        private IEnumerable<string> GetExtraInstructions(string description)
        {
            if (string.IsNullOrEmpty(description))
            {
                return Enumerable.Empty<string>();
            }
            return description.ToUpper().Split(";", StringSplitOptions.RemoveEmptyEntries).AsEnumerable();
        }

        private async Task<RequestDetailsResponse> GetOrderRequest(string orderNumber)
        {
            using (var client = GetHttpClient())
            {
                var payload = new RequestGetDetailsModel { OrderNumber = orderNumber };
                var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json");
                var response = await client.GetAsync($"{_options.TolkApiBaseUrl}/Request/View?orderNumber=" + orderNumber);
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
        }

        private async Task<bool> AssignInterpreter(string orderNumber, InterpreterModel interpreter, string location, string competenceLevel, IEnumerable<RequirementAnswerModel> requirementAnswers)
        {
            using (var client = GetHttpClient())
            {
                var payload = new RequestAnswerModel
                {
                    OrderNumber = orderNumber,
                    Interpreter = interpreter,
                    Location = location,
                    CompetenceLevel = competenceLevel,
                    ExpectedTravelCosts = 0,
                    CallingUser = "regular-user@formedling1.se",
                    RequirementAnswers = requirementAnswers
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_options.TolkApiBaseUrl}/Request/Answer", content);
                if ((await response.Content.ReadAsAsync<ResponseBase>()).Success)
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/Accept]:: Boknings-ID: {orderNumber} skickad tolk: {interpreter}");
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/Accept] FAILED:: Boknings-ID: {orderNumber} skickad tolk: {interpreter} ErrorMessage: {errorResponse.ErrorMessage}");
                }
            }

            return true;
        }

        private async Task<bool> Acknowledge(string orderNumber)
        {
            using (var client = GetHttpClient())
            {
                var payload = new RequestAcknowledgeModel
                {
                    OrderNumber = orderNumber,
                    CallingUser = "regular-user@formedling1.se"
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_options.TolkApiBaseUrl}/Request/Acknowledge", content);
                if (response.Content.ReadAsAsync<ResponseBase>().Result.Success)
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/Acknowledge]:: Boknings-ID: {orderNumber} accat mottagande");
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/Acknowledge] FAILED:: Boknings-ID: {orderNumber} accat mottagande");
                }
            }

            return true;
        }

        private async Task<bool> Decline(string orderNumber, string message)
        {
            using (var client = GetHttpClient())
            {
                var payload = new RequestDeclineModel
                {
                    OrderNumber = orderNumber,
                    CallingUser = "regular-user@formedling1.se",
                    Message = message
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_options.TolkApiBaseUrl}/Request/Decline", content);
                if (response.Content.ReadAsAsync<ResponseBase>().Result.Success)
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/Decline]:: Boknings-ID: {orderNumber} Svarat nej på förfrågan");
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/Decline] FAILED:: Boknings-ID: {orderNumber} Svarat nej på förfrågan");
                }
            }

            return true;
        }

        private async Task<bool> GetFile(string orderNumber, int attachmentId)
        {
            using (var client = GetHttpClient())
            {
                var response = await client.GetAsync($"{_options.TolkApiBaseUrl}/Request/File?OrderNumber={orderNumber}&AttachmentId={ attachmentId}");
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

        private async Task<bool> ChangeInterpreter(string orderNumber, InterpreterModel interpreter, string location, string competenceLevel, IEnumerable<RequirementAnswerModel> requirementAnswers)
        {
            using (var client = GetHttpClient())
            {
                var payload = new RequestAnswerModel
                {
                    OrderNumber = orderNumber,
                    Interpreter = interpreter,
                    Location = location,
                    CompetenceLevel = competenceLevel,
                    ExpectedTravelCosts = 0,
                    CallingUser = "regular-user@formedling1.se",
                    RequirementAnswers = requirementAnswers
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_options.TolkApiBaseUrl}/Request/ChangeInterpreter", content);
                if (response.Content.ReadAsAsync<ResponseBase>().Result.Success)
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/ChangeInterpreter]:: Boknings-ID: {orderNumber} ändrat tolk: {interpreter}");
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/ChangeInterpreter] FAILED:: Boknings-ID: {orderNumber} ändrat tolk: {interpreter}");
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
