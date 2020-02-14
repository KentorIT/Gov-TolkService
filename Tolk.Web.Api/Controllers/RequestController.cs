using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Enums;
using Tolk.Api.Payloads.Responses;
using Tolk.Api.Payloads.WebHookPayloads;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Api.Authorization;
using Tolk.Web.Api.Exceptions;
using Tolk.Web.Api.Helpers;
using Tolk.Web.Api.Services;

namespace Tolk.Web.Api.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    [Authorize(Policies.Broker)]
    public class RequestController : ControllerBase
    {
        private readonly TolkDbContext _dbContext;
        private readonly RequestService _requestService;
        private readonly ApiUserService _apiUserService;
        private readonly ISwedishClock _timeService;
        private readonly ApiOrderService _apiOrderService;
        private readonly ILogger _logger;

        public RequestController(
            TolkDbContext tolkDbContext,
            RequestService requestService,
            ApiUserService apiUserService,
            ISwedishClock timeService,
            ApiOrderService apiOrderService,
            ILogger<RequestController> logger)
        {
            _dbContext = tolkDbContext;
            _apiUserService = apiUserService;
            _timeService = timeService;
            _requestService = requestService;
            _apiOrderService = apiOrderService;
            _logger = logger;
        }

        #region Updating Methods

        [HttpPost]
        public async Task<IActionResult> Answer([FromBody] RequestAnswerModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId();
                var apiUserId = User.UserId();
                var order = await _dbContext.Orders
                    .Include(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(o => o.Requests).ThenInclude(r => r.RequirementAnswers)
                    .Include(o => o.Requests).ThenInclude(r => r.PriceRows)
                    .Include(o => o.CustomerOrganisation)
                    .Include(o => o.CustomerUnit)
                    .Include(o => o.CreatedByUser)
                    .Include(o => o.ContactPersonUser)
                    .Include(o => o.Requirements)
                    .Include(o => o.InterpreterLocations)
                    .Include(o => o.CompetenceRequirements)
                    .Include(o => o.Language)
                    .SingleOrDefaultAsync(o => o.OrderNumber == model.OrderNumber &&
                        //Must have a request connected to the order for the broker, any status...
                        o.Requests.Any(r => r.Ranking.BrokerId == User.TryGetBrokerId()));
                if (order == null)
                {
                    return ReturnError(ErrorCodes.OrderNotFound);
                }
                if (order.OrderGroupId != null)
                {
                    return ReturnError(ErrorCodes.RequestIsPartOfAGroup);
                }

                //Possibly the user should be added, if not found?? 
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId.Value);
                var request = order.Requests.SingleOrDefault(r =>
                    brokerId == r.Ranking.BrokerId &&
                    //Possibly other statuses, but this code is only temporary. Should be coalesced with the controller code.
                    (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received));
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                InterpreterBroker interpreter;
                try
                {
                    interpreter = _apiUserService.GetInterpreter(new InterpreterDetailsModel(model.Interpreter), brokerId.Value);
                }
                catch (InvalidOperationException)
                {
                    return ReturnError(ErrorCodes.InterpreterOfficialIdAlreadySaved);
                }

                //Does not handle Kammarkollegiets tolknummer
                if (interpreter == null)
                {
                    //Possibly the interpreter should be added, if not found?? 
                    return ReturnError(ErrorCodes.InterpreterNotFound);
                }
                if (model.Location == null)
                {
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, "Location was missing");
                }
                if (model.CompetenceLevel == null)
                {
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, "CompetenceLevel was missing");
                }
                var now = _timeService.SwedenNow;
                if (request.Status == RequestStatus.Created)
                {
                    _requestService.Acknowledge(request, now, user?.Id ?? apiUserId, user != null ? (int?)apiUserId : null);
                }
                try
                {
                    await _requestService.Accept(
                        request,
                        now,
                        user?.Id ?? apiUserId,
                        (user != null ? (int?)apiUserId : null),
                        interpreter,
                        EnumHelper.GetEnumByCustomName<InterpreterLocation>(model.Location).Value,
                        EnumHelper.GetEnumByCustomName<CompetenceAndSpecialistLevel>(model.CompetenceLevel).Value,
                        model.RequirementAnswers == null ? new List<OrderRequirementRequestAnswer>() :
                        model.RequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                        {
                            Answer = ra.Answer,
                            CanSatisfyRequirement = ra.CanMeetRequirement,
                            OrderRequirementId = ra.RequirementId,
                        }).ToList(),
                        //Does not handle attachments yet.
                        new List<RequestAttachment>(),
                        model.ExpectedTravelCosts,
                        model.ExpectedTravelCostInfo,
                        model.LatestAnswerTimeForCustomer //check if answer contains interpreter location with travel, else give error message or set to null?
                    );
                    await _dbContext.SaveChangesAsync();
                    //End of service
                    return Ok(new AnswerResponse { InterpreterId = interpreter.InterpreterBrokerId });
                }
                catch (InvalidOperationException ex)
                {
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, ex.Message);
                }
                catch (ArgumentNullException ex)
                {
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, ex.Message);
                }
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Acknowledge([FromBody] RequestAcknowledgeModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();
                var order = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);
                if (order.OrderGroupId != null)
                {
                    return ReturnError(ErrorCodes.RequestIsPartOfAGroup);
                }
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                var request = await _dbContext.Requests
                    .Include(r => r.Order)
                    .SingleOrDefaultAsync(r => r.Order.OrderNumber == model.OrderNumber && brokerId == r.Ranking.BrokerId && r.Status == RequestStatus.Created);
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                _requestService.Acknowledge(request, _timeService.SwedenNow, user?.Id ?? apiUserId, (user != null ? (int?)apiUserId : null));
                await _dbContext.SaveChangesAsync();
                //End of service
                return Ok(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Decline([FromBody] RequestDeclineModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();
                var order = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);
                if (order.OrderGroupId != null)
                {
                    return ReturnError(ErrorCodes.RequestIsPartOfAGroup);
                }
                //Possibly the user should be added, if not found?? 
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                var request = await _dbContext.Requests
                    .Include(r => r.Order).ThenInclude(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.Order.CreatedByUser)
                    .Include(r => r.Order.CustomerUnit)
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder).ThenInclude(r => r.Requests)
                    .SingleOrDefaultAsync(r => r.Order.OrderNumber == model.OrderNumber &&
                        //Must have a request connected to the order for the broker, any status...
                        r.Ranking.BrokerId == brokerId &&
                        //Possibly other statuses, but this code is only temporary. Should be coalesced with the controller code.
                        (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received));
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                await _requestService.Decline(request, _timeService.SwedenNow, user?.Id ?? apiUserId, (user != null ? (int?)apiUserId : null), model.Message);
                await _dbContext.SaveChangesAsync();
                //End of service
                return Ok(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Cancel([FromBody] RequestCancelModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();
                var order = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);
                //Possibly the user should be added, if not found?? 
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);

                var request = await _dbContext.Requests
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerUnit)
                    .Include(r => r.Order.CreatedByUser)
                    .Include(r => r.Order.ContactPersonUser)
                    .Include(r => r.Interpreter)
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .SingleOrDefaultAsync(r => r.Order.OrderNumber == model.OrderNumber &&
                //Must have a request connected to the order for the broker, any status...
                r.Ranking.BrokerId == brokerId &&
                //TODO: Possibly other statuses, but this code is only temporary. Should be coalesced with the controller code.
                (r.Status == RequestStatus.Approved));
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                try
                {
                    _requestService.CancelByBroker(request, _timeService.SwedenNow, user?.Id ?? apiUserId, (user != null ? (int?)apiUserId : null), model.Message);
                    await _dbContext.SaveChangesAsync();

                }
                catch (InvalidOperationException)
                {
                    //TODO: Should log the acctual exception here!!
                    return ReturnError(ErrorCodes.RequestNotInCorrectState);
                }

                //End of service
                return Ok(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeInterpreter([FromBody] RequestAnswerModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();
                var order = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);
                //Possibly the user should be added, if not found?? 
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                var request = await _dbContext.Requests
                    .Include(r => r.RequestGroup)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerUnit)
                    .Include(r => r.Order).ThenInclude(o => o.Requests).ThenInclude(r => r.PriceRows)
                    .Include(r => r.Order).ThenInclude(o => o.Requirements)
                    .Include(r => r.Order).ThenInclude(o => o.InterpreterLocations)
                    .Include(r => r.Order).ThenInclude(o => o.CompetenceRequirements)
                    .Include(r => r.Order).ThenInclude(o => o.IsExtraInterpreterForOrder).ThenInclude(r => r.Requests)
                    .Include(r => r.Order).ThenInclude(o => o.ExtraInterpreterOrder).ThenInclude(r => r.Requests)
                    .Include(r => r.Order.CreatedByUser)
                    .Include(r => r.Order.ContactPersonUser)
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .SingleOrDefaultAsync(r => r.Order.OrderNumber == model.OrderNumber &&
                        r.Ranking.BrokerId == brokerId &&
                        (r.Status == RequestStatus.Approved ||
                        r.Status == RequestStatus.Created ||
                        r.Status == RequestStatus.Received ||
                        r.Status == RequestStatus.InterpreterReplaced ||
                        r.Status == RequestStatus.Accepted));
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                InterpreterBroker interpreter;
                try
                {
                    interpreter = _apiUserService.GetInterpreter(new InterpreterDetailsModel(model.Interpreter), brokerId);
                }
                catch (InvalidOperationException)
                {
                    return ReturnError(ErrorCodes.InterpreterOfficialIdAlreadySaved);
                }
                if (interpreter == null)
                {
                    //Possibly the interpreter should be added, if not found?? 
                    return ReturnError(ErrorCodes.InterpreterNotFound);
                }
                if (model.Location == null)
                {
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, "Location was missing");
                }
                if (model.CompetenceLevel == null)
                {
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, "CompetenceLevel was missing");
                }
                try
                {
                    await _requestService.ChangeInterpreter(
                        request,
                        _timeService.SwedenNow,
                        user?.Id ?? apiUserId,
                        (user != null ? (int?)apiUserId : null),
                        interpreter,
                        EnumHelper.GetEnumByCustomName<InterpreterLocation>(model.Location).Value,
                        EnumHelper.GetEnumByCustomName<CompetenceAndSpecialistLevel>(model.CompetenceLevel).Value,
                        model.RequirementAnswers == null ? new List<OrderRequirementRequestAnswer>() :
                        model.RequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                        {
                            Answer = ra.Answer,
                            CanSatisfyRequirement = ra.CanMeetRequirement,
                            OrderRequirementId = ra.RequirementId,
                        }).ToList(),
                        //Does not handle attachments yet.
                        new List<RequestAttachment>(),
                        model.ExpectedTravelCosts,
                        model.ExpectedTravelCostInfo,
                        model.LatestAnswerTimeForCustomer);
                    await _dbContext.SaveChangesAsync();
                }
                catch (InvalidOperationException ex)
                {
                    return ReturnError(ErrorCodes.RequestNotInCorrectState, ex.Message);
                }
                return Ok(new ChangeInterpreterResponse { InterpreterId = interpreter.InterpreterBrokerId });
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AcceptReplacement([FromBody] RequestAcceptReplacementModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();

                var order = _dbContext.Orders
                .Include(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(o => o.Requests).ThenInclude(r => r.PriceRows)
                .Include(o => o.Requests).ThenInclude(r => r.Order)
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.CustomerUnit)
                .Include(o => o.CreatedByUser)
                .Include(o => o.ContactPersonUser)
                .SingleOrDefault(o => o.OrderNumber == model.OrderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.Requests.Any(r => r.Ranking.BrokerId == brokerId));
                if (order == null)
                {
                    return ReturnError(ErrorCodes.OrderNotFound);
                }
                //Possibly the user should be added, if not found?? 
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                var request = order.Requests.SingleOrDefault(r =>
                    brokerId == r.Ranking.BrokerId &&
                    r.Order.ReplacingOrderId != null &&
                    //Possibly other statuses
                    (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received));
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                if (model.Location == null)
                {
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, "Location was missing");
                }
                var now = _timeService.SwedenNow;
                //Add transaction here!!!
                if (request.Status == RequestStatus.Created)
                {
                    request.Received(now, user?.Id ?? apiUserId, (user != null ? (int?)apiUserId : null));
                }
                _requestService.AcceptReplacement(
                    request,
                    now,
                    user?.Id ?? apiUserId,
                    (user != null ? (int?)apiUserId : null),
                    EnumHelper.GetEnumByCustomName<InterpreterLocation>(model.Location).Value,
                    model.ExpectedTravelCosts,
                    model.ExpectedTravelCostInfo,
                    model.LatestAnswerTimeForCustomer
                );
                _dbContext.SaveChanges();
                //End of service
                return Ok(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmDenial([FromBody] ConfirmDenialModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();
                var order = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);
                //Get User, if any...
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                Request request = await GetConfirmedRequest(model.OrderNumber, brokerId, new[] { RequestStatus.DeniedByCreator });
                await _requestService.ConfirmDenial(
                    request,
                    _timeService.SwedenNow,
                    user?.Id ?? apiUserId,
                    (user != null ? (int?)apiUserId : null)
                );
                //Do The magic
                return Ok(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }


        [HttpPost]
        public async Task<IActionResult> ConfirmNoAnswer([FromBody] ConfirmNoAnswerModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();
                var order = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);
                //Get User, if any...
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                Request request = await GetConfirmedRequest(model.OrderNumber, brokerId, new[] { RequestStatus.ResponseNotAnsweredByCreator });
                await _requestService.ConfirmNoAnswer(
                    request,
                    _timeService.SwedenNow,
                    user?.Id ?? apiUserId,
                    user != null ? (int?)apiUserId : null
                );
                return Ok(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmChange([FromBody] ConfirmChangeModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();
                var order = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);

                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                Request request = await GetOrderChangedRequest(model.OrderNumber, brokerId);
                var allNonConfirmedOrderChanges = request.Order.OrderChangeLogEntries.Where(oc => oc.BrokerId == brokerId && oc.OrderChangeConfirmation == null && oc.OrderChangeLogType != OrderChangeLogType.ContactPerson).ToList();
                await _requestService.ConfirmOrderChange(request, allNonConfirmedOrderChanges.Select(c => c.OrderChangeLogEntryId).ToList(), _timeService.SwedenNow, user?.Id ?? apiUserId, user != null ? (int?)apiUserId : null);
                return Ok(new ConfirmChangeResponse { ConfirmedChanges = allNonConfirmedOrderChanges.Select(o => new ConfirmedChangeModel { ChangedAt = o.LoggedAt, ChangeType = o.OrderChangeLogType.GetCustomName() }) });
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmCancellation([FromBody] ConfirmCancellationModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();
                var order = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);
                //Get User, if any...
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                Request request = await GetConfirmedRequest(model.OrderNumber, brokerId, new[] { RequestStatus.CancelledByCreator, RequestStatus.CancelledByCreatorWhenApproved });
                await _requestService.ConfirmCancellation(
                    request,
                    _timeService.SwedenNow,
                    user?.Id ?? apiUserId,
                    user != null ? (int?)apiUserId : null
                );
                return Ok(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmNoRequisition([FromBody] ConfirmNoRequisitionModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();
                var order = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);
                //Get User, if any...
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                Request request = await GetConfirmedRequest(model.OrderNumber, brokerId, new[] { RequestStatus.Approved });
                await _requestService.ConfirmNoRequisition(
                    request,
                    _timeService.SwedenNow,
                    user?.Id ?? apiUserId,
                    user != null ? (int?)apiUserId : null
                );
                return Ok(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        #endregion

        #region getting methods

        [HttpGet]
        public IActionResult File(string orderNumber, int attachmentId, string callingUser)
        {
            _logger.LogInformation($"{callingUser} called {nameof(File)} to get the attachment {attachmentId} on order {orderNumber}");

            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var order = _dbContext.Orders
                    .Include(o => o.Requests).ThenInclude(r => r.Ranking)
                    .Include(o => o.Attachments).ThenInclude(a => a.Attachment)
                    .Include(o => o.Group).ThenInclude(og => og.Attachments).ThenInclude(a => a.Attachment).ThenInclude(at => at.OrderAttachmentHistoryEntries).ThenInclude(oh => oh.OrderChangeLogEntry)
                    .SingleOrDefault(o => o.OrderNumber == orderNumber &&
                        //Must have a request connected to the order for the broker, any status...
                        o.Requests.Any(r => r.Ranking.BrokerId == brokerId));
                if (order == null)
                {
                    return ReturnError(ErrorCodes.OrderNotFound);
                }

                var attachment = order.Attachments.Where(a => a.AttachmentId == attachmentId).SingleOrDefault()?.Attachment;
                if (attachment == null)
                {
                    attachment = order.OrderGroupId.HasValue ?
                        order.Group.Attachments
                        .Where(oa => !oa.Attachment.OrderAttachmentHistoryEntries.Any(h => h.OrderGroupAttachmentRemoved && h.OrderChangeLogEntry.OrderId == order.OrderId)
                            && oa.AttachmentId == attachmentId).SingleOrDefault(a => a.AttachmentId == attachmentId)?.Attachment
                        : null;
                }
                if (attachment == null)
                {
                    return ReturnError(ErrorCodes.AttachmentNotFound);
                }
                return Ok(new FileResponse
                {
                    FileBase64 = Convert.ToBase64String(attachment.Blob)
                });
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpGet]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This is a public api, do not return 500")]
        public async Task<IActionResult> View(string orderNumber, string callingUser)
        {
            _logger.LogInformation($"'{callingUser ?? "Unspecified user"}' called {nameof(View)} for the active request for the order {orderNumber}");
            try
            {
                var brokerId = User.TryGetBrokerId().Value;

                //GET THE MOST CURRENT REQUEST, IE THE REQUEST WITHOUT ReplacedBy....
                var request = await _dbContext.Requests
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.RequirementAnswers)
                    .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                    .Include(r => r.Interpreter)
                    .Include(r => r.Order).ThenInclude(o => o.CreatedByUser)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerUnit)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Order).ThenInclude(o => o.Region)
                    .Include(r => r.Order).ThenInclude(o => o.Language)
                    .Include(r => r.Order).ThenInclude(o => o.Requirements)
                    .Include(r => r.Order).ThenInclude(o => o.InterpreterLocations)
                    .Include(r => r.Order).ThenInclude(o => o.CompetenceRequirements)
                    .Include(r => r.Order).ThenInclude(o => o.PriceRows).ThenInclude(p => p.PriceListRow)
                    .SingleOrDefaultAsync(r => r.Order.OrderNumber == orderNumber &&
                        //Must have a request connected to the order for the broker, any status...
                        r.Ranking.BrokerId == brokerId &&
                        r.ReplacingRequest == null);
                if (request == null)
                {
                    return ReturnError(ErrorCodes.OrderNotFound);
                }
                //End of service
                return Ok(_apiOrderService.GetResponseFromRequest(request));
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Unexpected error occured when client called Request/{nameof(View)}");
                return ReturnError(ErrorCodes.UnspecifiedProblem);
            }
        }

        #endregion

        #region private methods

        private async Task<Request> GetConfirmedRequest(string orderNumber, int brokerId, IEnumerable<RequestStatus> expectedStatuses)
        {
            var request = await _dbContext.Requests
                .Include(r => r.Ranking)
                .Include(r => r.Order)
                .Include(r => r.RequestStatusConfirmations)
                .SingleOrDefaultAsync(r => r.Order.OrderNumber == orderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    r.Ranking.BrokerId == brokerId && expectedStatuses.Contains(r.Status));
            if (request == null)
            {
                throw new InvalidApiCallException(ErrorCodes.RequestNotFound);
            }
            return request;
        }

        private async Task<Request> GetOrderChangedRequest(string orderNumber, int brokerId)
        {
            var request = await _dbContext.Requests
                .Include(r => r.Ranking)
                .Include(r => r.Order).ThenInclude(o => o.OrderChangeLogEntries).ThenInclude(oc => oc.OrderChangeConfirmation).OrderBy(r => r.RequestId)
                .LastOrDefaultAsync(r => r.Order.OrderNumber == orderNumber && r.Ranking.BrokerId == brokerId);
            if (request == null)
            {
                throw new InvalidApiCallException(ErrorCodes.RequestNotFound);
            }
            return request;
        }

        //Break out to error generator service...
        private IActionResult ReturnError(string errorCode, string specifiedErrorMessage = null)
        {
            //TODO: Add to log, information...
            var message = TolkApiOptions.ErrorResponses.Single(e => e.ErrorCode == errorCode).Copy();
            if (!string.IsNullOrEmpty(specifiedErrorMessage))
            {
                message.ErrorMessage = specifiedErrorMessage;
            }
            return Ok(message);
        }

        #endregion
    }
}
