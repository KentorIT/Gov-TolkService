﻿using Microsoft.AspNetCore.Authorization;
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
        private readonly OrderService _orderService;
        private readonly IAuthorizationService _authorizationService;
        private readonly PriceCalculationService _priceCalculationService;
        private readonly DateCalculationService _dateCalculationService;
        private readonly ILogger _logger;
        private readonly TolkOptions _options;
        private readonly NotificationService _notificationService;
        private readonly RequestService _requestService;

        public RequestController(
            TolkDbContext dbContext,
            ISwedishClock clock,
            OrderService orderService,
            IAuthorizationService authorizationService,
            PriceCalculationService priceCalculationService,
            DateCalculationService dateCalculationService,
            ILogger<RequestController> logger,
            IOptions<TolkOptions> options,
            NotificationService notificationService,
            RequestService requestService
        )
        {
            _dbContext = dbContext;
            _clock = clock;
            _orderService = orderService;
            _authorizationService = authorizationService;
            _priceCalculationService = priceCalculationService;
            _dateCalculationService = dateCalculationService;
            _logger = logger;
            _options = options.Value;
            _notificationService = notificationService;
            _requestService = requestService;
        }

        public IActionResult List(RequestFilterModel model)
        {
            if (model == null)
            {
                model = new RequestFilterModel();
            }
            var items = _dbContext.Requests.Include(r => r.Order)
                        .Where(r => r.Ranking.Broker.BrokerId == User.GetBrokerId() && r.Status != RequestStatus.InterpreterReplaced);
            // Filters
            items = model.Apply(items);

            return View(
                new RequestListModel
                {
                    Items = items.SelectRequestListItemModel(),
                    FilterModel = model
                });
        }

        public async Task<IActionResult> View(int id)
        {
            var request = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(r => r.Order).ThenInclude(r => r.Requirements)
                .Include(r => r.Order).ThenInclude(r => r.CreatedByUser).ThenInclude(u => u.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(r => r.ContactPersonUser)
                .Include(r => r.Order).ThenInclude(l => l.InterpreterLocations)
                .Include(r => r.Order).ThenInclude(r => r.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(r => r.Language)
                .Include(r => r.Order).ThenInclude(r => r.Region)
                .Include(r => r.Order).ThenInclude(r => r.CompetenceRequirements)
                .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder).ThenInclude(r => r.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.ReplacedByOrder).ThenInclude(r => r.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.Attachments).ThenInclude(a => a.Attachment)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Interpreter)
                .Include(r => r.RequirementAnswers)
                .Include(r => r.Requisitions)
                .Include(r => r.Complaints)
                .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(r => r.Attachments).ThenInclude(r => r.Attachment)
                .Include(r => r.AnsweringUser).ThenInclude(u => u.Broker)
                .Include(r => r.ProcessingUser).ThenInclude(u => u.CustomerOrganisation)
                .Include(r => r.ReceivedByUser).ThenInclude(u => u.Broker)
                .Include(r => r.CancelledByUser).ThenInclude(u => u.CustomerOrganisation)
                .Include(r => r.CancelledByUser).ThenInclude(u => u.Broker)
                .Include(r => r.ReplacingRequest).ThenInclude(rr => rr.Requisitions)
                .Include(r => r.ReplacingRequest).ThenInclude(rr => rr.Complaints)
                .Include(r => r.ReplacingRequest).ThenInclude(r => r.Interpreter)
                .Include(r => r.ReplacedByRequest)
                .Include(r => r.RequestStatusConfirmations).ThenInclude(rs => rs.ConfirmedByUser)
                .Single(o => o.RequestId == id);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded)
            {
                return View(GetModel(request, true));
            }
            return Forbid();
        }

        public async Task<IActionResult> Process(int id)
        {
            var request = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.PriceRows)
                .Include(r => r.Order).ThenInclude(o => o.Requirements)
                .Include(r => r.Order).ThenInclude(o => o.CreatedByUser)
                .Include(r => r.Order).ThenInclude(o => o.ContactPersonUser)
                .Include(r => r.Order).ThenInclude(o => o.InterpreterLocations)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.Region)
                .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder).ThenInclude(r => r.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.ReplacedByOrder).ThenInclude(r => r.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.Attachments).ThenInclude(a => a.Attachment)
                .Include(r => r.Order).ThenInclude(r => r.CompetenceRequirements)
                .Include(r => r.Interpreter)
                .Include(r => r.Ranking)
                .Include(r => r.PriceRows)
                .Include(r => r.RequirementAnswers)
                .Include(r => r.Attachments).ThenInclude(r => r.Attachment)
                .Single(o => o.RequestId == id);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
            {
                if (request.Status == RequestStatus.Created)
                {
                    request.Received(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    _dbContext.SaveChanges();
                }
                RequestModel model = GetModel(request);
                model.FileGroupKey = new Guid();
                model.CombinedMaxSizeAttachments = _options.CombinedMaxSizeAttachments;
                return View(model);
            }
            return Forbid();
        }

        public async Task<IActionResult> Change(int id)
        {
            var request = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(r => r.PriceRows)
                .Include(r => r.Order).ThenInclude(r => r.Requirements)
                .Include(r => r.Order).ThenInclude(r => r.CreatedByUser)
                .Include(r => r.Order).ThenInclude(r => r.ContactPersonUser)
                .Include(r => r.Order).ThenInclude(l => l.InterpreterLocations)
                .Include(r => r.Order).ThenInclude(r => r.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(r => r.Language)
                .Include(r => r.Order).ThenInclude(r => r.Region)
                .Include(r => r.Order).ThenInclude(r => r.CompetenceRequirements)
                .Include(r => r.Order).ThenInclude(o => o.Attachments).ThenInclude(a => a.Attachment)
                .Include(r => r.Attachments).ThenInclude(r => r.Attachment)
                .Include(r => r.Ranking)
                .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(r => r.RequirementAnswers)
                .Include(r => r.Interpreter)
                .Include(r => r.ReplacingRequest)
                .Include(r => r.ReplacedByRequest)
                .Include(r => r.Requisitions)
                .Single(o => o.RequestId == id);
            RequestModel model = GetModel(request);
            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
            {
                if (request.Status == RequestStatus.Approved || request.Status == RequestStatus.Accepted)
                {
                    model.Status = RequestStatus.AcceptedNewInterpreterAppointed;
                    model.ExpectedTravelCosts = 0;
                }
                model.FileGroupKey = new Guid();
                model.CombinedMaxSizeAttachments = _options.CombinedMaxSizeAttachments;
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
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Order).ThenInclude(o => o.CompetenceRequirements)
                    .Include(r => r.Order).ThenInclude(o => o.CreatedByUser)
                    .Include(r => r.Order).ThenInclude(o => o.ContactPersonUser)
                    .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder).ThenInclude(r => r.Requests)
                    .Include(r => r.Interpreter)
                    .Include(r => r.RequirementAnswers)
                    .Include(r => r.PriceRows)
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Single(o => o.RequestId == model.RequestId);

                if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
                {

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
                    //if change interpreter or else if not a replacementorder
                    if (model.Status == RequestStatus.AcceptedNewInterpreterAppointed || (!request.Order.ReplacingOrderId.HasValue && model.Status != RequestStatus.AcceptedNewInterpreterAppointed))
                    {
                        var interpreter = GetInterpreter(model.InterpreterId.Value, model.GetNewInterpreterInformation(), request.Ranking.BrokerId);

                        if (model.Status == RequestStatus.AcceptedNewInterpreterAppointed)
                        {
                            _requestService.ChangeInterpreter(
                                request,
                                _clock.SwedenNow,
                                User.GetUserId(),
                                User.TryGetImpersonatorId(),
                                interpreter,
                                model.InterpreterLocation.Value,
                                model.InterpreterCompetenceLevel.Value,
                                requirementAnswers,
                                model.Files?.Select(f => new RequestAttachment { AttachmentId = f.Id }) ?? Enumerable.Empty<RequestAttachment>(),
                                model.ExpectedTravelCosts
                            );
                        }
                        else
                        {
                            _requestService.Accept(
                                request,
                                _clock.SwedenNow,
                                User.GetUserId(),
                                User.TryGetImpersonatorId(),
                                interpreter,
                                model.InterpreterLocation.Value,
                                model.InterpreterCompetenceLevel.Value,
                                requirementAnswers,
                                model.Files?.Select(f => new RequestAttachment { AttachmentId = f.Id }).ToList(),
                                model.ExpectedTravelCosts
                            );
                        }
                    }
                    else
                    {
                        request.AcceptReplacementOrder(
                            _clock.SwedenNow,
                            User.GetUserId(),
                            User.TryGetImpersonatorId(),
                            model.ExpectedTravelCosts,
                            _priceCalculationService.GetPrices(request, (CompetenceAndSpecialistLevel)request.CompetenceLevel, model.ExpectedTravelCosts)
                        );

                        _notificationService.RequestReplamentOrderAccepted(request);
                    }
                    _dbContext.SaveChanges();
                    return RedirectToAction("Index", "Home", new { message = model.Status == RequestStatus.AcceptedNewInterpreterAppointed ? "Tolk har bytts ut för uppdraget" : "Svar har skickats" });
                }
                return Forbid();
            }
            return RedirectToAction(nameof(Process), new { id = model.RequestId });
        }

        private InterpreterBroker GetInterpreter(int interpreterBrokerId, InterpreterInformation interpreterInformation, int brokerId)
        {
            if (interpreterBrokerId == SelectListService.NewInterpreterId)
            {
                var interpreter = new InterpreterBroker(
                    interpreterInformation.FirstName,
                    interpreterInformation.LastName,
                    brokerId,
                    interpreterInformation.Email,
                    interpreterInformation.PhoneNumber,
                    interpreterInformation.OfficialInterpreterId
                );
                _dbContext.Add(interpreter);
                return interpreter;
            }
            return _dbContext.InterpreterBrokers.Single(i => i.InterpreterBrokerId == interpreterBrokerId);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Cancel(RequestCancelModel model)
        {
            if (ModelState.IsValid)
            {
                var request = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order.CreatedByUser)
                .Include(r => r.Order.ContactPersonUser)
                .Include(r => r.Interpreter)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Single(r => r.RequestId == model.RequestId && r.Status == RequestStatus.Approved);
                if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Cancel)).Succeeded)
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
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder).ThenInclude(r => r.Requests)
                .Include(r => r.Interpreter)
                .Single(r => r.RequestId == model.RequestId);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.Accept)).Succeeded)
            {
                await _requestService.Decline(request, _clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.DenyMessage);
                _dbContext.SaveChanges();
                return RedirectToAction("Index", "Home", new { message = "Svar har skickats" });
            }

            return Forbid();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ConfirmCancellation(int requestId)
        {
            return await ConfirmNewRequestStatus(requestId, RequestStatus.CancelledByCreatorWhenApproved, "Avbokning är bekräftad");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ConfirmDenial(int requestId)
        {
            return await ConfirmNewRequestStatus(requestId, RequestStatus.DeniedByCreator, "Avböjande är bekräftat");
        }

        private async Task<IActionResult> ConfirmNewRequestStatus(int requestId, RequestStatus expectedStatus, string infoMessage)
        {
            var request = _dbContext.Requests
                .Include(r => r.Ranking)
                .Include(r => r.Order)
                .Single(r => r.RequestId == requestId);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded && request.Status == expectedStatus)
            {
                _dbContext.Add(new RequestStatusConfirmation { RequestId = requestId, ConfirmedBy = User.GetUserId(), ImpersonatingConfirmedBy = User.TryGetImpersonatorId(), RequestStatus = request.Status, ConfirmedAt = _clock.SwedenNow });
                _dbContext.SaveChanges();
                return RedirectToAction("Index", "Home", new { message = infoMessage });
            }
            return Forbid();
        }


        private RequestModel GetModel(Request request, bool includeLog = false)
        {
            var model = RequestModel.GetModelFromRequest(request);
            model.OrderModel.ActiveRequest = model; //We're only interested in the request we have access to
            model.RequestCalculatedPriceInformationModel = GetPriceinformationToDisplay(request);
            model.OrderCalculatedPriceInformationModel = GetPriceinformationOrderToDisplay(request, model.OrderModel.RequestedCompetenceLevels);
            if (request.InterpreterLocation != null)
            {
                model.InterpreterLocationAnswer = (InterpreterLocation)request.InterpreterLocation.Value;
            }
            if (request.Status == RequestStatus.CancelledByCreatorWhenApproved)
            {
                model.Info48HCancelledByCustomer = _dateCalculationService.GetNoOf24HsPeriodsWorkDaysBetween(request.CancelledAt.Value.DateTime, request.Order.StartAt.DateTime) < 2 ? "Detta är en avbokning som skett med mindre än 48 timmar till tolkuppdragets start. Därmed utgår full ersättning, inklusive bland annat spilltid och förmedlingsavgift, i de fall något ersättningsuppdrag inte kan ordnas av kund. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna." : "Detta är en avbokning som skett med mer än 48 timmar till tolkuppdragets start. Därmed utgår förmedlingsavgift till leverantören. Obs: Lördagar, söndagar och helgdagar räknas inte in i de 48 timmarna.";
            }
            model.BrokerId = request.Ranking.BrokerId;
            model.AllowInterpreterChange = (request.Status == RequestStatus.Approved || request.Status == RequestStatus.Accepted || request.Status == RequestStatus.AcceptedNewInterpreterAppointed) && request.Order.StartAt > _clock.SwedenNow;
            model.AllowRequisitionRegistration = (request.Status == RequestStatus.Approved) && !request.Requisitions.Any() && request.Order.StartAt < _clock.SwedenNow;
            model.AllowCancellation = request.Order.StartAt > _clock.SwedenNow && _authorizationService.AuthorizeAsync(User, request, Policies.Cancel).Result.Succeeded;
            model.AllowConfirmationDenial = request.Status == RequestStatus.DeniedByCreator && !request.RequestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.DeniedByCreator);
            model.AllowConfirmCancellation = request.Status == RequestStatus.CancelledByCreatorWhenApproved && !request.RequestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.CancelledByCreatorWhenApproved);
            if (includeLog)
            {
                model.EventLog = new EventLogModel
                {
                    Entries = EventLogHelper.GetEventLog(request,
                    previousRequests: _dbContext.Requests
                        .Include(r => r.ReceivedByUser).ThenInclude(u => u.Broker)
                        .Include(r => r.AnsweringUser).ThenInclude(u => u.Broker)
                        .Include(r => r.ProcessingUser).ThenInclude(u => u.CustomerOrganisation)
                        .Include(r => r.CancelledByUser).ThenInclude(u => u.CustomerOrganisation)
                        .Include(r => r.CancelledByUser).ThenInclude(u => u.Broker)
                        .Include(r => r.ReplacedByRequest).ThenInclude(rbr => rbr.AnsweringUser).ThenInclude(u => u.Broker)
                        .Include(r => r.RequestStatusConfirmations).ThenInclude(rs => rs.ConfirmedByUser)
                        .Include(r => r.Requisitions)
                        .Include(r => r.Complaints)
                        .Where(r => r.OrderId == request.OrderId && r.RequestId != request.RequestId))
                    .OrderBy(e => e.Timestamp).ToList()
                };
            }
            return model;
        }

        private PriceInformationModel GetPriceinformationOrderToDisplay(Request request, List<CompetenceAndSpecialistLevel> requestedCompetenceLevels)
        {
            return new PriceInformationModel
            {
                PriceInformationToDisplay = _priceCalculationService.GetPriceInformationToDisplay(
                    _priceCalculationService.GetPrices(request, OrderService.SelectCompetenceLevelForPriceEstimation(requestedCompetenceLevels), null).PriceRows),
                Header = "Beräknat pris enligt bokningsförfrågan",
                UseDisplayHideInfo = true
            };
        }

        private PriceInformationModel GetPriceinformationToDisplay(Request request)
        {
            if (request.PriceRows == null || !request.PriceRows.Any())
            {
                return null;
            }
            return new PriceInformationModel
            {
                PriceInformationToDisplay = _priceCalculationService.GetPriceInformationToDisplay(request.PriceRows.OfType<PriceRowBase>().ToList()),
                Header = "Beräknat pris enligt bokningsbekräftelse",
                UseDisplayHideInfo = true
            };
        }
    }
}
