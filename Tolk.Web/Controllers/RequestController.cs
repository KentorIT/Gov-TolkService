using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Utilities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

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

        public RequestController(
            TolkDbContext dbContext,
            ISwedishClock clock,
            IAuthorizationService authorizationService,
            PriceCalculationService priceCalculationService,
            DateCalculationService dateCalculationService,
            ILogger<RequestController> logger,
            IOptions<TolkOptions> options,
            RequestService requestService,
            InterpreterService interpreterService
        )
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
        }

        public IActionResult List()
        {
            return View(new RequestListModel { FilterModel = new RequestFilterModel() });
        }

        [HttpPost]
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
            var request = await GetRequestToView(id);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded)
            {
                //if user tries to view a request with status InterpreterReplaced (email-link) - redirect to latest request for broker
                if (request.Status == RequestStatus.InterpreterReplaced)
                {
                    id = _dbContext.Requests.OrderBy(r => r.RequestId).Last(r => r.OrderId == request.OrderId && r.Ranking.BrokerId == User.GetBrokerId()).RequestId;
                    return RedirectToAction(nameof(View), new { id });
                }
                if (request.IsToBeProcessedByBroker)
                {
                    return RedirectToAction(nameof(Process), new { id });
                }
                return View(GetModel(request, true));
            }
            return Forbid();
        }

        public async Task<IActionResult> Process(int id)
        {
            var request = await GetRequestToProcess(id);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
            {
                if (!request.IsToBeProcessedByBroker)
                {
                    _logger.LogWarning("Wrong status when trying to process request. Status: {request.Status}, RequestId: {request.RequestId}", request.Status, request.RequestId);
                    return RedirectToAction(nameof(View), new { id });
                }
                if (request.Status == RequestStatus.Created)
                {
                    _requestService.Acknowledge(request, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    await _dbContext.SaveChangesAsync();
                }

                RequestModel model = GetModel(request);
                model.FileGroupKey = new Guid();
                model.CombinedMaxSizeAttachments = _options.CombinedMaxSizeAttachments;
                model.ExpectedTravelCosts = null;
                model.AllowProcessing = !request.RequestGroupId.HasValue;
                return View(model);
            }
            return Forbid();
        }

        public async Task<IActionResult> Change(int id)
        {
            var request = await GetRequestToProcess(id);
            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded && request.CanChangeInterpreter(_clock.SwedenNow))
            {
                RequestModel model = GetModel(request);
                model.Status = RequestStatus.AcceptedNewInterpreterAppointed;
                model.OldInterpreterId = request.InterpreterBrokerId;
                model.OtherInterpreterId = _requestService.GetOtherInterpreterIdForSameOccasion(request);
                model.FileGroupKey = new Guid();
                model.CombinedMaxSizeAttachments = _options.CombinedMaxSizeAttachments;
                model.LatestAnswerTimeForCustomer = null;
                return View("Process", model);
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Accept(RequestAcceptModel model)
        {
            if (ModelState.IsValid)
            {
                var request = _dbContext.Requests
                    .Include(r => r.RequestGroup)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerUnit)
                    .Include(r => r.Order).ThenInclude(o => o.Requests).ThenInclude(r => r.PriceRows)
                    .Include(r => r.Order).ThenInclude(o => o.CompetenceRequirements)
                    .Include(r => r.Order).ThenInclude(o => o.Requirements)
                    .Include(r => r.Order).ThenInclude(o => o.InterpreterLocations)
                    .Include(r => r.Order).ThenInclude(o => o.CreatedByUser)
                    .Include(r => r.Order).ThenInclude(o => o.ContactPersonUser)
                    .Include(r => r.Order).ThenInclude(o => o.Language)
                    .Include(r => r.Order).ThenInclude(o => o.IsExtraInterpreterForOrder).ThenInclude(r => r.Requests)
                    .Include(r => r.Order).ThenInclude(o => o.ExtraInterpreterOrder).ThenInclude(r => r.Requests)
                    .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder).ThenInclude(r => r.Requests)
                    .Include(r => r.Interpreter)
                    .Include(r => r.RequirementAnswers)
                    .Include(r => r.PriceRows)
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Single(o => o.RequestId == model.RequestId);

                if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
                {
                    if (!request.IsToBeProcessedByBroker && model.Status != RequestStatus.AcceptedNewInterpreterAppointed)
                    {
                        return RedirectToAction("Index", "Home", new { ErrorMessage = "Förfrågan är redan behandlad" });
                    }
                    else if (model.Status == RequestStatus.AcceptedNewInterpreterAppointed && !request.CanChangeInterpreter(_clock.SwedenNow))
                    {
                        return RedirectToAction("Index", "Home", new { ErrorMessage = "Det gick inte att byta tolk, kontrollera tiden för uppdragsstart" });
                    }
                    var requirementAnswers = model.RequiredRequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                    {
                        RequestId = model.Status == RequestStatus.AcceptedNewInterpreterAppointed ? 0 : request.RequestId,
                        OrderRequirementId = ra.OrderRequirementId,
                        Answer = ra.Answer,
                        CanSatisfyRequirement = ra.CanMeetRequirement
                    }).ToList();
                    requirementAnswers.AddRange(model.DesiredRequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                    {
                        RequestId = model.Status == RequestStatus.AcceptedNewInterpreterAppointed ? 0 : request.RequestId,
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
                        //if change interpreter or normal accept (no replacementorder)
                        if (model.Status == RequestStatus.AcceptedNewInterpreterAppointed || (!request.Order.ReplacingOrderId.HasValue && model.Status != RequestStatus.AcceptedNewInterpreterAppointed))
                        {
                            var interpreter = await _interpreterService.GetInterpreter(model.InterpreterId.Value, model.GetNewInterpreterInformation(), request.Ranking.BrokerId);
                            if (model.Status == RequestStatus.AcceptedNewInterpreterAppointed)
                            {
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
                                        (model.SetLatestAnswerTimeForCustomer != null && EnumHelper.Parse<TrueFalse>(model.SetLatestAnswerTimeForCustomer.SelectedItem.Value) == TrueFalse.Yes) ? model.LatestAnswerTimeForCustomer : null
                                    );
                                }
                                catch (InvalidOperationException ex)
                                {
                                    return RedirectToAction("Index", "Home", new { errormessage = ex.Message });
                                }
                            }
                            else
                            {
                                await _requestService.Accept(
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
                                    (model.SetLatestAnswerTimeForCustomer != null && EnumHelper.Parse<TrueFalse>(model.SetLatestAnswerTimeForCustomer.SelectedItem.Value) == TrueFalse.Yes) ? model.LatestAnswerTimeForCustomer : null
                                );
                            }
                        }
                        else
                        {
                            _requestService.AcceptReplacement(
                                request,
                                _clock.SwedenNow,
                                User.GetUserId(),
                                User.TryGetImpersonatorId(),
                                model.InterpreterLocation.Value,
                                model.ExpectedTravelCosts,
                                model.ExpectedTravelCostInfo,
                                (model.SetLatestAnswerTimeForCustomer != null && EnumHelper.Parse<TrueFalse>(model.SetLatestAnswerTimeForCustomer.SelectedItem.Value) == TrueFalse.Yes) ? model.LatestAnswerTimeForCustomer : null
                            );
                        }
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (ArgumentException ex)
                    {
                        request = await GetRequestToProcess(model.RequestId);
                        RequestModel requestModel = GetModel(request);
                        requestModel.CombinedMaxSizeAttachments = _options.CombinedMaxSizeAttachments;
                        if (model.Status == RequestStatus.AcceptedNewInterpreterAppointed)
                        {
                            requestModel.Status = RequestStatus.AcceptedNewInterpreterAppointed;
                            requestModel.OldInterpreterId = request.InterpreterBrokerId;
                            requestModel.OtherInterpreterId = _requestService.GetOtherInterpreterIdForSameOccasion(request);
                        }

                        //Set the temporarly saved Files if any
                        if (model.Files != null && model.Files.Any())
                        {
                            List<FileModel> files = _dbContext.Attachments.
                                Where(a => model.Files.Select(f => f.Id).Contains(a.AttachmentId)).ToList()
                                .Select(a => new FileModel
                                {
                                    Id = a.AttachmentId,
                                    FileName = a.FileName,
                                    Size = a.Blob.Length
                                }).ToList();
                            requestModel.Files = files.Any() ? files : null;
                        }
                        ModelState.AddModelError(ex.ParamName, ex.Message);
                        return View(nameof(Process), requestModel);
                    }
                    catch (InvalidOperationException ex)
                    {
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
        public async Task<IActionResult> Cancel(RequestCancelModel model)
        {
            if (ModelState.IsValid)
            {
                var request = _dbContext.Requests
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerUnit)
                    .Include(r => r.Order.CreatedByUser)
                    .Include(r => r.Order.ContactPersonUser)
                    .Include(r => r.Interpreter)
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .SingleOrDefault(r => r.RequestId == model.RequestId && r.Status == RequestStatus.Approved);
                if (request == null)
                {
                    return RedirectToAction("Index", "Home", new { ErrorMessage = "Tillfället kunde inte avbokas" });
                }
                if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Cancel)).Succeeded && request != null)
                {
                    _requestService.CancelByBroker(request, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.CancelMessage);
                    _dbContext.SaveChanges();
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
            var request = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order.CreatedByUser)
                .Include(r => r.Order.ContactPersonUser)
                .Include(r => r.Order.CustomerUnit)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder).ThenInclude(r => r.Requests)
                .Include(r => r.Interpreter)
                .Single(r => r.RequestId == model.DeniedRequestId);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded && request.CanDecline)
            {
                try
                {
                    await _requestService.Decline(request, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.DenyMessage);
                }
                catch (InvalidOperationException ex)
                {
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
                    return RedirectToAction("Index", "Home", new { errormessage = ex.Message });
                }
            }
            return Forbid();
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmOrderChange(ConfirmOrderChangeModel model)
        {
            Request request = await GetOrderChangedRequest(model.RequestId);
            if ((request.Status == RequestStatus.Approved || request.Status == RequestStatus.AcceptedNewInterpreterAppointed) && (await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded)
            {
                try
                {
                    await _requestService.ConfirmOrderChange(request, model.ConfirmedOrderChangeLogEntries, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    return RedirectToAction("Index", "Home", new { message = "Bokningsändringar bekräftade" });
                }
                catch (InvalidOperationException ex)
                {
                    return RedirectToAction("Index", "Home", new { errormessage = ex.Message });
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpDelete]
        public JsonResult DeleteRequestView(int requestId)
        {
            var requestViews = _dbContext.RequestViews
                .Where(r => r.RequestId == requestId && r.ViewedBy == User.GetUserId());
            if (requestViews.Any())
            {
                _dbContext.RequestViews.RemoveRange(requestViews);
                _dbContext.SaveChanges();
            }
            return Json(new { success = true });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public JsonResult AddRequestView(int requestId)
        {
            var request = _dbContext.Requests
               .Include(r => r.RequestViews).Single(r => r.RequestId == requestId);
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
            var request = await GetRequestForEventlog(id);
            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded)
            {
                return PartialView("_EventLogDynamic", new EventLogModel
                {
                    Entries = EventLogHelper.GetEventLog(request, request.Order.CustomerOrganisation.Name, request.Ranking.Broker.Name,
                    previousRequests: _dbContext.Requests
                        .Include(r => r.ReceivedByUser)
                        .Include(r => r.AnsweringUser)
                        .Include(r => r.ProcessingUser)
                        .Include(r => r.CancelledByUser)
                        .Include(r => r.Interpreter)
                        .Include(r => r.ReplacedByRequest).ThenInclude(rbr => rbr.AnsweringUser)
                        .Include(r => r.ReplacedByRequest).ThenInclude(rbr => rbr.Interpreter)
                        .Include(r => r.RequestUpdateLatestAnswerTime).ThenInclude(ru => ru.UpdatedByUser)
                        .Include(r => r.RequestStatusConfirmations).ThenInclude(rs => rs.ConfirmedByUser)
                        .Include(r => r.Requisitions).ThenInclude(u => u.CreatedByUser)
                        .Include(r => r.Requisitions).ThenInclude(u => u.ProcessedUser)
                        .Include(r => r.Complaints).ThenInclude(c => c.CreatedByUser)
                        .Include(r => r.Complaints).ThenInclude(c => c.AnsweringUser)
                        .Include(r => r.Complaints).ThenInclude(c => c.AnswerDisputingUser)
                        .Include(r => r.Complaints).ThenInclude(c => c.TerminatingUser)
                        .Include(r => r.Ranking).ThenInclude(ra => ra.Broker)
                        .Where(r => r.OrderId == request.OrderId && r.RequestId != request.RequestId))
                    .OrderBy(e => e.Timestamp).ToList()
                });
            }
            return Forbid();
        }

        private async Task<Request> GetRequestToProcess(int requestId)
        {
            return await _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(r => r.Order).ThenInclude(o => o.Requirements)
                .Include(r => r.Order).ThenInclude(o => o.CreatedByUser)
                .Include(r => r.Order).ThenInclude(o => o.ContactPersonUser)
                .Include(r => r.Order).ThenInclude(o => o.InterpreterLocations)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.CustomerUnit)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.Region)
                .Include(r => r.Order).ThenInclude(o => o.Group).ThenInclude(o => o.Attachments).ThenInclude(a => a.Attachment).ThenInclude(at => at.OrderAttachmentHistoryEntries).ThenInclude(oh => oh.OrderChangeLogEntry)
                .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder).ThenInclude(r => r.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.ReplacedByOrder).ThenInclude(r => r.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.IsExtraInterpreterForOrder).ThenInclude(r => r.Requests)
                .Include(r => r.Order).ThenInclude(o => o.ExtraInterpreterOrder).ThenInclude(r => r.Requests)
                .Include(r => r.Order).ThenInclude(o => o.Attachments).ThenInclude(a => a.Attachment)
                .Include(r => r.Order).ThenInclude(r => r.CompetenceRequirements)
                .Include(r => r.Interpreter)
                .Include(r => r.RequestViews).ThenInclude(rv => rv.ViewedByUser)
                .Include(r => r.Requisitions)
                .Include(r => r.Ranking)
                .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(r => r.RequirementAnswers)
                .Include(r => r.Attachments).ThenInclude(r => r.Attachment)
                .SingleAsync(r => r.RequestId == requestId);
        }

        private async Task<Request> GetRequestToView(int requestId)
        {
            return await _dbContext.Requests
                .Include(r => r.Order).ThenInclude(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(r => r.Order).ThenInclude(r => r.Requirements)
                .Include(r => r.Order).ThenInclude(r => r.CreatedByUser)//.ThenInclude(u => u.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(r => r.ContactPersonUser)
                .Include(r => r.Order).ThenInclude(l => l.InterpreterLocations)
                .Include(r => r.Order).ThenInclude(r => r.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.CustomerUnit)
                .Include(r => r.Order).ThenInclude(r => r.Language)
                .Include(r => r.Order).ThenInclude(r => r.Region)
                .Include(r => r.Order).ThenInclude(r => r.CompetenceRequirements)
                //.Include(r => r.Order).ThenInclude(o => o.OrderChangeLogEntries).ThenInclude(oc => oc.UpdatedByUser)
                //.Include(r => r.Order).ThenInclude(o => o.OrderChangeLogEntries).ThenInclude(oc => oc.OrderChangeConfirmation).ThenInclude(rs => rs.ConfirmedByUser)
                //.Include(r => r.Order).ThenInclude(o => o.OrderChangeLogEntries).ThenInclude(oc => oc.OrderHistories)
                .Include(r => r.Order).ThenInclude(o => o.Group).ThenInclude(o => o.Attachments).ThenInclude(a => a.Attachment).ThenInclude(at => at.OrderAttachmentHistoryEntries).ThenInclude(oh => oh.OrderChangeLogEntry)
                .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder).ThenInclude(r => r.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.ReplacedByOrder).ThenInclude(r => r.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.Attachments).ThenInclude(a => a.Attachment)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.RequestViews).ThenInclude(rv => rv.ViewedByUser)//
                .Include(r => r.Interpreter)
                .Include(r => r.RequestGroup).ThenInclude(o => o.Attachments).ThenInclude(a => a.Attachment)
                .Include(r => r.RequirementAnswers)
                .Include(r => r.Requisitions)
                .Include(r => r.Complaints)
                .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(r => r.Attachments).ThenInclude(r => r.Attachment)
                .Include(r => r.AnsweringUser)//.ThenInclude(u => u.Broker)
                //.Include(r => r.ProcessingUser)
                //.Include(r => r.ReceivedByUser).ThenInclude(u => u.Broker)
                //.Include(r => r.CancelledByUser).ThenInclude(u => u.Broker)
                .Include(r => r.RequestUpdateLatestAnswerTime)//.ThenInclude(r => r.UpdatedByUser)
                .Include(r => r.RequestStatusConfirmations)//.ThenInclude(rs => rs.ConfirmedByUser)
                .SingleAsync(r => r.RequestId == requestId);
        }

        private async Task<Request> GetRequestForEventlog(int requestId)
        {
            return await _dbContext.Requests
                .Include(r => r.Order).ThenInclude(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(r => r.Order).ThenInclude(r => r.Requirements)
                .Include(r => r.Order).ThenInclude(r => r.CreatedByUser).ThenInclude(u => u.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(r => r.ContactPersonUser)
                .Include(r => r.Order).ThenInclude(l => l.InterpreterLocations)
                .Include(r => r.Order).ThenInclude(r => r.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.CustomerUnit)
                .Include(r => r.Order).ThenInclude(r => r.Language)
                .Include(r => r.Order).ThenInclude(r => r.Region)
                .Include(r => r.Order).ThenInclude(r => r.CompetenceRequirements)
                .Include(r => r.Order).ThenInclude(o => o.OrderChangeLogEntries).ThenInclude(oc => oc.UpdatedByUser)
                .Include(r => r.Order).ThenInclude(o => o.OrderChangeLogEntries).ThenInclude(oc => oc.OrderChangeConfirmation).ThenInclude(rs => rs.ConfirmedByUser)
                .Include(r => r.Order).ThenInclude(o => o.OrderChangeLogEntries).ThenInclude(oc => oc.OrderHistories)
                .Include(r => r.Order).ThenInclude(o => o.Group).ThenInclude(o => o.Attachments).ThenInclude(a => a.Attachment).ThenInclude(at => at.OrderAttachmentHistoryEntries).ThenInclude(oh => oh.OrderChangeLogEntry)
                .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder).ThenInclude(r => r.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.ReplacedByOrder).ThenInclude(r => r.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.Attachments).ThenInclude(a => a.Attachment)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.RequestViews).ThenInclude(rv => rv.ViewedByUser)
                .Include(r => r.Interpreter)
                .Include(r => r.RequestGroup).ThenInclude(o => o.Attachments).ThenInclude(a => a.Attachment)
                .Include(r => r.RequirementAnswers)
                .Include(r => r.Requisitions).ThenInclude(u => u.CreatedByUser).ThenInclude(u => u.Broker)
                .Include(r => r.Requisitions).ThenInclude(u => u.ProcessedUser)
                .Include(r => r.Complaints).ThenInclude(c => c.CreatedByUser)
                .Include(r => r.Complaints).ThenInclude(c => c.AnsweringUser).ThenInclude(u => u.Broker)
                .Include(r => r.Complaints).ThenInclude(c => c.AnswerDisputingUser)
                .Include(r => r.Complaints).ThenInclude(c => c.TerminatingUser)
                .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(r => r.Attachments).ThenInclude(r => r.Attachment)
                .Include(r => r.AnsweringUser).ThenInclude(u => u.Broker)
                .Include(r => r.ProcessingUser)
                .Include(r => r.ReceivedByUser).ThenInclude(u => u.Broker)
                .Include(r => r.CancelledByUser).ThenInclude(u => u.Broker)
                .Include(r => r.RequestUpdateLatestAnswerTime).ThenInclude(r => r.UpdatedByUser)
                .Include(r => r.RequestStatusConfirmations).ThenInclude(rs => rs.ConfirmedByUser)
                .SingleAsync(r => r.RequestId == requestId);
        }

        private async Task<Request> GetConfirmedRequest(int requestId)
        {
            return await _dbContext.Requests
                .Include(r => r.Ranking)
                .Include(r => r.Order)
                .Include(r => r.RequestStatusConfirmations)
                .SingleAsync(r => r.RequestId == requestId);
        }

        private async Task<Request> GetOrderChangedRequest(int requestId)
        {
            return await _dbContext.Requests
                .Include(r => r.Ranking)
                .Include(r => r.Order).ThenInclude(o => o.OrderChangeLogEntries).ThenInclude(oc => oc.OrderChangeConfirmation)
                .SingleAsync(r => r.RequestId == requestId);
        }

        private RequestModel GetModel(Request request, bool isView = false)
        {
            var model = RequestModel.GetModelFromRequest(request);
            model.OrderModel.ActiveRequest = model; //We're only interested in the request we have access to
            model.RequestCalculatedPriceInformationModel = PriceInformationModel.GetPriceinformationToDisplay(request);
            model.OrderCalculatedPriceInformationModel = GetPriceinformationOrderToDisplay(request, model.OrderModel.RequestedCompetenceLevels);
            if (request.InterpreterLocation != null)
            {
                model.InterpreterLocationAnswer = model.OrderModel.InterpreterLocationAnswer = (InterpreterLocation)request.InterpreterLocation.Value;
            }
            if (request.Status == RequestStatus.CancelledByCreatorWhenApproved)
            {
                model.Info48HCancelledByCustomer = _dateCalculationService.GetNoOf24HsPeriodsWorkDaysBetween(request.CancelledAt.Value.DateTime, request.Order.StartAt.DateTime) < 2 ? "Detta är en avbokning som skett med mindre än 48 timmar till tolkuppdragets start. Därmed utgår full ersättning, inklusive bland annat spilltid och förmedlingsavgift, i de fall något ersättningsuppdrag inte kan ordnas av kund. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna." : "Detta är en avbokning som skett med mer än 48 timmar till tolkuppdragets start. Därmed utgår förmedlingsavgift till leverantören. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna.";
            }
            if (request.RequestViews != null && request.RequestViews.Any(rv => rv.ViewedBy != User.GetUserId()))
            {
                model.ViewedByUser = request.RequestViews.First(rv => rv.ViewedBy != User.GetUserId()).ViewedByUser.FullName + " håller också på med denna förfrågan";
            }
            model.BrokerId = request.Ranking.BrokerId;
            model.AllowInterpreterChange = request.CanChangeInterpreter(_clock.SwedenNow);
            //todo flytta till request
            model.AllowRequisitionRegistration = (request.Status == RequestStatus.Approved || request.Status == RequestStatus.Delivered) && !request.Requisitions.Any() && request.Order.StartAt < _clock.SwedenNow;
            model.AllowConfirmNoRequisition = request.Status == RequestStatus.Approved && !request.Requisitions.Any() && request.Order.StartAt < _clock.SwedenNow && !request.RequestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.Approved);
            model.AllowCancellation = request.Order.StartAt > _clock.SwedenNow && _authorizationService.AuthorizeAsync(User, request, Policies.Cancel).Result.Succeeded;
            model.AllowConfirmationDenial = request.Status == RequestStatus.DeniedByCreator && !request.RequestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.DeniedByCreator);
            model.AllowConfirmNoAnswer = request.Status == RequestStatus.ResponseNotAnsweredByCreator && !request.RequestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.ResponseNotAnsweredByCreator);
            model.AllowConfirmCancellation = request.Status == RequestStatus.CancelledByCreatorWhenApproved && !request.RequestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.CancelledByCreatorWhenApproved);

            if (isView)
            {
                model.DisplayOrderChangeText = "";// DisplayOrderChange(request) ? GetOrderChangeText(request.Order, request) : string.Empty;
                //model.ConfirmedOrderChangeLogEntries = request.Order.OrderChangeLogEntries.Where(oc => oc.BrokerId == request.Ranking.BrokerId && oc.OrderChangeLogType != OrderChangeLogType.ContactPerson && oc.OrderChangeConfirmation == null).Select(oc => oc.OrderChangeLogEntryId).ToList();
                model.EventLog = new EventLogModel
                {
                    Header = "Bokningshändelser",
                    Id = "EventLog_Request",
                    DynamicLoadPath = $"Request/{nameof(GetEventLog)}/{request.RequestId}",
                };
            }
            return model;
        }

        private bool DisplayOrderChange(Request request) => (request.Status == RequestStatus.Approved || request.Status == RequestStatus.AcceptedNewInterpreterAppointed) && request.Order.EndAt > _clock.SwedenNow &&
            request.Order.OrderChangeLogEntries.Any(oc => oc.BrokerId == request.Ranking.BrokerId && oc.OrderChangeLogType != OrderChangeLogType.ContactPerson && oc.OrderChangeConfirmation == null);

        private static string GetOrderChangeText(Order order, Request request)
        {
            StringBuilder sb = new StringBuilder();
            var orderChangeLogEntries = order.OrderChangeLogEntries.Where(oc => (oc.OrderChangeLogType == OrderChangeLogType.OrderInformationFields || oc.OrderChangeLogType == OrderChangeLogType.AttachmentAndOrderInformationFields)
            && oc.OrderChangeConfirmation == null && oc.BrokerId == request.Ranking.BrokerId).OrderBy(oc => oc.OrderChangeLogEntryId).ToList();
            var interpreterLocation = (InterpreterLocation)request.InterpreterLocation.Value;

            string interpreterLocationText = interpreterLocation == InterpreterLocation.OffSitePhone || interpreterLocation == InterpreterLocation.OffSiteVideo ?
                order.InterpreterLocations.Where(il => il.InterpreterLocation == interpreterLocation).Single().OffSiteContactInformation :
                order.InterpreterLocations.Where(il => il.InterpreterLocation == interpreterLocation).Single().Street;
            int i = 0;
            foreach (OrderChangeLogEntry oce in orderChangeLogEntries)
            {
                i++;
                var nextToCompareTo = orderChangeLogEntries.Count > i ? orderChangeLogEntries[i] : null;
                var date = $"{oce.LoggedAt.ToSwedishString("yyyy-MM-dd HH:mm")} - ";
                foreach (OrderHistoryEntry oh in oce.OrderHistories)
                {
                    switch (oh.ChangeOrderType)
                    {
                        case ChangeOrderType.LocationStreet:
                            sb.Append(GetOrderFieldText(date, oh, nextToCompareTo == null ? interpreterLocationText : nextToCompareTo.OrderHistories.SingleOrDefault(o => o.ChangeOrderType == ChangeOrderType.LocationStreet).Value));
                            break;
                        case ChangeOrderType.OffSiteContactInformation:
                            sb.Append(GetOrderFieldText(date, oh, nextToCompareTo == null ? interpreterLocationText : nextToCompareTo.OrderHistories.SingleOrDefault(o => o.ChangeOrderType == ChangeOrderType.OffSiteContactInformation).Value));
                            break;
                        case ChangeOrderType.Description:
                            sb.Append(GetOrderFieldText(date, oh, nextToCompareTo == null ? order.Description : nextToCompareTo.OrderHistories.SingleOrDefault(o => o.ChangeOrderType == ChangeOrderType.Description).Value));
                            break;
                        case ChangeOrderType.InvoiceReference:
                            sb.Append(GetOrderFieldText(date, oh, nextToCompareTo == null ? order.InvoiceReference : nextToCompareTo.OrderHistories.SingleOrDefault(o => o.ChangeOrderType == ChangeOrderType.InvoiceReference).Value));
                            break;
                        case ChangeOrderType.CustomerReferenceNumber:
                            sb.Append(GetOrderFieldText(date, oh, nextToCompareTo == null ? order.CustomerReferenceNumber : nextToCompareTo.OrderHistories.SingleOrDefault(o => o.ChangeOrderType == ChangeOrderType.CustomerReferenceNumber).Value));
                            break;
                        case ChangeOrderType.CustomerDepartment:
                            sb.Append(GetOrderFieldText(date, oh, nextToCompareTo == null ? order.UnitName : nextToCompareTo.OrderHistories.SingleOrDefault(o => o.ChangeOrderType == ChangeOrderType.CustomerDepartment).Value));
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
            var orderAttachmentChangeLogEntries = order.OrderChangeLogEntries.Where(oc => (oc.OrderChangeLogType == OrderChangeLogType.Attachment || oc.OrderChangeLogType == OrderChangeLogType.AttachmentAndOrderInformationFields)
                && oc.OrderChangeConfirmation == null && oc.BrokerId == request.Ranking.BrokerId).OrderBy(oc => oc.OrderChangeLogEntryId).ToList();
            if (orderAttachmentChangeLogEntries.Any())
            {
                sb.Append("\n");
                foreach (OrderChangeLogEntry oce in orderAttachmentChangeLogEntries)
                {
                    sb.Append($"{oce.LoggedAt.ToSwedishString("yyyy-MM-dd HH:mm")} - Bifogade bilagor ändrade\n");
                }
            }
            return sb.ToString();
        }

        private static string GetOrderFieldText(string date, OrderHistoryEntry oh, string newValue)
        {
            return (string.IsNullOrEmpty(newValue) && string.IsNullOrEmpty(oh.Value)) ? string.Empty :
                string.IsNullOrEmpty(newValue) ? $"{date}{oh.ChangeOrderType.GetDescription()} - Informationen togs bort\n" :
                newValue.Equals(oh.Value, StringComparison.OrdinalIgnoreCase) ? string.Empty :
                $"{date}{oh.ChangeOrderType.GetDescription()} - Nytt värde: {newValue}\n";
        }

        private PriceInformationModel GetPriceinformationOrderToDisplay(Request request, List<CompetenceAndSpecialistLevel> requestedCompetenceLevels)
        {
            return new PriceInformationModel
            {
                MealBreakIsNotDetucted = request.Order.MealBreakIncluded ?? false,
                PriceInformationToDisplay = PriceCalculationService.GetPriceInformationToDisplay(
                    _priceCalculationService.GetPrices(request, OrderService.SelectCompetenceLevelForPriceEstimation(requestedCompetenceLevels), null).PriceRows),
                Header = "Beräknat pris enligt bokningsförfrågan",
                UseDisplayHideInfo = true
            };
        }
    }
}
