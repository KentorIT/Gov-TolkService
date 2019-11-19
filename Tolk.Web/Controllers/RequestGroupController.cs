using AutoMapper;
using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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

        public RequestGroupController(
            TolkDbContext dbContext,
            IAuthorizationService authorizationService,
            RequestService requestService,
            ISwedishClock clock,
            ILogger<OrderController> logger,
            IOptions<TolkOptions> options,
            InterpreterService interpreterService
            )
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _requestService = requestService;
            _clock = clock;
            _logger = logger;
            _options = options.Value;
            _interpreterService = interpreterService;
        }

        public async Task<IActionResult> View(int id)
        {
            var requestGroup = await _dbContext.RequestGroups
                .Include(g => g.Ranking)
                .Include(g => g.OrderGroup)
                .SingleAsync(r => r.RequestGroupId == id);

            if ((await _authorizationService.AuthorizeAsync(User, requestGroup, Policies.View)).Succeeded)
            {
                if (requestGroup.IsToBeProcessedByBroker)
                {
                    return RedirectToAction(nameof(Process), new { id = requestGroup.RequestGroupId });
                }
                return View(RequestGroupViewModel.GetModelFromRequestGroup(requestGroup));
            }
            return Forbid();
        }

        public async Task<IActionResult> Process(int id)
        {
            var requestGroup = await _dbContext.RequestGroups
               .Include(g => g.Ranking)
               .Include(g => g.Views).ThenInclude(v => v.ViewedByUser)
               .Include(g => g.OrderGroup).ThenInclude(o => o.Attachments).ThenInclude(a => a.Attachment)
               .Include(g => g.OrderGroup).ThenInclude(o => o.CreatedByUser)
               .Include(g => g.OrderGroup).ThenInclude(o => o.Requirements)
               .Include(g => g.OrderGroup).ThenInclude(o => o.CompetenceRequirements)
               .Include(g => g.OrderGroup).ThenInclude(o => o.Language)
               .Include(g => g.OrderGroup).ThenInclude(o => o.Region)
               .Include(g => g.OrderGroup).ThenInclude(o => o.CustomerOrganisation)
               .Include(g => g.Requests).ThenInclude(o => o.Order).ThenInclude(o => o.InterpreterLocations)
               .Include(g => g.Requests).ThenInclude(o => o.Order).ThenInclude(o => o.PriceRows).ThenInclude(r => r.PriceListRow)
               .Include(g => g.OrderGroup).ThenInclude(o => o.Orders)
               .SingleAsync(r => r.RequestGroupId == id);

            if ((await _authorizationService.AuthorizeAsync(User, requestGroup, Policies.Accept)).Succeeded)
            {
                if (!requestGroup.IsToBeProcessedByBroker)
                {
                    _logger.LogWarning("Wrong status when trying to process request group. Status: {request.Status}, RequestGroupId: {request.RequestGroupId}", requestGroup.Status, requestGroup.RequestGroupId);
                    return RedirectToAction(nameof(View), new { id });
                }
                if (requestGroup.Status == RequestStatus.Created)
                {
                    _requestService.AcknowledgeGroup(requestGroup, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    await _dbContext.SaveChangesAsync();
                }

                return View(RequestGroupProcessModel.GetModelFromRequestGroup(requestGroup, Guid.NewGuid(), _options.CombinedMaxSizeAttachments, User.GetUserId(), _options.AllowDeclineExtraInterpreterOnRequestGroups));
            }
            return Forbid();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(RequestGroupProcessModel model)
        {
            var requestGroup = await _dbContext.RequestGroups
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Requests).ThenInclude(r => r.RequirementAnswers)
                .Include(r => r.Requests).ThenInclude(r => r.PriceRows)
                .Include(r => r.Requests).ThenInclude(r => r.Order).ThenInclude(o => o.Requests)
               //.Include(r => r.OrderGroup).ThenInclude(o => o.CustomerUnit)
               .Include(g => g.OrderGroup).ThenInclude(o => o.CreatedByUser)
               .Include(g => g.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.Requirements)
               .Include(g => g.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.CompetenceRequirements)
               .Include(g => g.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.Language)
               .Include(g => g.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.Region)
               .Include(g => g.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.PriceRows).ThenInclude(r => r.PriceListRow)
               .Include(g => g.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.CustomerOrganisation)
               .Include(g => g.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.InterpreterLocations)
               .SingleAsync(r => r.RequestGroupId == model.RequestGroupId);

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
                        //Collect, if any, attachments
                        await _requestService.AcceptGroup(
                            requestGroup, 
                            _clock.SwedenNow, 
                            User.GetUserId(), 
                            User.TryGetImpersonatorId(), 
                            model.InterpreterLocation.Value, 
                            interpreterModel, 
                            extrainterpreterModel, 
                            model.Files?.Select(f => new RequestGroupAttachment { AttachmentId = f.Id }).ToList()
                        );
                        await _dbContext.SaveChangesAsync();
                        return RedirectToAction("Index", "Home", new { message = "Svar har skickats på sammanhållen bokning" });
                    }
                    catch (InvalidOperationException e)
                    {
                        _logger.LogInformation(e, e.Message);
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
            var requestGroup = await _dbContext.RequestGroups
                .Include(r => r.OrderGroup).ThenInclude(o => o.RequestGroups).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.OrderGroup).ThenInclude(o => o.CreatedByUser)
                .Include(r => r.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.Requests)
                .Include(r => r.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.CustomerUnit)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .SingleOrDefaultAsync(r => r.RequestGroupId == model.DeniedRequestGroupId);
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
            RequestGroup requestGroup =  await _dbContext.RequestGroups
                .Include(r => r.Ranking)
                .Include(r => r.Requests)
                .Include(r => r.StatusConfirmations)
                .SingleAsync(r => r.RequestGroupId == requestGroupId);

            if (requestGroup.Status == RequestStatus.DeniedByCreator && (await _authorizationService.AuthorizeAsync(User, requestGroup, Policies.View)).Succeeded)
            {
                await _requestService.ConfirmGroupDenial(requestGroup, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                return RedirectToAction("Index", "Home", new { message = "Bokningsförfrågan arkiverad" });
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
            var requestGroup = _dbContext.RequestGroups
               .Include(r => r.Views).Single(r => r.RequestGroupId == id);
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
    }
}
