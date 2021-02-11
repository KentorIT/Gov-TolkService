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
    public class RequestController : Controller
    {
        private readonly IHubContext<WebHooksHub> _hubContext;
        private readonly BrokerMockOptions _options;
        private readonly ApiCallService _apiService;
        private readonly IMemoryCache _cache;
        private readonly static HttpClient client = new HttpClient(GetCertHandler());

        private readonly object clientLock = new object();

        public RequestController(IHubContext<WebHooksHub> hubContext, IOptions<BrokerMockOptions> options, ApiCallService apiService, IMemoryCache cache)
        {
            _hubContext = hubContext;
            _options = options.Value;
            _apiService = apiService;
            _cache = cache;
            if (_options.UseApiKey)
            {
                lock (clientLock)
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

        #region incomming

        [HttpPost]
        public async Task<JsonResult> CreatedToOther([FromBody] RequestModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: TILL ANDRA FÖRMEDLINGEN Boknings-ID: {payload.OrderNumber} skapad av {payload.CustomerInformation.Name} organisationsnummer {payload.CustomerInformation.OrganisationNumber} i {payload.Region}");
            }
            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> Created([FromBody] RequestModel payload)
        {
            Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-ApiKey", out var apiKey);
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Boknings-ID: {payload.OrderNumber} skapad av {payload.CustomerInformation.Name} organisationsnummer {payload.CustomerInformation.OrganisationNumber} i {payload.Region}");
            }
            if (_cache.Get<List<ListItemResponse>>("LocationTypes") == null)
            {
                await _apiService.GetAllLists();
            }
            var interpreters = _cache.Get<List<InterpreterDetailsModel>>("BrokerInterpreters");
            var extraInstructions = GetExtraInstructions(payload.Description);

            if (extraInstructions.Contains("SLEEP20"))
            {
                Thread.Sleep(20000);
            }

            if (extraInstructions.Contains("VIEWUNAUTHORIZED"))
            {
                await _apiService.CallRequestViewUnauthorized(payload.OrderNumber);
            }

            if (extraInstructions.Contains("THROW"))
            {
                throw new Exception();
            }
            if (extraInstructions.Contains("VIEW"))
            {
                var req = await _apiService.GetOrderRequest(payload.OrderNumber);
            }

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
                        var interpreter = interpreters?.FirstOrDefault();
                        DateTimeOffset? latestAnswerAt = null;
                        decimal? expectedTravelCosts = null;
                        if (interpreter == null || extraInstructions.Contains("NEWINTERPRETER"))
                        {
                            interpreter = GetNewInterpreter();
                        }
                        if (extraInstructions.Contains("SETLATESTANSWERAT"))
                        {
                            latestAnswerAt = payload.StartAt.AddMinutes(-60);
                            expectedTravelCosts = 300;
                        }
                        if (extraInstructions.Contains("BADLOCATION"))
                        {
                            var badLocation = _cache.Get<List<ListItemResponse>>("LocationTypes").First(l => !payload.Locations.Any(pl => pl.Key == l.Key)).Key;
                            //Find a location that is not present in payload

                            await AssignInterpreter(
                                payload.OrderNumber,
                                interpreter,
                                badLocation,
                                payload.CompetenceLevels.OrderBy(c => c.Rank).FirstOrDefault()?.Key ?? _cache.Get<List<ListItemResponse>>("CompetenceLevels").First(c => c.Key != "no_interpreter").Key,
                                payload.Requirements.Select(r => new RequirementAnswerModel
                                {
                                    Answer = "Japp",
                                    CanMeetRequirement = true,
                                    RequirementId = r.RequirementId
                                }),
                                expectedTravelCosts,
                                latestAnswerAt
                            );
                        }
                        else
                        {
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
                                }),
                                expectedTravelCosts,
                                latestAnswerAt
                            );
                        }
                    }
                    if (extraInstructions.Contains("CHANGEINTERPRETERONCREATE"))
                    {

                        Thread.Sleep(3000);
                        await ChangeInterpreter(
                            payload.OrderNumber,
                            interpreters.Last(),
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

            if (extraInstructions.Contains("CREATEINTERPRETER"))
            {
                await CreateInterpreter(new InterpreterDetailsModel(GetNewInterpreter()));
            }
            if (extraInstructions.Contains("UPDATEINTERPRETER"))
            {
                await _apiService.GetInterpreters();
                interpreters = _cache.Get<List<InterpreterDetailsModel>>("BrokerInterpreters");
                await UpdateInterpreter(interpreters.OrderBy(i => i.FirstName).First());
            }
            if (extraInstructions.Contains("INACTIVATEINTERPRETER"))
            {
                await _apiService.GetInterpreters();
                interpreters = _cache.Get<List<InterpreterDetailsModel>>("BrokerInterpreters");
                var interpreter = interpreters.Where(i => i.IsActive).OrderBy(i => i.LastName).First();
                await ToggleIfInterpreterIsActive(interpreter);
            }
            if (extraInstructions.Contains("ACTIVATEINTERPRETER"))
            {
                await _apiService.GetInterpreters();
                interpreters = _cache.Get<List<InterpreterDetailsModel>>("BrokerInterpreters");
                var interpreter = interpreters.Where(i => !i.IsActive).OrderBy(i => i.LastName).FirstOrDefault();
                if (interpreter == null)
                {
                    interpreter = interpreters.Where(i => i.IsActive).OrderBy(i => i.LastName).First();
                    interpreter = await ToggleIfInterpreterIsActive(interpreter);
                }
                await ToggleIfInterpreterIsActive(interpreter);
            }
            if (extraInstructions.Contains("VIEWINTERPRETER"))
            {
                await _apiService.GetInterpreters();
                interpreters = _cache.Get<List<InterpreterDetailsModel>>("BrokerInterpreters");
                var interpreter = interpreters.Where(i => i.OfficialInterpreterId != null).OrderBy(i => i.Email).First();
                await _apiService.GetInterpreter(interpreter.InterpreterId.Value);
                await _apiService.GetInterpreter(interpreter.OfficialInterpreterId);
            }

            if (extraInstructions.Contains("GETFILE"))
            {
                await GetFile(payload.OrderNumber, payload.Attachments.First().AttachmentId);
            }
            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> GroupCreated([FromBody] RequestGroupModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Sammanhållen Boknings-ID: {payload.OrderGroupNumber} skapad av {payload.CustomerInformation.Name} i {payload.Region}");
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
                    await AcknowledgeGroup(payload.OrderGroupNumber);
                    if (extraInstructions.Contains("ONLYACKNOWLEDGE"))
                    {
                        return new JsonResult("Success");
                    }
                }
                if (extraInstructions.Contains("ACKNOWLEDGESINGLEOCCASION"))
                {
                    await Acknowledge(payload.Occasions.First().OrderNumber);
                }

                else if (extraInstructions.Contains("DECLINESINGLEOCCASION"))
                {
                    await Decline(payload.Occasions.First().OrderNumber, "Nix pix i rutan");
                }
                else if (extraInstructions.Contains("DECLINE"))
                {
                    await DeclineGroup(payload.OrderGroupNumber, "Vill inte, kan inte bör inte...");
                }
                else if (extraInstructions.Contains("VIEW"))
                {
                    await ViewGroup(payload.OrderGroupNumber);
                }
                else
                {
                    var declineExtraInterpreter = extraInstructions.Contains("DECLINEEXTRAINTERPRETER");
                    var interpreter = _cache.Get<List<InterpreterDetailsModel>>("BrokerInterpreters")?.FirstOrDefault();
                    InterpreterModel extraInterpreter = null;
                    if (payload.Occasions.Any(o => !string.IsNullOrEmpty(o.IsExtraInterpreterForOrderNumber)))
                    {
                        extraInterpreter = _cache.Get<List<InterpreterDetailsModel>>("BrokerInterpreters")?.LastOrDefault();
                    }
                    if (interpreter == null || extraInstructions.Contains("NEWINTERPRETER"))
                    {
                        interpreter = GetNewInterpreter();
                    }
                    if (extraInstructions.Contains("BADLOCATION"))
                    {
                        var badLocation = _cache.Get<List<ListItemResponse>>("LocationTypes").First(l => !payload.Locations.Any(pl => pl.Key == l.Key)).Key;
                        //Find a location that is not present in payload

                        await AnswerGroup(
                            payload.OrderGroupNumber,
                            interpreter,
                            extraInterpreter,
                            badLocation,
                            payload.CompetenceLevels.OrderBy(c => c.Rank).FirstOrDefault()?.Key ?? _cache.Get<List<ListItemResponse>>("CompetenceLevels").First(c => c.Key != "no_interpreter").Key,
                            payload.Requirements.Select(r => new RequirementAnswerModel
                            {
                                Answer = "Japp",
                                CanMeetRequirement = true,
                                RequirementId = r.RequirementId
                            }),
                            declineExtraInterpreter
                        );
                    }
                    else
                    {
                        if (extraInstructions.Contains("ANSWERSINGLEOCCASION"))
                        {
                            //This should not be allowed
                            await AssignInterpreter(
                                payload.Occasions.First().OrderNumber,
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
                        else
                        {
                            await AnswerGroup(
                                payload.OrderGroupNumber,
                                interpreter,
                                extraInterpreter,
                                payload.Locations.First().Key,
                                payload.CompetenceLevels.OrderBy(c => c.Rank).FirstOrDefault()?.Key ?? _cache.Get<List<ListItemResponse>>("CompetenceLevels").First(c => c.Key != "no_interpreter").Key,
                                payload.Requirements.Select(r => new RequirementAnswerModel
                                {
                                    Answer = "Japp",
                                    CanMeetRequirement = true,
                                    RequirementId = r.RequirementId
                                }),
                                declineExtraInterpreter,
                                extraInstructions.Contains("ADDTRAVELCOSTS") ? 100 : 0
                            );
                        }
                    }
                }
                //Get the headers:
                //X-Kammarkollegiet-InterpreterService-Delivery

            }
            if (extraInstructions.Contains("GETFILE"))
            {
                await GetGroupFile(payload.OrderGroupNumber, payload.Attachments.First().AttachmentId);
            }
            return new JsonResult("Success");
        }

        public async Task<JsonResult> ReplacementCreated([FromBody] RequestReplacementCreatedModel payload)
        {
            var originalRequest = payload.OriginalRequest;
            var replacementRequest = payload.ReplacementRequest;
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Boknings-ID: {originalRequest.OrderNumber} har ersatts av {replacementRequest.OrderNumber}");
            }
            if (_cache.Get<List<ListItemResponse>>("LocationTypes") == null)
            {
                await _apiService.GetAllLists();
            }
            var extraInstructions = GetExtraInstructions(replacementRequest.Description);

            if (!extraInstructions.Contains("LEAVEUNACKNOWLEDGED"))
            {
                if (extraInstructions.Contains("ACKNOWLEDGE") || extraInstructions.Contains("ONLYACKNOWLEDGE"))
                {
                    await Acknowledge(replacementRequest.OrderNumber);
                }
                if (extraInstructions.Contains("DECLINE"))
                {
                    await Decline(replacementRequest.OrderNumber, "Vill inte, kan inte bör inte ens på en ersättning...");
                }
                else
                {
                    if (!extraInstructions.Contains("ONLYACKNOWLEDGE"))
                    {
                        await AcceptReplacement(replacementRequest.OrderNumber, replacementRequest.Locations.First().Key);
                    }
                }
                //Get the headers:
                //X-Kammarkollegiet-InterpreterService-Delivery
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

            var request = await _apiService.GetOrderRequest(payload.OrderNumber);

            var extraInstructions = GetExtraInstructions(request.Description);
            if (extraInstructions.Contains("CHANGEINTERPRETERONAPPROVE"))
            {
                await ChangeInterpreter(
                    payload.OrderNumber,
                    _cache.Get<List<InterpreterDetailsModel>>("BrokerInterpreters").Last(),
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
            if (extraInstructions.Contains("CANCELONAPPROVE"))
            {
                await Cancel(payload.OrderNumber);
            }

            return new JsonResult("Success");
        }
        [HttpPost]
        public async Task<JsonResult> GroupAnswerApproved([FromBody] RequestGroupAnswerApprovedModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Sammanhållen bokning med Boknings-ID: {payload.OrderGroupNumber} har blivit godkänd");
            }

            var requestGroup = await _apiService.GetOrderGroupRequest(payload.OrderGroupNumber);

            var extraInstructions = GetExtraInstructions(requestGroup.Description);
            if (extraInstructions.Contains("GETFIRSTOCCASION"))
            {
                var request = await _apiService.GetOrderRequest(requestGroup.Occasions.First().OrderNumber);
                if (request != null)
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/View]::Boknings-ID {request.OrderNumber}, tillhör Sammanhållen bokning {payload.OrderGroupNumber}. ");
                }
            }

            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> CancelledByCustomer([FromBody] RequestCancelledByCustomerModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Boknings-ID: {payload.OrderNumber} har blivit avbokad, med meddelande: '{payload.Message}'");
                await ConfirmCancellation(payload.OrderNumber);
            }

            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> GroupCancelledByCustomer([FromBody] RequestGroupCancelledByCustomerModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Boknings-ID: {payload.OrderGroupNumber} har blivit avbokad, med meddelande: '{payload.Message}'");
                await ConfirmGroupCancellation(payload.OrderGroupNumber);
            }

            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> AnswerDenied([FromBody] RequestAnswerDeniedModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Svaret på Boknings-ID: {payload.OrderNumber} har nekats, med meddelande: '{payload.Message}'");
                await ConfirmDenial(payload.OrderNumber);
            }

            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> GroupAnswerDenied([FromBody] RequestGroupAnswerDeniedModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Svaret på sammanhållen Boknings-ID: {payload.OrderGroupNumber} har nekats, med meddelande: '{payload.Message}'");
                await ConfirmGroupDenial(payload.OrderGroupNumber);
            }

            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> Updated([FromBody] RequestUpdatedModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Informationen på Boknings-ID: {payload.OrderNumber} har uppdaterats.");
                await ConfirmUpdate(payload.OrderNumber);
            }
            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> ChangedInterpreterAccepted([FromBody] RequestChangedInterpreterAcceptedModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Tolkändringen på Boknings-ID: {payload.OrderNumber} har accepterats");
            }

            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> RequestLostDueToInactivity([FromBody] RequestLostDueToInactivityModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Boknings-ID: {payload.OrderNumber} har gått vidare till nästa förmedling.");
            }

            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> RequestGroupLostDueToInactivity([FromBody] RequestGroupLostDueToInactivityModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Sammanhållen Boknings-ID: {payload.OrderGroupNumber} har gått vidare till nästa förmedling.");
            }

            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> RequestNoAnswerFromCustomer([FromBody] RequestLostDueToNoAnswerFromCustomerModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Boknings-ID: {payload.OrderNumber} har inte besvarats av myndighet inom utsatt tid.");
                await ConfirmNoAnswer(payload.OrderNumber);
            }
            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> RequestGroupNoAnswerFromCustomer([FromBody] RequestGroupLostDueToNoAnswerFromCustomerModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Sammanhållen Boknings-ID: {payload.OrderGroupNumber} har inte besvarats av myndighet inom utsatt tid.");
                await ConfirmGroupNoAnswer(payload.OrderGroupNumber);
            }
            return new JsonResult("Success");
        }

        [HttpPost]
        public async Task<JsonResult> RequestAssignmentTimePassed([FromBody] RequestCompletedModel payload)
        {
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-Event", out var type))
            {
                await _hubContext.Clients.All.SendAsync("IncommingCall", $"[{type.ToString()}]:: Boknings-ID: {payload.OrderNumber} Tiden för det tillsatta tolkuppdraget har passerat, det finns nu möjlighet att registrera rekvisition alternativt att arkivera bokningen.");
                await ConfirmNoRequisition(payload.OrderNumber);
            }

            return new JsonResult("Success");
        }
        #endregion

        #region private methods

        private async Task<bool> CreateInterpreter(InterpreterDetailsModel payload)
        {
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("Interpreter/Create"), content))
                {
                    CreateInterpreterResponse responseInterpreter = response.Content.ReadAsAsync<CreateInterpreterResponse>().Result;
                    if (responseInterpreter.Success)
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Interpreter/Create]:: Tolk {responseInterpreter.Interpreter.Email} skapades med detta id: {responseInterpreter.Interpreter.InterpreterId}");
                    }
                    else
                    {
                        var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Interpreter/Create] FAILED:: Tolk {payload.Email} kunde inte skapas, med detta fel: {errorResponse.ErrorMessage}");
                    }
                }

                return true;
            }
        }

        private async Task<bool> UpdateInterpreter(InterpreterDetailsModel payload)
        {
            var originalFirstName = payload.FirstName;
            payload.FirstName = $"Ö{payload.FirstName}";
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("Interpreter/Update"), content))
                {
                    UpdateInterpreterResponse responseInterpreter = response.Content.ReadAsAsync<UpdateInterpreterResponse>().Result;
                    if (responseInterpreter.Success)
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Interpreter/Update]:: Tolk {responseInterpreter.Interpreter.Email} förnamnet ändrades från: {originalFirstName} till: {responseInterpreter.Interpreter.FirstName}");
                    }
                    else
                    {
                        var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Interpreter/Update] FAILED:: Tolk {payload.Email} förnamnet kunde inte ändras, med följande fel: {errorResponse.ErrorMessage}");
                    }
                }

                return true;
            }
        }

        private async Task<InterpreterDetailsModel> ToggleIfInterpreterIsActive(InterpreterDetailsModel payload)
        {
            payload.IsActive = !payload.IsActive;
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("Interpreter/Update"), content))
                {
                    UpdateInterpreterResponse responseInterpreter = response.Content.ReadAsAsync<UpdateInterpreterResponse>().Result;
                    if (responseInterpreter.Success)
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Interpreter/Update]:: Tolk {responseInterpreter.Interpreter.Email} {(payload.IsActive ? "aktiverades" : "inaktiverades")}");
                        return responseInterpreter.Interpreter;
                    }
                    else
                    {
                        var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Interpreter/Update] FAILED:: Tolk {payload.Email} kunde inte : {errorResponse.ErrorMessage}");
                        return null;
                    }
                }

            }
        }

        private static InterpreterDetailsModel GetNewInterpreter()
        {
            var newName = DateTime.Now.ToSwedishString("yyyyMMdd-fff");
            return new InterpreterDetailsModel
            {
                Email = $"{newName}@new.guy",
                FirstName = newName,
                LastName = "Goy",
                PhoneNumber = "12121345",
                InterpreterInformationType = EnumHelper.GetCustomName(InterpreterInformationType.NewInterpreter)
            };
        }

        private static IEnumerable<string> GetExtraInstructions(string description)
        {
            if (string.IsNullOrEmpty(description))
            {
                return Enumerable.Empty<string>();
            }
            return description.ToSwedishUpper().Split(";", StringSplitOptions.RemoveEmptyEntries).AsEnumerable();
        }

        private async Task<bool> AssignInterpreter(string orderNumber, InterpreterModel interpreter, string location, string competenceLevel, IEnumerable<RequirementAnswerModel> requirementAnswers, decimal? expectedTravelCosts = null, DateTimeOffset? latestAnswerAt = null)
        {
            var payload = new RequestAnswerModel
            {
                OrderNumber = orderNumber,
                Interpreter = interpreter,
                Location = location,
                CompetenceLevel = competenceLevel,
                ExpectedTravelCosts = expectedTravelCosts,
                CallingUser = "regular-user@formedling1.se",
                RequirementAnswers = requirementAnswers,
                LatestAnswerTimeForCustomer = latestAnswerAt
            };
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("Request/Answer"), content))
                {
                    var answer = response.Content.ReadAsAsync<AnswerResponse>().Result;
                    if (answer.Success)
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/Answer]:: Boknings-ID: {orderNumber} skickad tolk: {interpreter.Email}, och fick tillbaka id: {answer.InterpreterId}");
                    }
                    else
                    {
                        var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/Answer] FAILED:: Boknings-ID: {orderNumber} skickad tolk: {interpreter.Email} ErrorMessage: {errorResponse.ErrorMessage}");
                    }
                }

                return true;
            }
        }

        private async Task<bool> AnswerGroup(string orderGroupNumber, InterpreterModel interpreter, InterpreterModel extraInterpreter, string location, string competenceLevel, IEnumerable<RequirementAnswerModel> requirementAnswers, bool declineExtranInterpreter = false, decimal expectedTravelCosts = 0)
        {
            var payload = new RequestGroupAnswerModel
            {
                OrderGroupNumber = orderGroupNumber,
                CallingUser = "regular-user@formedling1.se",
                InterpreterLocation = location,
                InterpreterAnswer = new InterpreterGroupAnswerModel
                {
                    Accepted = true,
                    Interpreter = interpreter,
                    CompetenceLevel = competenceLevel,
                    ExpectedTravelCosts = expectedTravelCosts,
                    RequirementAnswers = requirementAnswers
                },
                ExtraInterpreterAnswer = !declineExtranInterpreter ? new InterpreterGroupAnswerModel
                {
                    Accepted = true,
                    Interpreter = extraInterpreter,
                    CompetenceLevel = competenceLevel,
                    ExpectedTravelCosts = expectedTravelCosts,
                    RequirementAnswers = requirementAnswers
                } : new InterpreterGroupAnswerModel { Accepted = false, DeclineMessage = "Det är svårt för att lösa det, helt enkelt." },
            };
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("RequestGroup/Answer"), content))
                {
                    var answer = response.Content.ReadAsAsync<GroupAnswerResponse>().Result;
                    if (answer.Success)
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[RequestGroup/Answer]:: Sammanhållen Boknings-ID: {orderGroupNumber} skickad tolk: {interpreter.Email}, och fick tillbaka id: {answer.InterpreterId}. {(answer.ExtraInterpreterId.HasValue ? $"Extra tolk id: {answer.ExtraInterpreterId}" : string.Empty)}");
                    }
                    else
                    {
                        var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[RequestGroup/Answer] FAILED:: Sammanhållen Boknings-ID: {orderGroupNumber} skickad tolk: {interpreter.Email} ErrorMessage: {errorResponse.ErrorMessage}");
                    }
                }

                return true;
            }
        }

        private async Task<bool> AcceptReplacement(string orderNumber, string location)
        {
            var payload = new RequestAcceptReplacementModel
            {
                OrderNumber = orderNumber,
                Location = location,
                ExpectedTravelCosts = 0,
                CallingUser = "regular-user@formedling1.se",
            };
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("Request/AcceptReplacement"), content))
                {
                    if ((await response.Content.ReadAsAsync<ResponseBase>()).Success)
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/AcceptReplacement]:: Boknings-ID: {orderNumber} ersättning har accepterats");
                    }
                    else
                    {
                        var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/AcceptReplacement] FAILED:: Boknings-ID: {orderNumber} ersättning skulle accepteras ErrorMessage: {errorResponse.ErrorMessage}");
                    }
                }

                return true;
            }
        }

        private async Task<bool> Acknowledge(string orderNumber)
        {
            var payload = new RequestAcknowledgeModel
            {
                OrderNumber = orderNumber,
                CallingUser = "regular-user@formedling1.se"
            };
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("Request/Acknowledge"), content))
                {
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
        }

        private async Task<bool> ConfirmDenial(string orderNumber)
        {
            var payload = new ConfirmDenialModel
            {
                OrderNumber = orderNumber,
                CallingUser = "regular-user@formedling1.se"
            };
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("Request/ConfirmDenial"), content))
                {
                    if (response.Content.ReadAsAsync<ResponseBase>().Result.Success)
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/ConfirmDenial]:: Boknings-ID: {orderNumber} accat nekande");
                    }
                    else
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/ConfirmDenial] FAILED:: Boknings-ID: {orderNumber} accat nekande");
                    }
                }

                return true;
            }
        }

        private async Task<bool> ConfirmGroupDenial(string orderGroupNumber)
        {
            var payload = new ConfirmGroupDenialModel
            {
                OrderGroupNumber = orderGroupNumber,
                CallingUser = "regular-user@formedling1.se"
            };
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("RequestGroup/ConfirmDenial"), content))
                {
                    if (response.Content.ReadAsAsync<ResponseBase>().Result.Success)
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[RequestGroup/ConfirmDenial]:: Boknings-ID: {orderGroupNumber} accat nekande");
                    }
                    else
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[RequestGroup/ConfirmDenial] FAILED:: Boknings-ID: {orderGroupNumber} accat nekande");
                    }
                }

                return true;
            }
        }

        private async Task<bool> ConfirmGroupCancellation(string orderGroupNumber)
        {
            var payload = new ConfirmGroupCancellationModel
            {
                OrderGroupNumber = orderGroupNumber,
                CallingUser = "regular-user@formedling1.se"
            };
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("RequestGroup/ConfirmCancellation"), content))
                {
                    if (response.Content.ReadAsAsync<ResponseBase>().Result.Success)
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[RequestGroup/ConfirmCancellation]:: Boknings-ID: {orderGroupNumber} konfirmerat gruppavbokning");
                    }
                    else
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[RequestGroup/ConfirmCancellation] FAILED:: Boknings-ID: {orderGroupNumber} konfirmerat gruppavbokning");
                    }
                }

                return true;
            }
        }

        private async Task<bool> ConfirmCancellation(string orderNumber)
        {
            var payload = new ConfirmDenialModel
            {
                OrderNumber = orderNumber,
                CallingUser = "regular-user@formedling1.se"
            };
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("Request/ConfirmCancellation"), content))
                {
                    if (response.Content.ReadAsAsync<ResponseBase>().Result.Success)
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/ConfirmCancellation]:: Boknings-ID: {orderNumber} konfirmerat avbokning");
                    }
                    else
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/ConfirmCancellation] FAILED:: Boknings-ID: {orderNumber} konfirmerat avbokning");
                    }
                }

                return true;
            }
        }

        private async Task<bool> ConfirmNoAnswer(string orderNumber)
        {
            var payload = new ConfirmNoAnswerModel
            {
                OrderNumber = orderNumber,
                CallingUser = "regular-user@formedling1.se"
            };
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("Request/ConfirmNoAnswer"), content))
                {
                    if (response.Content.ReadAsAsync<ResponseBase>().Result.Success)
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/ConfirmNoAnswer]:: Boknings-ID: {orderNumber} tagit del av obesvarad förfrågan");
                    }
                    else
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/ConfirmNoAnswer] FAILED:: Boknings-ID: {orderNumber} tagit del av obesvarad förfrågan");
                    }
                }
                return true;
            }
        }

        private async Task<bool> ConfirmUpdate(string orderNumber)
        {
            var payload = new ConfirmUpdateModel
            {
                OrderNumber = orderNumber,
                CallingUser = "regular-user@formedling1.se"
            };
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("Request/ConfirmUpdate"), content))
                {
                    if (response.Content.ReadAsAsync<ResponseBase>().Result.Success)
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/ConfirmUpdate]:: Boknings-ID: {orderNumber} tagit del av ändrad förfrågan");
                    }
                    else
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/ConfirmUpdate] FAILED:: Boknings-ID: {orderNumber} tagit del av ändrad förfrågan");
                    }
                }
                return true;
            }
        }

        private async Task<bool> ConfirmGroupNoAnswer(string orderGroupNumber)
        {
            var payload = new ConfirmGroupNoAnswerModel
            {
                OrderGroupNumber = orderGroupNumber,
                CallingUser = "regular-user@formedling1.se"
            };
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("RequestGroup/ConfirmNoAnswer"), content))
                {
                    if (response.Content.ReadAsAsync<ResponseBase>().Result.Success)
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[RequestGroup/ConfirmNoAnswer]:: Boknings-ID: {orderGroupNumber} tagit del av obesvarad sammanhållen förfrågan");
                    }
                    else
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[RequestGroup/ConfirmNoAnswer] FAILED:: Boknings-ID: {orderGroupNumber} tagit del av obesvarad sammanhållen förfrågan");
                    }
                }
                return true;
            }
        }

        private async Task<bool> ConfirmNoRequisition(string orderNumber)
        {
            var payload = new ConfirmNoRequisitionModel
            {
                OrderNumber = orderNumber,
                CallingUser = "regular-user@formedling1.se"
            };
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("Request/ConfirmNoRequisition"), content))
                {
                    if (response.Content.ReadAsAsync<ResponseBase>().Result.Success)
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/ConfirmNoRequisition]:: Boknings-ID: {orderNumber} arkiverad utan rekvisition");
                    }
                    else
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/ConfirmNoRequisition] FAILED:: Boknings-ID: {orderNumber} arkiverad utan rekvisitionn");
                    }
                }
                return true;
            }
        }

        private async Task<bool> AcknowledgeGroup(string orderGroupNumber)
        {
            var payload = new RequestGroupAcknowledgeModel
            {
                OrderGroupNumber = orderGroupNumber,
                CallingUser = "regular-user@formedling1.se"
            };
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("RequestGroup/Acknowledge"), content))
                {
                    if (response.Content.ReadAsAsync<ResponseBase>().Result.Success)
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[RequestGroup/Acknowledge]:: Sammanhållen Boknings-ID: {orderGroupNumber} accat mottagande");
                    }
                    else
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[RequestGroup/Acknowledge] FAILED:: Sammanhållen Boknings-ID: {orderGroupNumber} accat mottagande");
                    }
                }

                return true;
            }
        }

        private async Task<bool> Decline(string orderNumber, string message)
        {
            var payload = new RequestDeclineModel
            {
                OrderNumber = orderNumber,
                CallingUser = "regular-user@formedling1.se",
                Message = message
            };
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("Request/Decline"), content))
                {
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
        }

        private async Task<bool> DeclineGroup(string orderGroupNumber, string message)
        {
            var payload = new RequestGroupDeclineModel
            {
                OrderGroupNumber = orderGroupNumber,
                CallingUser = "regular-user@formedling1.se",
                Message = message
            };
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("RequestGroup/Decline"), content))
                {
                    if (response.Content.ReadAsAsync<ResponseBase>().Result.Success)
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[RequestGroup/Decline]:: Sammanhållen Boknings-ID: {orderGroupNumber} Svarat nej på förfrågan");
                    }
                    else
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[RequestGroup/Decline] FAILED:: Sammanhållen Boknings-ID: {orderGroupNumber} Svarat nej på förfrågan");
                    }
                }

                return true;
            }
        }

        private async Task<bool> ViewGroup(string orderGroupNumber)
        {
            using (var response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("RequestGroup/View", $"OrderGroupNumber={orderGroupNumber}&callingUser=regular-user@formedling1.se")))
            {
                if (response.Content.ReadAsAsync<ResponseBase>().Result.Success)
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[RequestGroup/View]:: Sammanhållen Boknings-ID: {orderGroupNumber} hämtat förfrågan");
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[RequestGroup/View] FAILED:: Sammanhållen Boknings-ID: {orderGroupNumber} hämtat förfrågan");
                }
            }
            return true;
        }

        private async Task<bool> GetFile(string orderNumber, int attachmentId)
        {
            using (var response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("Request/File", $"OrderNumber={orderNumber}&AttachmentId={attachmentId}&callingUser=regular-user@formedling1.se")))
            {
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

        private async Task<bool> GetGroupFile(string orderGroupNumber, int attachmentId)
        {
            using (var response = await client.GetAsync(_options.TolkApiBaseUrl.BuildUri("RequestGroup/File", $"OrderGroupNumber={orderGroupNumber}&AttachmentId={attachmentId}&callingUser=regular-user@formedling1.se")))
            {
                var file = response.Content.ReadAsAsync<FileResponse>().Result;
                if (file.Success)
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[RequestGroup/File]:: Boknings-ID: {orderGroupNumber} fil hämtad. Base64 stäng var {file.FileBase64.Length} tecken lång");
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[RequestGroup/File] FAILED:: Boknings-ID: {orderGroupNumber} accat mottagande");
                }
            }

            return true;
        }

        private async Task<bool> ChangeInterpreter(string orderNumber, InterpreterModel interpreter, string location, string competenceLevel, IEnumerable<RequirementAnswerModel> requirementAnswers)
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
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("Request/ChangeInterpreter"), content))
                {
                    var answer = response.Content.ReadAsAsync<ChangeInterpreterResponse>().Result;
                    if (answer.Success)
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/ChangeInterpreter]:: Boknings-ID: {orderNumber} ändrat tolk: {interpreter.Email}, och fick tillbaka id: {answer.InterpreterId}");
                    }
                    else
                    {
                        var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/ChangeInterpreter] FAILED:: Boknings-ID: {orderNumber} skickade tolk: {interpreter.Email} ErrorMessage: {errorResponse.ErrorMessage}");
                    }
                }

                return true;
            }
        }

        private async Task<bool> Cancel(string orderNumber)
        {
            var payload = new RequestCancelModel
            {
                OrderNumber = orderNumber,
                CallingUser = "regular-user@formedling1.se",
                Message = "Cancelled at hello"
            };
            using (var content = new StringContent(JsonConvert.SerializeObject(payload, Formatting.Indented), Encoding.UTF8, "application/json"))
            {
                using (var response = await client.PostAsync(_options.TolkApiBaseUrl.BuildUri("Request/Cancel"), content))
                {
                    if (response.Content.ReadAsAsync<ResponseBase>().Result.Success)
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/Cancel]:: Boknings-ID: {orderNumber} avbokat från förmedling");
                    }
                    else
                    {
                        await _hubContext.Clients.All.SendAsync("OutgoingCall", $"[Request/Cancel] FAILED:: Boknings-ID: {orderNumber} avbokat från förmedling");
                    }
                }

                return true;
            }
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
