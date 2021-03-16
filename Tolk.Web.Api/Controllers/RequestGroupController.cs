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
using Tolk.BusinessLogic.Helpers;
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
    public class RequestGroupController : ControllerBase
    {
        private readonly TolkDbContext _dbContext;
        private readonly RequestService _requestService;
        private readonly ApiUserService _apiUserService;
        private readonly ISwedishClock _timeService;
        private readonly ApiOrderService _apiOrderService;
        private readonly ILogger _logger;

        public RequestGroupController(
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
        [ProducesResponseType(200, Type = typeof(GroupAnswerResponse))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Anropas för att svara på ett inkommet sammanhållet avrop")]
        [OpenApiTag("RequestGroup", AddToDocument = true, Description = "Grupp av metoder för att hantera sammanhållna beställningar(avrop)")]
        public async Task<IActionResult> Answer([FromBody] RequestGroupAnswerModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
 
                var requestGroup = await _dbContext.RequestGroups.GetFullRequestGroupForApiWithBrokerAndOrderNumber(model.OrderGroupNumber, brokerId);
                if (requestGroup == null)
                {
                    return ReturnError(ErrorCodes.RequestGroupNotFound);
                }
                if (!(requestGroup.Status == RequestStatus.Created || requestGroup.Status == RequestStatus.Received))
                {
                    return ReturnError(ErrorCodes.RequestGroupNotInCorrectState);
                }
                var mainInterpreterAnswer = _apiUserService.GetInterpreterModel(model.InterpreterAnswer, brokerId);

                InterpreterAnswerDto extraInterpreterAnswer = null;
                requestGroup.OrderGroup.Orders = await _dbContext.Orders.GetOrdersForOrderGroup(requestGroup.OrderGroup.OrderGroupId).ToListAsync();
                if (requestGroup.HasExtraInterpreter)
                {
                    extraInterpreterAnswer = _apiUserService.GetInterpreterModel(model.ExtraInterpreterAnswer, brokerId, false);
                }
                var now = _timeService.SwedenNow;
                if (requestGroup.Status == RequestStatus.Created)
                {
                    await _requestService.AcknowledgeGroup(requestGroup, now, user?.Id ?? apiUserId, (user != null ? (int?)apiUserId : null));
                }
                try
                {
                    await _requestService.AcceptGroup(
                        requestGroup,
                        now,
                        user?.Id ?? apiUserId,
                        user != null ? (int?)apiUserId : null,
                        EnumHelper.GetEnumByCustomName<InterpreterLocation>(model.InterpreterLocation).Value,
                        mainInterpreterAnswer,
                        extraInterpreterAnswer,
                        //Does not handle attachments yet.
                        new List<RequestGroupAttachment>(),
                        model.LatestAnswerTimeForCustomer
                    );
                    await _dbContext.SaveChangesAsync();
                    //End of service
                    return Ok(new GroupAnswerResponse
                    {
                        InterpreterId = mainInterpreterAnswer.Interpreter.InterpreterBrokerId,
                        ExtraInterpreterId = extraInterpreterAnswer?.Interpreter?.InterpreterBrokerId
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, ex.Message);
                }
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle request group answer");
                return ReturnError(ErrorCodes.UnspecifiedProblem);
            }
        }

        [HttpPost]
        [ProducesResponseType(200, Type = typeof(ResponseBase))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Anropas för att bekräfta mottagandet av ett sammanhållet avrop")]
        [OpenApiTag("RequestGroup")]
        public async Task<IActionResult> Acknowledge([FromBody] RequestGroupAcknowledgeModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();
                var requestGroup = await _apiOrderService.CheckOrderGroupAndGetRequestGroup(model.OrderGroupNumber, brokerId);
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                if (requestGroup.Status != RequestStatus.Created)
                {
                    return ReturnError(ErrorCodes.RequestGroupNotInCorrectState);
                }
                await _requestService.AcknowledgeGroup(requestGroup, _timeService.SwedenNow, user?.Id ?? apiUserId, (user != null ? (int?)apiUserId : null));
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
        [ProducesResponseType(200, Type = typeof(ResponseBase))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Anropas för att avstå tillsättning av ett sammanhållet avrop")]
        [OpenApiTag("RequestGroup")]
        public async Task<IActionResult> Decline([FromBody] RequestGroupDeclineModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();
                var requestGroup = await _apiOrderService.CheckOrderGroupAndGetRequestGroup(model.OrderGroupNumber, brokerId);
                //Possibly the user should be added, if not found?? 
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                if (!(requestGroup.Status == RequestStatus.Created || requestGroup.Status == RequestStatus.Received))
                {
                    return ReturnError(ErrorCodes.RequestGroupNotInCorrectState);
                }
                requestGroup = await _dbContext.RequestGroups.GetRequestGroupToProcessById(requestGroup.RequestGroupId);
                await _requestService.DeclineGroup(requestGroup, _timeService.SwedenNow, user?.Id ?? apiUserId, (user != null ? (int?)apiUserId : null), model.Message);
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
        [Description("Anropas för att kvittera en nekad tillsättning av ett sammanhållet avrop")]
        [OpenApiTag("RequestGroup")]
        public async Task<IActionResult> ConfirmDenial([FromBody] ConfirmGroupDenialModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();
                var requestGroup = await _apiOrderService.CheckOrderGroupAndGetRequestGroup(model.OrderGroupNumber, brokerId);
                if (requestGroup.Status != RequestStatus.DeniedByCreator)
                {
                    _logger.LogWarning($"Broker with broker id {brokerId}, tried to confirm denial {model.OrderGroupNumber}, but requestgroup not in correct status.");
                    throw new InvalidApiCallException(ErrorCodes.RequestGroupNotInCorrectState);
                }
                //Get User, if any...
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);

                await _requestService.AddRequestsWithConfirmationListsToRequestGroup(requestGroup);
                await _requestService.ConfirmGroupDenial(
                    requestGroup,
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
        [Description("Anropas för att kvittera information om uteblivet svar på tillsättning av ett sammanhållet avrop")]
        [OpenApiTag("RequestGroup")]
        public async Task<IActionResult> ConfirmNoAnswer([FromBody] ConfirmGroupNoAnswerModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();
                var requestGroup = await _apiOrderService.CheckOrderGroupAndGetRequestGroup(model.OrderGroupNumber, brokerId);
                //Get User, if any...
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                if (requestGroup.Status != RequestStatus.ResponseNotAnsweredByCreator)
                {
                    _logger.LogWarning($"Broker with broker id {brokerId}, tried to confirm no answer {model.OrderGroupNumber}, but requestgroup not in correct status.");
                    throw new InvalidApiCallException(ErrorCodes.RequestGroupNotInCorrectState);
                }
                await _requestService.AddRequestsWithConfirmationListsToRequestGroup(requestGroup);
                await _requestService.ConfirmGroupNoAnswer(
                    requestGroup,
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
        [Description("Anropas för att kvittera information om avbokning av ett sammanhållet avrop")]
        [OpenApiTag("RequestGroup")]
        public async Task<IActionResult> ConfirmCancellation([FromBody] ConfirmGroupCancellationModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();
                var requestGroup = await _apiOrderService.CheckOrderGroupAndGetRequestGroup(model.OrderGroupNumber, brokerId);
                //Get User, if any...
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                if (requestGroup.Status != RequestStatus.CancelledByCreator)
                {
                    _logger.LogWarning($"Broker with broker id {brokerId}, tried to confirm cancellation {model.OrderGroupNumber}, but requestgroup not in correct status.");
                    throw new InvalidApiCallException(ErrorCodes.RequestGroupNotInCorrectState);
                }
                await _requestService.AddRequestsWithConfirmationListsToRequestGroup(requestGroup);
                await _requestService.ConfirmGroupCancellation(
                    requestGroup,
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
        [ProducesResponseType(200, Type = typeof(RequestGroupDetailsResponse))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Returnerar all information om ett sammanhållet avrop")]
        [OpenApiTag("RequestGroup")]
        public async Task<IActionResult> View(string orderGroupNumber, string callingUser)
        {
            _logger.LogInformation($"'{callingUser ?? "Unspecified user"}' called {nameof(View)} for the active request for the order group {orderGroupNumber}");
            try
            {
                var brokerId = User.TryGetBrokerId().Value;

                var requestGroup = await _dbContext.RequestGroups.GetFullRequestGroupForApiWithBrokerAndOrderNumber(orderGroupNumber, brokerId);
                if (requestGroup == null)
                {
                    return ReturnError(ErrorCodes.OrderGroupNotFound);
                }
                return Ok(await _apiOrderService.GetResponseFromRequestGroup(requestGroup));
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Unexpected error occured when client called RequestGroup/{nameof(View)}");
                return ReturnError(ErrorCodes.UnspecifiedProblem);
            }
        }

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(FileResponse))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Returnerar en fil i base64 format, kopplad till ett specifikt uppdrag")]
        [OpenApiTag("RequestGroup")]
        public async Task<IActionResult> File(string orderGroupNumber, int attachmentId, string callingUser)
        {
            _logger.LogInformation($"{callingUser} called {nameof(File)} to get the attachment {attachmentId} on order group {orderGroupNumber}");

            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var requestGroup = await _dbContext.RequestGroups.GetRequestGroupForApiWithBrokerAndOrderNumber(orderGroupNumber, brokerId);
                if (requestGroup == null)
                {
                    return ReturnError(ErrorCodes.OrderGroupNotFound);
                }

                var attachments = await _dbContext.Attachments.GetAttachmentsForOrderGroup(requestGroup.OrderGroupId).ToListAsync();
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

        #endregion

        #region private methods

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
