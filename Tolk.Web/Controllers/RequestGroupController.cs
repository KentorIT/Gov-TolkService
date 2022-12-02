using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic;
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
    public class RequestGroupController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly IAuthorizationService _authorizationService;
        private readonly RequestService _requestService;
        private readonly ISwedishClock _clock;
        private readonly ILogger _logger;
        private readonly TolkOptions _options;
        private readonly InterpreterService _interpreterService;
        private readonly PriceCalculationService _priceCalculationService;
        private readonly CacheService _cacheService;
        private readonly OrderService _orderService;
        private readonly ListToModelService _listToModelService;

        public RequestGroupController(
            TolkDbContext dbContext,
            IAuthorizationService authorizationService,
            RequestService requestService,
            ISwedishClock clock,
            ILogger<OrderController> logger,
            IOptions<TolkOptions> options,
            InterpreterService interpreterService,
            PriceCalculationService priceCalculationService,
            OrderService orderService,
            ListToModelService listToModelService,
            CacheService cacheService)
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _requestService = requestService;
            _clock = clock;
            _logger = logger;
            _options = options.Value;
            _interpreterService = interpreterService;
            _priceCalculationService = priceCalculationService;
            _cacheService = cacheService;
            _orderService = orderService;
            _listToModelService = listToModelService;
        }

        public async Task<IActionResult> View(int id)
        {
            var requestGroup = await _dbContext.RequestGroups.GetRequestGroupToView(id);
            OrderGroup orderGroup = await _dbContext.OrderGroups.GetFullOrderGroupById(requestGroup.OrderGroupId);
            if ((await _authorizationService.AuthorizeAsync(User, requestGroup, Policies.View)).Succeeded)
            {
                if (EnumHelper.Parent<RequestStatus, NegotiationState>(requestGroup.Status) == NegotiationState.ReplacedByOtherEntity)
                {
                    id = _dbContext.RequestGroups.OrderBy(rg => rg.RequestGroupId).Last(rg => rg.OrderGroupId == requestGroup.OrderGroupId && rg.Ranking.BrokerId == User.GetBrokerId()).RequestGroupId;
                    return RedirectToAction(nameof(View), new { id });
                }
                if (requestGroup.IsToBeProcessedByBroker)
                {
                    return RedirectToAction(nameof(Process), new { id = requestGroup.RequestGroupId });
                }

                await _orderService.AddOrdersWithListsForGroup(orderGroup);
                await _requestService.AddListsForRequestGroup(requestGroup);
                requestGroup.Requests.ForEach(r => r.Order = orderGroup.Orders.Where(o => o.OrderId == r.OrderId).SingleOrDefault());
                var model = RequestGroupViewModel.GetModelFromRequestGroup(requestGroup, true, false);
                await _listToModelService.AddInformationFromListsToModel(model);
                model.CustomerInformationModel.IsCustomer = false;
                model.CustomerInformationModel.UseSelfInvoicingInterpreter = _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == requestGroup.OrderGroup.CustomerOrganisationId && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseSelfInvoicingInterpreter));
                model.OrderGroupModel = OrderGroupModel.GetModelFromOrderGroup(requestGroup.OrderGroup, requestGroup, true);
                await _listToModelService.AddInformationFromListsToModel(model.OrderGroupModel, requestGroup.OrderGroupId);
                if (requestGroup.QuarantineId.HasValue)
                {
                    List<OrderOccasionDisplayModel> tempOccasionList = new List<OrderOccasionDisplayModel>();
                    foreach (OrderOccasionDisplayModel occasion in model.OccasionList.Occasions)
                    {
                        var request = requestGroup.Requests.Single(r => r.RequestId == occasion.RouteId);
                        tempOccasionList.Add(OrderOccasionDisplayModel.GetModelFromOrder(request.Order, GetPriceinformationOrderToDisplay(request, model.OrderGroupModel.RequestedCompetenceLevels.ToList()), request));
                    }
                    model.OccasionList.Occasions = tempOccasionList;
                }
                model.OrderGroupModel.UseAttachments = true;
                return View(model);
            }
            return Forbid();
        }

        public async Task<IActionResult> Process(int id)
        {
            var requestGroup = await _dbContext.RequestGroups.GetRequestGroupToView(id);

            if ((await _authorizationService.AuthorizeAsync(User, requestGroup, Policies.Accept)).Succeeded)
            {
                if (!requestGroup.IsToBeProcessedByBroker)
                {
                    _logger.LogWarning("Wrong status when trying to process request group. Status: {request.Status}, RequestGroupId: {request.RequestGroupId}", requestGroup.Status, requestGroup.RequestGroupId);
                    return RedirectToAction(nameof(View), new { id });
                }
                if (requestGroup.Status == RequestStatus.Created)
                {
                    await _requestService.AcknowledgeGroup(requestGroup, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    await _dbContext.SaveChangesAsync();
                }

                OrderGroup orderGroup = await _dbContext.OrderGroups.GetFullOrderGroupById(requestGroup.OrderGroupId);
                await _orderService.AddOrdersWithListsForGroup(orderGroup);
                await _requestService.AddListsForRequestGroup(requestGroup);
                requestGroup.Requests.ForEach(r => r.Order = orderGroup.Orders.Where(o => o.OrderId == r.OrderId).SingleOrDefault());

                var model = RequestGroupProcessModel.GetModelFromRequestGroup(requestGroup, Guid.NewGuid(), _options.CombinedMaxSizeAttachments, _options.AllowDeclineExtraInterpreterOnRequestGroups);
                await _listToModelService.AddInformationFromListsToModel(model, User.GetUserId());
                model.CustomerInformationModel.IsCustomer = false;
                model.CustomerInformationModel.UseSelfInvoicingInterpreter = _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == requestGroup.OrderGroup.CustomerOrganisationId && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseSelfInvoicingInterpreter));
                //if not first broker in rank (requests are not answered and have no pricerows) we need to get a calculated price with correct broker fee 
                if (requestGroup.Ranking.Rank != 1)
                {
                    List<OrderOccasionDisplayModel> tempOccasionList = new List<OrderOccasionDisplayModel>();
                    foreach (OrderOccasionDisplayModel occasion in model.OccasionList.Occasions)
                    {
                        var request = requestGroup.Requests.Single(r => r.RequestId == occasion.RouteId);
                        tempOccasionList.Add(OrderOccasionDisplayModel.GetModelFromOrder(request.Order, GetPriceinformationOrderToDisplay(request, model.RequestedCompetenceLevels.ToList()), request));
                    }
                    model.OccasionList.Occasions = tempOccasionList;
                }
                return View(model);
            }
            return Forbid();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Answer(RequestGroupAnswerModel model)
        {
            var requestGroup = await _dbContext.RequestGroups.GetRequestGroupToProcessById(model.RequestGroupId);
            if ((await _authorizationService.AuthorizeAsync(User, requestGroup, Policies.Accept)).Succeeded)
            {
                if (!requestGroup.IsToBeProcessedByBroker)
                {
                    return RedirectToAction("Index", "Home", new { ErrorMessage = "Förfrågan är redan behandlad" });
                }
                InterpreterAnswerDto interpreterModel = null;
                try
                {
                    interpreterModel = await GetInterpreter(model.InterpreterAnswerModel, requestGroup.Ranking.BrokerId);
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError($"{nameof(model.InterpreterAnswerModel)}.{ex.ParamName}", ex.Message);
                }
                InterpreterAnswerDto extrainterpreterModel = null;
                try
                {
                    extrainterpreterModel = model.ExtraInterpreterAnswerModel != null ? await GetInterpreter(model.ExtraInterpreterAnswerModel, requestGroup.Ranking.BrokerId) : null;
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError($"{nameof(model.ExtraInterpreterAnswerModel)}.{ex.ParamName}", ex.Message);
                }
                if (ModelState.IsValid)
                {
                    try
                    {
                        await _requestService.AnswerGroup(
                            requestGroup,
                            _clock.SwedenNow,
                            User.GetUserId(),
                            User.TryGetImpersonatorId(),
                            model.InterpreterLocation.Value,
                            interpreterModel,
                            extrainterpreterModel,
                            model.Files?.Select(f => new RequestGroupAttachment { AttachmentId = f.Id }).ToList(),
                            (model.SetLatestAnswerTimeForCustomer != null && EnumHelper.Parse<TrueFalse>(model.SetLatestAnswerTimeForCustomer.SelectedItem.Value) == TrueFalse.Yes) ? model.LatestAnswerTimeForCustomer : null,
                            model.BrokerReferenceNumber
                        );
                        await _dbContext.SaveChangesAsync();
                        return RedirectToAction("Index", "Home", new { message = "Svar har skickats på sammanhållen bokning" });
                    }
                    catch (InvalidOperationException e)
                    {
                        _logger.LogError("Process failed for requestgroup, RequestGroupId: {requestGroup.RequestGroupId}, message {e.Message}", requestGroup.RequestGroupId, e.Message);
                        return RedirectToAction("Index", "Home", new { errormessage = e.Message });
                    }
                }

                //Should return to Process if error is of a kind that can be handled in the ui.
                return View(nameof(Process), model);
            }
            return Forbid();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(RequestGroupAcceptModel model)
        {
            var requestGroup = await _dbContext.RequestGroups.GetRequestGroupToProcessById(model.RequestGroupId);
            if ((await _authorizationService.AuthorizeAsync(User, requestGroup, Policies.Accept)).Succeeded)
            {
                if (!requestGroup.CanAccept)
                {
                    return RedirectToAction("Index", "Home", new { ErrorMessage = "Förfrågan är redan behandlad" });
                }
                if (ModelState.IsValid)
                {
                    try
                    {
                        await _requestService.AcceptGroup(
                            requestGroup,
                            _clock.SwedenNow,
                            User.GetUserId(),
                            User.TryGetImpersonatorId(),
                            model.InterpreterLocationOnAccept,
                            model.InterpreterAcceptModel.AcceptDto,
                            model.ExtraInterpreterAcceptModel?.AcceptDto,
                            model.Files?.Select(f => new RequestGroupAttachment { AttachmentId = f.Id }).ToList(),
                            model.BrokerReferenceNumber
                        );
                        await _dbContext.SaveChangesAsync();
                        return RedirectToAction("Index", "Home", new { message = "Bekräftelse har skickats på sammanhållen bokning" });
                    }
                    catch (InvalidOperationException e)
                    {
                        _logger.LogError("Process failed for requestgroup, RequestGroupId: {requestGroup.RequestGroupId}, message {e.Message}", requestGroup.RequestGroupId, e.Message);
                        return RedirectToAction("Index", "Home", new { errormessage = e.Message });
                    }
                }

                //Should return to Process if error is of a kind that can be handled in the ui.
                return View(nameof(Process), model);
            }
            return Forbid();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(RequestGroupDeclineModel model)
        {
            var requestGroup = await _dbContext.RequestGroups.GetRequestGroupToProcessById(model.DeniedRequestGroupId);
            if ((await _authorizationService.AuthorizeAsync(User, requestGroup, Policies.Accept)).Succeeded)
            {
                if (!requestGroup.IsToBeProcessedByBroker)
                {
                    _logger.LogWarning("Wrong status when trying to process request group. Status: {request.Status}, RequestGroupId: {request.RequestGroupId}", requestGroup.Status, requestGroup.RequestGroupId);
                    return RedirectToAction(nameof(View), new { model.DeniedRequestGroupId });
                }
                await _requestService.DeclineGroup(requestGroup, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.DenyMessage);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(View), new { id = model.DeniedRequestGroupId });
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ConfirmDenial(int requestGroupId)
        {
            RequestGroup requestGroup = await GetConfirmedRequestGroup(requestGroupId);
            if (requestGroup.Status == RequestStatus.DeniedByCreator && (await _authorizationService.AuthorizeAsync(User, requestGroup, Policies.View)).Succeeded)
            {
                try
                {
                    await _requestService.ConfirmGroupDenial(requestGroup, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    return RedirectToAction("Index", "Home", new { message = "Sammanhållen bokningsförfrågan arkiverad" });
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError("ConfirmDenial failed for requestgroup, RequestGroupId: {requestGroupId}, message {ex.Message}", requestGroupId, ex.Message);
                    return RedirectToAction("Index", "Home", new { errormessage = ex.Message });
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ConfirmNoAnswer(int requestGroupId)
        {
            RequestGroup requestGroup = await GetConfirmedRequestGroup(requestGroupId);
            if (requestGroup.Status == RequestStatus.ResponseNotAnsweredByCreator && (await _authorizationService.AuthorizeAsync(User, requestGroup, Policies.View)).Succeeded)
            {
                try
                {
                    await _requestService.ConfirmGroupNoAnswer(requestGroup, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    return RedirectToAction("Index", "Home", new { message = "Sammanhållen bokningsförfrågan arkiverad" });
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError("ConfirmNoAnswer failed for requestgroup, RequestGroupId: {requestGroupId}, message {ex.Message}", requestGroupId, ex.Message);
                    return RedirectToAction("Index", "Home", new { errormessage = ex.Message });
                }
            }
            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpDelete]
        public async Task<JsonResult> DeleteView(int id)
        {
            var requestViews = _dbContext.RequestGroupViews
                .Where(r => r.RequestGroupId == id && r.ViewedBy == User.GetUserId());
            if (requestViews.Any())
            {
                _dbContext.RequestGroupViews.RemoveRange(requestViews);
                await _dbContext.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<JsonResult> AddView(int id)
        {
            var requestGroup = await _dbContext.RequestGroups.GetRequestGroupById(id);
            requestGroup.Views = await _dbContext.RequestGroupViews.GetRequestViewsForRequestGroup(requestGroup.RequestGroupId).ToListAsync();
            if (requestGroup != null)
            {
                requestGroup.AddView(User.GetUserId(), User.TryGetImpersonatorId(), _clock.SwedenNow);
                await _dbContext.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        private async Task<InterpreterAnswerDto> GetInterpreter(InterpreterAnswerModel interpreterModel, int brokerId)
        {
            if (interpreterModel.InterpreterId == Constants.DeclineInterpreterId)
            {
                return new InterpreterAnswerDto
                {
                    Accepted = false,
                    DeclineMessage = interpreterModel.DeclineMessage
                };
            }
            var newInterpreterInformation = new InterpreterInformation
            {
                FirstName = interpreterModel.NewInterpreterFirstName,
                LastName = interpreterModel.NewInterpreterLastName,
                Email = interpreterModel.NewInterpreterEmail,
                PhoneNumber = interpreterModel.NewInterpreterPhoneNumber,
                OfficialInterpreterId = interpreterModel.NewInterpreterOfficialInterpreterId
            };
            var interpreter = await _interpreterService.GetInterpreter(interpreterModel.InterpreterId.Value, newInterpreterInformation, brokerId);
            var requirementAnswers = interpreterModel.RequiredRequirementAnswers?.Select(ra => new OrderRequirementRequestAnswer
            {
                OrderRequirementId = ra.OrderRequirementId,
                Answer = ra.Answer,
                CanSatisfyRequirement = ra.CanMeetRequirement
            }).ToList() ?? new List<OrderRequirementRequestAnswer>();

            if (interpreterModel.DesiredRequirementAnswers != null)
            {
                requirementAnswers.AddRange(interpreterModel.DesiredRequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                {
                    OrderRequirementId = ra.OrderRequirementId,
                    Answer = ra.Answer,
                    CanSatisfyRequirement = ra.CanMeetRequirement
                }).ToList());
            }
            //Collect the interpreter information
            return new InterpreterAnswerDto
            {
                Accepted = true,
                CompetenceLevel = interpreterModel.InterpreterCompetenceLevel.Value,
                ExpectedTravelCosts = interpreterModel.ExpectedTravelCosts,
                ExpectedTravelCostInfo = interpreterModel.ExpectedTravelCostInfo,
                RequirementAnswers = requirementAnswers,
                Interpreter = interpreter
            };
        }

        private async Task<RequestGroup> GetConfirmedRequestGroup(int requestGroupId)
        {
            var requestGroup = await _dbContext.RequestGroups.GetRequestGroupById(requestGroupId);
            await _requestService.AddRequestsWithConfirmationListsToRequestGroup(requestGroup);
            return requestGroup;
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
