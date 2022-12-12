using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;
using Tolk.Web.Services;

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.Broker)]
    public class RequestController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly IAuthorizationService _authorizationService;
        private readonly PriceCalculationService _priceCalculationService;
        private readonly DateCalculationService _dateCalculationService;
        private readonly ILogger _logger;
        private readonly TolkOptions _options;
        private readonly RequestService _requestService;
        private readonly InterpreterService _interpreterService;
        private readonly ListToModelService _listToModelService;
        private readonly EventLogService _eventLogService;
        private readonly CacheService _cacheService;

        public RequestController(
            TolkDbContext dbContext,
            ISwedishClock clock,
            IAuthorizationService authorizationService,
            PriceCalculationService priceCalculationService,
            DateCalculationService dateCalculationService,
            ILogger<RequestController> logger,
            IOptions<TolkOptions> options,
            RequestService requestService,
            InterpreterService interpreterService,
            ListToModelService listToModelService,
            EventLogService eventLogService,
            CacheService cacheService)
        {
            _dbContext = dbContext;
            _clock = clock;
            _authorizationService = authorizationService;
            _priceCalculationService = priceCalculationService;
            _dateCalculationService = dateCalculationService;
            _logger = logger;
            _options = options.Value;
            _requestService = requestService;
            _interpreterService = interpreterService;
            _listToModelService = listToModelService;
            _eventLogService = eventLogService;
            _cacheService = cacheService;
        }

        public IActionResult List()
        {
            return View(new RequestListModel { FilterModel = new RequestFilterModel() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ListRequests(IDataTablesRequest request)
        {
            var model = new RequestFilterModel();
            await TryUpdateModelAsync(model);

            var requests = _dbContext.RequestListRows.Where(r => r.BrokerId == User.GetBrokerId()).Select(o => o);
            return AjaxDataTableHelper.GetData(request, requests.Count(), model.Apply(requests), r => r.SelectRequestListItemModel());
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public JsonResult ListColumnDefinition()
        {
            return Json(AjaxDataTableHelper.GetColumnDefinitions<RequestListItemModel>());
        }

        public async Task<IActionResult> View(int id)
        {
            var request = await _dbContext.Requests.GetRequestById(id);
            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded)
            {
                //if user tries to view a request with a status in negotiationState ReplacedByOtherEntity - redirect to latest request for broker
                if (EnumHelper.Parent<RequestStatus, NegotiationState>(request.Status) == NegotiationState.ReplacedByOtherEntity)
                {
                    id = _dbContext.Requests.OrderBy(r => r.RequestId).Last(r => r.OrderId == request.OrderId && r.Ranking.BrokerId == User.GetBrokerId()).RequestId;
                    return RedirectToAction(nameof(View), new { id });
                }
                if (request.IsToBeProcessedByBroker)
                {
                    if (request.ReplacingRequestId.HasValue)
                    {
                        return RedirectToAction(nameof(ProcessReplacement), new { id });
                    }
                    else
                    {
                        return RedirectToAction(nameof(Process), new { id });
                    }
                }
                return View(await GetModelForView(request));
            }
            return Forbid();
        }

        public async Task<IActionResult> Process(int id)
        {
            var request = await _dbContext.Requests.GetRequestById(id);
            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
            {
                if (!request.IsToBeProcessedByBroker)
                {
                    _logger.LogWarning("Wrong status when trying to process request. Status: {request.Status}, RequestId: {request.RequestId}", request.Status, request.RequestId);
                    return RedirectToAction(nameof(View), new { id });
                }
                if (request.ReplacingRequestId.HasValue)
                {
                    _logger.LogWarning("A replacing request should not be handled by {Method}. RequestId: {request.RequestId}", nameof(Process), request.RequestId);
                    return RedirectToAction(nameof(View), new { id });
                }

                if (request.Status == RequestStatus.Created)
                {
                    _requestService.Acknowledge(request, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    await _dbContext.SaveChangesAsync();
                }

                RequestModel model = await GetModel(request);
                model.FileGroupKey = new Guid();
                model.CombinedMaxSizeAttachments = _options.CombinedMaxSizeAttachments;
                model.ExpectedTravelCosts = null;
                model.AllowProcessing = !request.RequestGroupId.HasValue;
                model.AllowAccept = request.CanAccept;
                model.FullAnswer = !request.CanAccept;
                model.OrderViewModel.UseAttachments = true;
                return View(model);
            }
            return Forbid();
        }

        public async Task<IActionResult> ProcessReplacement(int id)
        {
            var request = await _dbContext.Requests.GetRequestById(id);
            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
            {
                if (!request.IsToBeProcessedByBroker)
                {
                    _logger.LogWarning("Wrong status when trying to process replacement request. Status: {request.Status}, RequestId: {request.RequestId}", request.Status, request.RequestId);
                    return RedirectToAction(nameof(View), new { id });
                }
                if (!request.ReplacingRequestId.HasValue)
                {
                    _logger.LogWarning("Cannot process this Request as a replacement request, RequestId: {request.RequestId}", request.Status, request.RequestId);
                    return RedirectToAction(nameof(View), new { id });
                }
                if (request.Status == RequestStatus.Created)
                {
                    _requestService.Acknowledge(request, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    await _dbContext.SaveChangesAsync();
                }

                RequestModel model = await GetModel(request);
                model.FileGroupKey = new Guid();
                model.CombinedMaxSizeAttachments = _options.CombinedMaxSizeAttachments;
                model.ExpectedTravelCosts = null;
                model.ProcessReplacementRequestViewModel = RequestViewModel.GetModelFromRequest(request, request.Order.AllowExceedingTravelCost);
                model.ProcessReplacementRequestViewModel.LanguageAndDialect = model.OrderViewModel.LanguageAndDialect;
                model.ProcessReplacementRequestViewModel.RegionName = model.OrderViewModel.RegionName;
                model.ProcessReplacementRequestViewModel.TimeRange = model.OrderViewModel.TimeRange;
                model.ProcessReplacementRequestViewModel.DisplayMealBreakIncluded = model.OrderViewModel.DisplayMealBreakIncludedText;
                model.ProcessReplacementRequestViewModel.IsReplacingOrderRequest = true;
                model.ProcessReplacementRequestViewModel.RequirementAnswers = await RequestRequirementAnswerModel.GetFromList(_dbContext.OrderRequirementRequestAnswer.GetRequirementAnswersForRequest(request.RequestId));
                model.OrderViewModel.UseAttachments = true;
                return View(model);
            }
            return Forbid();
        }

        public async Task<IActionResult> ChangeInterpreter(int id)
        {
            var request = await _dbContext.Requests.GetRequestForAcceptById(id);
            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded && request.CanChangeInterpreter(_clock.SwedenNow))
            {
                RequestModel model = await GetModel(request);
                model.OldInterpreterId = request.InterpreterBrokerId;
                model.OtherInterpreterId = await _requestService.GetOtherInterpreterIdForSameOccasion(request);
                model.FileGroupKey = new Guid();
                model.CombinedMaxSizeAttachments = _options.CombinedMaxSizeAttachments;
                model.LatestAnswerTimeForCustomer = null;
                return View(model);
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ChangeInterpreter(RequestAnswerModel model)
        {
            if (ModelState.IsValid)
            {
                var request = await _dbContext.Requests.GetRequestForAcceptById(model.RequestId);

                if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
                {
                    if (!request.CanChangeInterpreter(_clock.SwedenNow))
                    {
                        return RedirectToAction("Index", "Home", new { ErrorMessage = "Det gick inte att byta tolk, kontrollera tiden för uppdragsstart" });
                    }
                    var requirementAnswers = model.RequiredRequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                    {
                        OrderRequirementId = ra.OrderRequirementId,
                        Answer = ra.Answer,
                        CanSatisfyRequirement = ra.CanMeetRequirement
                    }).ToList();
                    requirementAnswers.AddRange(model.DesiredRequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                    {
                        OrderRequirementId = ra.OrderRequirementId,
                        Answer = ra.Answer,
                        CanSatisfyRequirement = ra.CanMeetRequirement
                    }).ToList());
                    try
                    {
                        //if user can choose between phone/video and an interpreter location with travel, user might have set costs, LatestAnswerTimeForCustomer etc
                        if (model.InterpreterLocation == InterpreterLocation.OffSitePhone || model.InterpreterLocation == InterpreterLocation.OffSiteVideo)
                        {
                            model.LatestAnswerTimeForCustomer = null;
                            model.ExpectedTravelCostInfo = null;
                            model.ExpectedTravelCosts = null;
                        }
                        var interpreter = await _interpreterService.GetInterpreter(model.InterpreterId.Value, model.GetNewInterpreterInformation(), request.Ranking.BrokerId);
                        try
                        {
                            await _requestService.ChangeInterpreter(
                                request,
                                _clock.SwedenNow,
                                User.GetUserId(),
                                User.TryGetImpersonatorId(),
                                interpreter,
                                model.InterpreterLocation.Value,
                                model.InterpreterCompetenceLevel.Value,
                                requirementAnswers,
                                model.Files?.Select(f => new RequestAttachment { AttachmentId = f.Id }) ?? Enumerable.Empty<RequestAttachment>(),
                                model.ExpectedTravelCosts,
                                model.ExpectedTravelCostInfo,
                                (model.SetLatestAnswerTimeForCustomer != null && EnumHelper.Parse<TrueFalse>(model.SetLatestAnswerTimeForCustomer.SelectedItem.Value) == TrueFalse.Yes) ? model.LatestAnswerTimeForCustomer : null,
                                model.BrokerReferenceNumber
                            );
                        }
                        catch (InvalidOperationException ex)
                        {
                            _logger.LogError("Change Interpreter for request {model.RequestId} failed. Message: {ex.Message}", model.RequestId, ex.Message);
                            return RedirectToAction("Index", "Home", new { errormessage = ex.Message });
                        }
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogError("Change interpreter for request {model.RequestId} failed. Message: {ex.Message}", model.RequestId, ex.Message);
                        return RedirectToAction("Index", "Home", new { errormessage = "Något gick fel i behandlingen av tolkbytet" });
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogError("Change interpreter for request {model.RequestId} failed. Message: {ex.Message}", model.RequestId, ex.Message);
                        return RedirectToAction("Index", "Home", new { errormessage = ex.Message });
                    }
                    return RedirectToAction("Index", "Home", new { message = "Tolk har bytts ut för uppdraget" });
                }
                return Forbid();
            }
            return RedirectToAction(nameof(ChangeInterpreter), new { id = model.RequestId });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ProcessReplacement(RequestAnswerModel model)
        {
            if (ModelState.IsValid)
            {
                var request = await _dbContext.Requests.GetRequestForAcceptById(model.RequestId);

                if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded && !request.ReplacingRequestId.HasValue)
                {
                    if (!request.IsToBeProcessedByBroker)
                    {
                        return RedirectToAction("Index", "Home", new { ErrorMessage = "Förfrågan är redan behandlad" });
                    }
                    var requirementAnswers = model.RequiredRequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                    {
                        RequestId = request.RequestId,
                        OrderRequirementId = ra.OrderRequirementId,
                        Answer = ra.Answer,
                        CanSatisfyRequirement = ra.CanMeetRequirement
                    }).ToList();
                    requirementAnswers.AddRange(model.DesiredRequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                    {
                        RequestId = request.RequestId,
                        OrderRequirementId = ra.OrderRequirementId,
                        Answer = ra.Answer,
                        CanSatisfyRequirement = ra.CanMeetRequirement
                    }).ToList());
                    try
                    {
                        //if user can choose between phone/video and an interpreter location with travel, user might have set costs, LatestAnswerTimeForCustomer etc
                        if (model.InterpreterLocation == InterpreterLocation.OffSitePhone || model.InterpreterLocation == InterpreterLocation.OffSiteVideo)
                        {
                            model.LatestAnswerTimeForCustomer = null;
                            model.ExpectedTravelCostInfo = null;
                            model.ExpectedTravelCosts = null;
                        }
                        await _requestService.AcceptReplacement(
                             request,
                             _clock.SwedenNow,
                             User.GetUserId(),
                             User.TryGetImpersonatorId(),
                             model.InterpreterLocation.Value,
                             model.ExpectedTravelCosts,
                             model.ExpectedTravelCostInfo,
                             (model.SetLatestAnswerTimeForCustomer != null && EnumHelper.Parse<TrueFalse>(model.SetLatestAnswerTimeForCustomer.SelectedItem.Value) == TrueFalse.Yes) ? model.LatestAnswerTimeForCustomer : null,
                             model.BrokerReferenceNumber
                         );
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogError("Accept replacement for request {model.RequestId} failed. Message: {ex.Message}", model.RequestId, ex.Message);
                        return RedirectToAction("Index", "Home", new { errormessage = "Något gick fel i behandlingen av ersättningsuppdraget" });
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogError("Accept replacement for request {model.RequestId} failed. Message: {ex.Message}", model.RequestId, ex.Message);
                        return RedirectToAction("Index", "Home", new { errormessage = ex.Message });
                    }
                    return RedirectToAction("Index", "Home", new { message = "Svar har skickats" });
                }
                return Forbid();
            }
            return RedirectToAction(nameof(ProcessReplacement), new { id = model.RequestId });

        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Answer(RequestAnswerModel model)
        {
            if (ModelState.IsValid)
            {
                var request = await _dbContext.Requests.GetRequestForAcceptById(model.RequestId);

                if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
                {
                    if (!request.IsToBeProcessedByBroker)
                    {
                        return RedirectToAction("Index", "Home", new { ErrorMessage = "Förfrågan är redan behandlad" });
                    }
                    var requirementAnswers = model.RequiredRequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                    {
                        RequestId = request.RequestId,
                        OrderRequirementId = ra.OrderRequirementId,
                        Answer = ra.Answer,
                        CanSatisfyRequirement = ra.CanMeetRequirement
                    }).ToList();
                    requirementAnswers.AddRange(model.DesiredRequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                    {
                        RequestId = request.RequestId,
                        OrderRequirementId = ra.OrderRequirementId,
                        Answer = ra.Answer,
                        CanSatisfyRequirement = ra.CanMeetRequirement
                    }).ToList());
                    try
                    {
                        //if user can choose between phone/video and an interpreter location with travel, user might have set costs, LatestAnswerTimeForCustomer etc
                        if (model.InterpreterLocation == InterpreterLocation.OffSitePhone || model.InterpreterLocation == InterpreterLocation.OffSiteVideo)
                        {
                            model.LatestAnswerTimeForCustomer = null;
                            model.ExpectedTravelCostInfo = null;
                            model.ExpectedTravelCosts = null;
                        }
                        var interpreter = await _interpreterService.GetInterpreter(model.InterpreterId.Value, model.GetNewInterpreterInformation(), request.Ranking.BrokerId);
                        await _requestService.Answer(
                            request,
                            _clock.SwedenNow,
                            User.GetUserId(),
                            User.TryGetImpersonatorId(),
                            interpreter,
                            model.InterpreterLocation.Value,
                            model.InterpreterCompetenceLevel.Value,
                            requirementAnswers,
                            model.Files?.Select(f => new RequestAttachment { AttachmentId = f.Id }).ToList(),
                            model.ExpectedTravelCosts,
                            model.ExpectedTravelCostInfo,
                            (model.SetLatestAnswerTimeForCustomer != null && EnumHelper.Parse<TrueFalse>(model.SetLatestAnswerTimeForCustomer.SelectedItem.Value) == TrueFalse.Yes) ? model.LatestAnswerTimeForCustomer : null,
                            model.BrokerReferenceNumber
                        );
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogError("Answer for request {model.RequestId} failed. Message: {ex.Message}", model.RequestId, ex.Message);
                        return RedirectToAction("Index", "Home", new { errormessage = "Något gick fel i behandlingen av bokningsförfrågan" });
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogError("Answer for request {model.RequestId} failed. Message: {ex.Message}", model.RequestId, ex.Message);
                        return RedirectToAction("Index", "Home", new { errormessage = ex.Message });
                    }
                    return RedirectToAction("Index", "Home", new { message = model.Status == RequestStatus.AcceptedNewInterpreterAppointed ? "Tolk har bytts ut för uppdraget" : "Svar har skickats" });
                }
                return Forbid();
            }
            return RedirectToAction(nameof(Process), new { id = model.RequestId });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Accept(RequestAcceptModel model)
        {
            if (ModelState.IsValid)
            {
                var request = await _dbContext.Requests.GetRequestForAcceptById(model.RequestId);

                if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
                {
                    if (!request.CanAccept)
                    {
                        return RedirectToAction("Index", "Home", new { ErrorMessage = "Förfrågan är redan behandlad" });
                    }
                    var requirementAnswers = model.RequiredRequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                    {
                        RequestId = request.RequestId,
                        OrderRequirementId = ra.OrderRequirementId,
                        Answer = ra.Answer,
                        CanSatisfyRequirement = ra.CanMeetRequirement
                    }).ToList();
                    requirementAnswers.AddRange(model.DesiredRequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                    {
                        RequestId = request.RequestId,
                        OrderRequirementId = ra.OrderRequirementId,
                        Answer = ra.Answer,
                        CanSatisfyRequirement = ra.CanMeetRequirement
                    }).ToList());
                    try
                    {

                        await _requestService.Accept(
                            request,
                            _clock.SwedenNow,
                            User.GetUserId(),
                            User.TryGetImpersonatorId(),
                            model.InterpreterLocationOnAccept,
                            model.InterpreterCompetenceLevelOnAccept,
                            requirementAnswers,
                            model.Files?.Select(f => new RequestAttachment { AttachmentId = f.Id }).ToList(),
                            model.BrokerReferenceNumber
                        );
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogError("Accept for request {model.RequestId} failed. Message: {ex.Message}", model.RequestId, ex.Message);
                        return RedirectToAction("Index", "Home", new { errormessage = "Något gick fel i behandlingen av bokningsförfrågan" });
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogError("Accept for request {model.RequestId} failed. Message: {ex.Message}", model.RequestId, ex.Message);
                        return RedirectToAction("Index", "Home", new { errormessage = ex.Message });
                    }
                    return RedirectToAction("Index", "Home", new { message = "Bekräftelse har skickats" });
                }
                return Forbid();
            }
            return RedirectToAction(nameof(Process), new { id = model.RequestId });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Cancel(RequestCancelModel model)
        {
            if (ModelState.IsValid)
            {
                var request = await _dbContext.Requests.GetRequestWithContactsById(model.RequestId);
                if (request == null)
                {
                    return RedirectToAction("Index", "Home", new { ErrorMessage = "Tillfället kunde inte avbokas" });
                }
                if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Cancel)).Succeeded && request != null)
                {
                    try
                    {
                        _requestService.CancelByBroker(request, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.CancelMessage);
                        _dbContext.SaveChanges();
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogError("Cancel by broker Request {request.Status}, RequestId: {request.RequestId}. Message: {ex.Message}", request.Status, request.RequestId, ex.Message);
                        return RedirectToAction("Index", "Home", new { errormessage = "Det gick inte att avboka uppdraget" });
                    }
                    return RedirectToAction("Index", "Home", new { message = "Avbokning har genomförts" });
                }
                return Forbid();
            }
            return RedirectToAction(nameof(View), new { id = model.RequestId });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Decline(RequestDeclineModel model)
        {
            var request = await _dbContext.Requests.GetRequestWithContactsById(model.DeniedRequestId);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
            {
                try
                {
                    await _requestService.Decline(request, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.DenyMessage);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError("Decline for request {model.DeniedRequestId} failed. Message: {ex.Message}", model.DeniedRequestId, ex.Message);
                    return RedirectToAction("Index", "Home", new { errormessage = ex.Message });
                }
                _dbContext.SaveChanges();
                return RedirectToAction("Index", "Home", new { message = "Svar har skickats" });
            }

            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ConfirmCancellation(int requestId)
        {
            Request request = await GetConfirmedRequest(requestId);
            if (request.Status == RequestStatus.CancelledByCreatorWhenApproved && (await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded)
            {
                try
                {
                    await _requestService.ConfirmCancellation(request, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    return RedirectToAction("Index", "Home", new { message = "Avbokning är bekräftad" });
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError("ConfirmCancellation for request {requestId} failed. Message: {ex.Message}", requestId, ex.Message);
                    return RedirectToAction("Index", "Home", new { errormessage = ex.Message });
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ConfirmDenial(int requestId)
        {
            Request request = await GetConfirmedRequest(requestId);
            if (request.Status == RequestStatus.DeniedByCreator && (await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded)
            {
                try
                {
                    await _requestService.ConfirmDenial(request, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    return RedirectToAction("Index", "Home", new { message = "Bokningsförfrågan arkiverad" });
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError("ConfirmDenial for request {requestId} failed. Message: {ex.Message}", requestId, ex.Message);
                    return RedirectToAction("Index", "Home", new { errormessage = ex.Message });
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ConfirmNoAnswer(int requestId)
        {
            Request request = await GetConfirmedRequest(requestId);
            if (request.Status == RequestStatus.ResponseNotAnsweredByCreator && (await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded)
            {
                try
                {
                    await _requestService.ConfirmNoAnswer(request, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    return RedirectToAction("Index", "Home", new { message = "Bokningsförfrågan arkiverad" });
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError("ConfirmNoAnswer for request {requestId} failed. Message: {ex.Message}", requestId, ex.Message);
                    return RedirectToAction("Index", "Home", new { errormessage = ex.Message });
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ConfirmNoRequisition(int requestId)
        {
            Request request = await GetConfirmedRequest(requestId);
            if (request.Status == RequestStatus.Approved && (await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded)
            {
                try
                {
                    await _requestService.ConfirmNoRequisition(request, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    return RedirectToAction("Index", "Home", new { message = "Bokningsförfrågan arkiverad utan rekvisition" });
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError("ConfirmNoRequisition for request {requestId} failed. Message: {ex.Message}", requestId, ex.Message);
                    return RedirectToAction("Index", "Home", new { errormessage = ex.Message });
                }
            }
            return Forbid();
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmOrderChange(ConfirmOrderChangeModel model)
        {
            Request request = await GetOrderChangedRequest(model.RequestId);
            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded)
            {
                if (request.Status == RequestStatus.Approved || request.Status == RequestStatus.AcceptedNewInterpreterAppointed)
                {
                    try
                    {
                        await _requestService.ConfirmOrderChange(request, model.ConfirmedOrderChangeLogEntries, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                        return RedirectToAction("Index", "Home", new { message = "Bokningsändringar bekräftade" });
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogError("ConfirmOrderChange for request {model.RequestId} failed. Message: {ex.Message}", model.RequestId, ex.Message);
                        return RedirectToAction("Index", "Home", new { errormessage = ex.Message });
                    }
                }
                return RedirectToAction("Index", "Home", new { errormessage = "Det gick inte att bekräfta bokningsändringarna" });
            }
            return Forbid();
        }

        public async Task DeleteRequestView(int id)
        {
            var requestViews = _dbContext.RequestViews
                .Where(r => r.RequestId == id && r.ViewedBy == User.GetUserId());
            if (requestViews.Any())
            {
                _dbContext.RequestViews.RemoveRange(requestViews);
                await _dbContext.SaveChangesAsync();
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public JsonResult AddRequestView(int requestId)
        {
            var request = _dbContext.Requests.Single(r => r.RequestId == requestId);
            request.RequestViews = _dbContext.RequestViews.GetRequestViewsForRequest(request.RequestId).ToList();
            if (request != null)
            {
                request.AddRequestView(User.GetUserId(), User.TryGetImpersonatorId(), _clock.SwedenNow);
                _dbContext.SaveChanges();
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> GetEventLog(int id)
        {
            var request = await _dbContext.Requests.GetRequestById(id);
            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded)
            {
                return PartialView("_EventLogDynamic", new EventLogModel
                {
                    Entries = (await _eventLogService.GetEventLogForRequestsOnOrder(request.OrderId, request.Order.CustomerOrganisation.Name, request.Ranking.Broker.Name, User.GetBrokerId())).OrderBy(e => e.Timestamp).ThenBy(e => e.Weight).ToList()
                });
            }
            return Forbid();
        }

        private async Task<Request> GetConfirmedRequest(int requestId)
        {
            var request = await _dbContext.Requests.GetSimpleRequestById(requestId);
            request.RequestStatusConfirmations = await _dbContext.RequestStatusConfirmation.GetStatusConfirmationsForRequest(request.RequestId).ToListAsync();
            return request;
        }

        private async Task<Request> GetOrderChangedRequest(int requestId)
        {
            var request = await _dbContext.Requests.GetSimpleRequestById(requestId);
            request.Order.OrderChangeLogEntries = await _dbContext.OrderChangeLogEntries.GetOrderChangeLogEntitesForOrder(request.OrderId).ToListAsync();
            return request;
        }

        private async Task<RequestModel> GetModel(Request request)
        {
            request.Order = await _dbContext.Orders.GetFullOrderByRequestId(request.RequestId);
            request.Order.Requirements = await _dbContext.OrderRequirements.GetRequirementsForOrder(request.OrderId).ToListAsync();
            var model = RequestModel.GetModelFromRequest(request);
            if (request.InterpreterLocation != null)
            {
                model.InterpreterLocationAnswer = model.OrderViewModel.InterpreterLocationAnswer = (InterpreterLocation)request.InterpreterLocation.Value;
            }
            if (request.Status == RequestStatus.CancelledByCreatorWhenApproved)
            {
                model.Info48HCancelledByCustomer = _dateCalculationService.GetNoOf24HsPeriodsWorkDaysBetween(request.CancelledAt.Value.DateTime, request.Order.StartAt.DateTime) < 2 ? "Detta är en avbokning som skett med mindre än 48 timmar till tolkuppdragets start. Därmed utgår full ersättning, inklusive bland annat spilltid och förmedlingsavgift, i de fall något ersättningsuppdrag inte kan ordnas av kund. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna." : "Detta är en avbokning som skett med mer än 48 timmar till tolkuppdragets start. Därmed utgår förmedlingsavgift till leverantören. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna.";
            }
            model.ViewedByUser = await _listToModelService.GetOtherViewer(request.RequestId, User.GetUserId());
            model.BrokerId = request.Ranking.BrokerId;
            model.OrderViewModel.ActiveRequest = new RequestViewModel
            {
                Status = request.Status,
                CreatedAt = model.CreatedAt
            };
            model.OrderViewModel.UseAttachments = true;
            await _listToModelService.AddInformationFromListsToModel(model.OrderViewModel);
            model.OrderCalculatedPriceInformationModel = GetPriceinformationOrderToDisplay(request, model.OrderViewModel.RequestedCompetenceLevels);
            model.AttachmentListModel = model.OrderViewModel.RequestAttachmentListModel;
            model.OrderViewModel.CustomerUseSelfInvoicingInterpreter = _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == request.Order.CustomerOrganisationId && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseSelfInvoicingInterpreter));
            model.BrokerReferenceNumber = request.BrokerReferenceNumber;
            return model;
        }

        private async Task<OrderViewModel> GetModelForView(Request request)
        {
            var order = await _dbContext.Orders.GetFullOrderByRequestId(request.RequestId);
            var model = OrderViewModel.GetModelFromOrder(order, request, true, true);
            model.StartAtIsInFuture = order.StartAt > _clock.SwedenNow;
            model.UserCanCanCreateRequisition = _authorizationService.AuthorizeAsync(User, request, Policies.Edit).Result.Succeeded;
            model.UserCanCancelRequest = _authorizationService.AuthorizeAsync(User, request, Policies.Cancel).Result.Succeeded;

            model.ActiveRequest = RequestViewModel.GetModelFromRequest(request, order.AllowExceedingTravelCost);
            model.ActiveRequest.DisplayMealBreakIncluded = order.MealBreakTextToDisplay;

            if (request.Status == RequestStatus.CancelledByCreatorWhenApproved)
            {
                model.ActiveRequest.Info48HCancelledByCustomer = _dateCalculationService.GetNoOf24HsPeriodsWorkDaysBetween(request.CancelledAt.Value.DateTime, request.Order.StartAt.DateTime) < 2 ? "Detta är en avbokning som skett med mindre än 48 timmar till tolkuppdragets start. Därmed utgår full ersättning, inklusive bland annat spilltid och förmedlingsavgift, i de fall något ersättningsuppdrag inte kan ordnas av kund. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna." : "Detta är en avbokning som skett med mer än 48 timmar till tolkuppdragets start. Därmed utgår förmedlingsavgift till leverantören. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna.";
            }
            model.ViewedByUser = await _listToModelService.GetOtherViewer(request.RequestId, User.GetUserId());

            model.ActiveRequest.AllowInterpreterChange = request.CanChangeInterpreter(_clock.SwedenNow);
            model.ActiveRequest.RegionName = model.RegionName;
            model.ActiveRequest.TimeRange = model.TimeRange;
            model.ActiveRequest.DisplayMealBreakIncluded = model.DisplayMealBreakIncludedText;
            model.ActiveRequest.IsCancelled = model.Status == OrderStatus.CancelledByCreator || model.Status == OrderStatus.CancelledByBroker;
            model.CustomerUseSelfInvoicingInterpreter = _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == request.Order.CustomerOrganisationId && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseSelfInvoicingInterpreter));
            //LISTS
            model.UseAttachments = true;
            await _listToModelService.AddInformationFromListsToModel(model);
            model.ActiveRequest.LanguageAndDialect = model.LanguageAndDialect;
            model.ActiveRequest.AttachmentListModel = model.RequestAttachmentListModel;
            model.ActiveRequest.RequestCalculatedPriceInformationModel = model.ActiveRequestPriceInformationModel;
            model.OrderCalculatedPriceInformationModel = GetPriceinformationOrderToDisplay(request, model.RequestedCompetenceLevels);
            model.FrameworkAgreementNumberOnCreated = request?.Ranking.FrameworkAgreement.AgreementNumber;
            model.EventLog = new EventLogModel
            {
                Header = "Bokningshändelser",
                Id = "EventLog_Request",
                DynamicLoadPath = $"Request/{nameof(GetEventLog)}/{request.RequestId}",
            };
            return model;
        }

        private PriceInformationModel GetPriceinformationOrderToDisplay(Request request, List<CompetenceAndSpecialistLevel> requestedCompetenceLevels)
        {
            return new PriceInformationModel
            {
                MealBreakIsNotDetucted = request.Order.MealBreakIncluded ?? false,
                PriceInformationToDisplay = PriceCalculationService.GetPriceInformationToDisplay(
                    _priceCalculationService.GetPrices(request, OrderService.SelectCompetenceLevelForPriceEstimation(requestedCompetenceLevels), null, null).PriceRows),
                Header = "Beräknat pris enligt bokningsförfrågan",
                UseDisplayHideInfo = true
            };
        }
    }
}
