using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSwag.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Responses;
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
        [ProducesResponseType(200, Type = typeof(AnswerResponse))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Anropas för att svara på ett inkommet avrop")]
        [OpenApiTag("Request", AddToDocument = true, Description = "Grupp av metoder för att hantera inkomna avrop")]
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

                var order = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId.Value);
                if (order.OrderGroupId != null)
                {
                    return ReturnError(ErrorCodes.RequestIsPartOfAGroup);
                }

                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId.Value);

                var request = await _dbContext.Requests.GetActiveRequestForApiWithBrokerAndOrderNumber(model.OrderNumber, User.TryGetBrokerId().Value);
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                if (!request.IsToBeProcessedByBroker)
                {
                    return ReturnError(ErrorCodes.RequestNotInCorrectState);
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
                    await _requestService.Answer(
                        request,
                        now,
                        user?.Id ?? apiUserId,
                        user != null ? (int?)apiUserId : null,
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
                        model.LatestAnswerTimeForCustomer,
                        model.BrokerReferenceNumber
                    );
                    await _dbContext.SaveChangesAsync();
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
        [ProducesResponseType(200, Type = typeof(ResponseBase))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Anropas för att bekräfta att man accepterar avropets krav, men utan tillsatt tolk")]
        [OpenApiTag("Request")]
        public async Task<IActionResult> Accept([FromBody] RequestAcceptModel model)
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
                var request = await _dbContext.Requests.GetSimpleActiveRequestForApiWithBrokerAndOrderNumber(model.OrderNumber, brokerId);
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                if (!request.CanAccept)
                {
                    return ReturnError(ErrorCodes.RequestNotInCorrectState);
                }
                if (!request.IsAnswerLevelAccept)
                {
                    return ReturnError(ErrorCodes.AcceptIsNotAllowedOnTheRequest);
                }
                if (model.Location == null)
                {
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, "Location was missing");
                }

                if (request.Order.SpecificCompetenceLevelRequired && model.CompetenceLevel == null)
                {
                    return ReturnError(ErrorCodes.AllRequirementsMustBeAnsweredOnAccept);
                }
                var now = _timeService.SwedenNow;
                await _requestService.Accept(
                        request,
                        now,
                        user?.Id ?? apiUserId,
                        user != null ? (int?)apiUserId : null,
                        EnumHelper.GetEnumByCustomName<InterpreterLocation>(model.Location).Value,
                        !string.IsNullOrEmpty(model.CompetenceLevel) ? EnumHelper.GetEnumByCustomName<CompetenceAndSpecialistLevel>(model.CompetenceLevel).Value : null,
                        model.RequirementAnswers == null ? new List<OrderRequirementRequestAnswer>() :
                        model.RequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                        {
                            Answer = ra.Answer,
                            CanSatisfyRequirement = ra.CanMeetRequirement,
                            OrderRequirementId = ra.RequirementId,
                        }).ToList(),
                        //Does not handle attachments yet.
                        new List<RequestAttachment>(),
                        model.BrokerReferenceNumber
                    );
                await _dbContext.SaveChangesAsync();
                return Ok(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Unexpected error occured when client called Request/{nameof(Accept)}");
                return ReturnError(ErrorCodes.UnspecifiedProblem);
            }
        }

        [HttpPost]
        [ProducesResponseType(200, Type = typeof(ResponseBase))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Anropas för att bekräfta mottagandet av ett avrop")]
        [OpenApiTag("Request")]
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
                var request = await _dbContext.Requests.GetSimpleActiveRequestForApiWithBrokerAndOrderNumber(model.OrderNumber, brokerId);
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                if (request.Status != RequestStatus.Created)
                {
                    return ReturnError(ErrorCodes.RequestNotInCorrectState);
                }
                _requestService.Acknowledge(request, _timeService.SwedenNow, user?.Id ?? apiUserId, user != null ? (int?)apiUserId : null);
                await _dbContext.SaveChangesAsync();

                return Ok(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        [ProducesResponseType(200, Type = typeof(ResponseBase))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Anropas för att avstå tillsättning av ett avrop")]
        [OpenApiTag("Request")]
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
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);

                var request = await _dbContext.Requests.GetActiveRequestForApiWithBrokerAndOrderNumber(model.OrderNumber, User.TryGetBrokerId().Value);
                request.Requisitions = new List<Requisition>();
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                if (!request.IsToBeProcessedByBroker)
                {
                    return ReturnError(ErrorCodes.RequestNotInCorrectState);
                }
                await _requestService.Decline(request, _timeService.SwedenNow, user?.Id ?? apiUserId, user != null ? (int?)apiUserId : null, model.Message);
                await _dbContext.SaveChangesAsync();

                return Ok(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        [ProducesResponseType(200, Type = typeof(ResponseBase))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Anropas för att avboka ett bokat tillfälle")]
        [OpenApiTag("Request")]
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
                _ = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);

                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);

                var request = await _dbContext.Requests.GetActiveRequestForApiWithBrokerAndOrderNumber(model.OrderNumber, User.TryGetBrokerId().Value);
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                if (request.Status != RequestStatus.Approved)
                {
                    return ReturnError(ErrorCodes.RequestNotInCorrectState);
                }
                try
                {
                    _requestService.CancelByBroker(request, _timeService.SwedenNow, user?.Id ?? apiUserId, user != null ? (int?)apiUserId : null, model.Message);
                    await _dbContext.SaveChangesAsync();
                }
                catch (InvalidOperationException)
                {
                    //TODO: Should log the acctual exception here!!
                    return ReturnError(ErrorCodes.RequestNotInCorrectState);
                }

                return Ok(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        [ProducesResponseType(200, Type = typeof(ChangeInterpreterResponse))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Anropas för att byta tolk på ett bokat tillfälle")]
        [OpenApiTag("Request")]
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
                _ = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);

                //Possibly the user should be added, if not found?? 
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);

                var request = await _dbContext.Requests.GetRequestForChangeInterpreterWithBrokerAndOrderNumber(model.OrderNumber, User.TryGetBrokerId().Value);

                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                if (!request.CanChangeInterpreter(_timeService.SwedenNow))
                {
                    return ReturnError(ErrorCodes.RequestNotInCorrectState);
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
                if (model.Location != null && EnumHelper.GetEnumByCustomName<InterpreterLocation>(model.Location).Value != (InterpreterLocation)request.InterpreterLocation.Value)
                {
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, "Location cannot be changed when changing interpreter");
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
                        model.LatestAnswerTimeForCustomer, 
                        model.BrokerReferenceNumber);
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
        [ProducesResponseType(200, Type = typeof(ResponseBase))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Anropas för godta ett ersättningsuppdrag")]
        [OpenApiTag("Request")]
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

                _ = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);

                //Possibly the user should be added, if not found?? 
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);

                var request = await _dbContext.Requests.GetActiveRequestForApiWithBrokerAndOrderNumber(model.OrderNumber, User.TryGetBrokerId().Value);

                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                if (!request.IsToBeProcessedByBroker)
                {
                    return ReturnError(ErrorCodes.RequestNotInCorrectState);
                }
                if (!request.Order.ReplacingOrderId.HasValue)
                {
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, "This is not a replacement order");
                }
                if (model.Location == null)
                {
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, "Location was missing");
                }
                var now = _timeService.SwedenNow;
                //Add transaction here!!!
                if (request.Status == RequestStatus.Created)
                {
                    _requestService.Acknowledge(request, now, user?.Id ?? apiUserId, (user != null ? (int?)apiUserId : null));
                }
                await _requestService.AcceptReplacement(
                    request,
                    now,
                    user?.Id ?? apiUserId,
                    (user != null ? (int?)apiUserId : null),
                    EnumHelper.GetEnumByCustomName<InterpreterLocation>(model.Location).Value,
                    model.ExpectedTravelCosts,
                    model.ExpectedTravelCostInfo,
                    model.LatestAnswerTimeForCustomer,
                    model.BrokerReferenceNumber
                );
                await _dbContext.SaveChangesAsync();

                return Ok(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        [ProducesResponseType(200, Type = typeof(ResponseBase))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Anropas för att kvittera en nekad tillsättning")]
        [OpenApiTag("Request")]
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
                _ = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);

                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                Request request = await GetConfirmedRequest(model.OrderNumber, brokerId, new[] { RequestStatus.DeniedByCreator });
                await _requestService.ConfirmDenial(
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
        [ProducesResponseType(200, Type = typeof(ResponseBase))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Anropas för att kvittera information om uteblivet svar på tillsättning")]
        [OpenApiTag("Request")]
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
                _ = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);

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
        [ProducesResponseType(200, Type = typeof(ResponseBase))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Anropas för att kvittera information om uppdatering av information om ett bokat uppdrag")]
        [OpenApiTag("Request")]
        public async Task<IActionResult> ConfirmUpdate([FromBody] ConfirmUpdateModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();
                _= await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);

                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                Request request = await GetOrderChangedRequest(model.OrderNumber, brokerId);
                var allNonConfirmedOrderChanges = request.Order.OrderChangeLogEntries.Where(oc => oc.BrokerId == brokerId && oc.OrderChangeConfirmation == null && oc.OrderChangeLogType != OrderChangeLogType.ContactPerson).ToList();
                await _requestService.ConfirmOrderChange(request, allNonConfirmedOrderChanges.Select(c => c.OrderChangeLogEntryId).ToList(), _timeService.SwedenNow, user?.Id ?? apiUserId, user != null ? (int?)apiUserId : null);
                return Ok(new ConfirmUpdateResponse { ConfirmedUpdates = allNonConfirmedOrderChanges.Select(o => new ConfirmedUpdateModel { UpdatedAt = o.LoggedAt, RequestUpdateType = o.OrderChangeLogType.GetCustomName() }) });
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        [ProducesResponseType(200, Type = typeof(ResponseBase))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Anropas för att kvittera information om avbokat uppdrag")]
        [OpenApiTag("Request")]
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
                _ = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);

                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                Request request = await GetConfirmedRequest(model.OrderNumber, brokerId, new[] { RequestStatus.CancelledByCreator, RequestStatus.CancelledByCreatorWhenApprovedOrAccepted });
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
        [ProducesResponseType(200, Type = typeof(ResponseBase))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Anropas för att markera att ett visst uppdrag inte kommer få rekvisition registrerad")]
        [OpenApiTag("Request")]
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
                _ = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);
               
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
        [ProducesResponseType(200, Type = typeof(FileResponse))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Returnerar en fil i base64 format, kopplad till ett specifikt uppdrag")]
        [OpenApiTag("Request")]
        public async Task<IActionResult> File(string orderNumber, int attachmentId, string callingUser)
        {
            _logger.LogInformation($"{callingUser} called {nameof(File)} to get the attachment {attachmentId} on order {orderNumber}");

            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                _ = await _apiOrderService.GetOrderAsync(orderNumber, brokerId);
                Request request = await _dbContext.Requests.GetSimpleActiveRequestForApiWithBrokerAndOrderNumber(orderNumber, brokerId);

                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                var attachments = await _dbContext.Attachments.GetAttachmentsForOrderAndGroup(request.OrderId, request.Order.OrderGroupId).ToListAsync();
                var attachment = attachments.Where(a => a.AttachmentId == attachmentId).SingleOrDefault();
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
        [ProducesResponseType(200, Type = typeof(RequestDetailsResponse))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Returnerar all information om ett visst uppdrag")]
        [OpenApiTag("Request")]
        public async Task<IActionResult> View(string orderNumber, string callingUser)
        {
            _logger.LogInformation($"'{callingUser ?? "Unspecified user"}' called {nameof(View)} for the active request for the order {orderNumber}");
            try
            {
                var request = await _dbContext.Requests.GetActiveRequestForApiWithBrokerAndOrderNumber(orderNumber, User.TryGetBrokerId().Value);
                if (request == null)
                {
                    return ReturnError(ErrorCodes.OrderNotFound);
                }
                return Ok(await _apiOrderService.GetResponseFromRequest(request));
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
            var request = await _dbContext.Requests.GetConfirmedRequestForApiWithBrokerAndOrderNumber(orderNumber, brokerId, expectedStatuses);
            if (request == null)
            {
                throw new InvalidApiCallException(ErrorCodes.RequestNotFound);
            }
            request.RequestStatusConfirmations = await _dbContext.RequestStatusConfirmation.GetStatusConfirmationsForRequest(request.RequestId).ToListAsync();
            return request;
        }

        private async Task<Request> GetOrderChangedRequest(string orderNumber, int brokerId)
        {
            var request = await _dbContext.Requests.GetSimpleActiveRequestForApiWithBrokerAndOrderNumber(orderNumber, brokerId);
            if (request == null)
            {
                throw new InvalidApiCallException(ErrorCodes.RequestNotFound);
            }
            request.Order.OrderChangeLogEntries = await _dbContext.OrderChangeLogEntries.GetOrderChangeLogEntitesForOrder(request.OrderId).ToListAsync();
            return request;
        }

        //Break out to error generator service...
        private IActionResult ReturnError(string errorCode, string specifiedErrorMessage = null)
        {
            //TODO: Add to log, information...
            var message = TolkApiOptions.BrokerApiErrorResponses.Union(TolkApiOptions.CommonErrorResponses).Single(e => e.ErrorCode == errorCode).Copy();
            if (!string.IsNullOrEmpty(specifiedErrorMessage))
            {
                message.ErrorMessage = specifiedErrorMessage;
            }
            return Ok(message);
        }

        #endregion
    }
}
