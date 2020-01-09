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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This is a public api, do not return 500")]
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
                if (!await _dbContext.OrderGroups
                    .AnyAsync(o => o.OrderGroupNumber == model.OrderGroupNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.RequestGroups.Any(r => r.Ranking.BrokerId == brokerId)))
                {
                    return ReturnError(ErrorCodes.OrderGroupNotFound);
                }

                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                var requestGroup = await _dbContext.RequestGroups
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.Requests).ThenInclude(r => r.RequirementAnswers)
                    .Include(r => r.Requests).ThenInclude(r => r.PriceRows)
                    .Include(r => r.Requests).ThenInclude(r => r.Order).ThenInclude(o => o.Requests)
                    .Include(r => r.Requests).ThenInclude(r => r.Order).ThenInclude(o => o.InterpreterLocations)
                    .Include(r => r.Requests).ThenInclude(r => r.Order).ThenInclude(o => o.CompetenceRequirements)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.CustomerUnit)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.CreatedByUser)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.Requirements)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.InterpreterLocations)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.CompetenceRequirements)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.Language)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.ContactPersonUser)
                    .SingleOrDefaultAsync(r =>
                        r.OrderGroup.OrderGroupNumber == model.OrderGroupNumber &&
                        brokerId == r.Ranking.BrokerId &&
                        (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received));
                if (requestGroup == null)
                {
                    return ReturnError(ErrorCodes.RequestGroupNotFound);
                }
                var mainInterpreterAnswer = _apiUserService.GetInterpreterModel(model.InterpreterAnswer, brokerId);

                InterpreterAnswerDto extraInterpreterAnswer = null;
                if (requestGroup.HasExtraInterpreter)
                {
                    extraInterpreterAnswer = _apiUserService.GetInterpreterModel(model.ExtraInterpreterAnswer, brokerId, false);
                }
                var now = _timeService.SwedenNow;
                if (requestGroup.Status == RequestStatus.Created)
                {
                    _requestService.AcknowledgeGroup(requestGroup, now, user?.Id ?? apiUserId, (user != null ? (int?)apiUserId : null));
                }
                try
                {
                    await _requestService.AcceptGroup(
                        requestGroup,
                        now,
                        user?.Id ?? apiUserId,
                        (user != null ? (int?)apiUserId : null),
                        EnumHelper.GetEnumByCustomName<InterpreterLocation>(model.InterpreterLocation).Value,
                        mainInterpreterAnswer,
                        extraInterpreterAnswer,
                        //Does not handle attachments yet.
                        new List<RequestGroupAttachment>()
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
                var order = await _apiOrderService.GetOrderGroupAsync(model.OrderGroupNumber, brokerId);
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                var requestGroup = await _dbContext.RequestGroups
                    .SingleOrDefaultAsync(r => r.OrderGroup.OrderGroupNumber == model.OrderGroupNumber && brokerId == r.Ranking.BrokerId && r.Status == RequestStatus.Created);
                if (requestGroup == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                _requestService.AcknowledgeGroup(requestGroup, _timeService.SwedenNow, user?.Id ?? apiUserId, (user != null ? (int?)apiUserId : null));
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
                var order = await _apiOrderService.GetOrderGroupAsync(model.OrderGroupNumber, brokerId);
                //Possibly the user should be added, if not found?? 
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                var request = await _dbContext.RequestGroups
                    .Include(r => r.OrderGroup).ThenInclude(o => o.RequestGroups).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.CreatedByUser)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.Requests)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.CustomerUnit)
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .SingleOrDefaultAsync(r => r.OrderGroup.OrderGroupNumber == model.OrderGroupNumber &&
                        //Must have a request connected to the order for the broker, any status...
                        r.Ranking.BrokerId == brokerId &&
                        //Possibly other statuses
                        (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received));
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestGroupNotFound);
                }
                await _requestService.DeclineGroup(request, _timeService.SwedenNow, user?.Id ?? apiUserId, (user != null ? (int?)apiUserId : null), model.Message);
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
                var order = await _apiOrderService.GetOrderGroupAsync(model.OrderGroupNumber, brokerId);
                //Get User, if any...
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                RequestGroup requestGroup = await GetConfirmedRequestGroup(model.OrderGroupNumber, brokerId, new[] { RequestStatus.DeniedByCreator });
                await _requestService.ConfirmGroupDenial(
                    requestGroup,
                    _timeService.SwedenNow,
                    user?.Id ?? apiUserId,
                    (user != null ? (int?)apiUserId : null)
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This is a public api, do not return 500")]
        public async Task<IActionResult> View(string orderGroupNumber, string callingUser)
        {
            _logger.LogInformation($"'{callingUser ?? "Unspecified user"}' called {nameof(View)} for the active request for the order group {orderGroupNumber}");
            try
            {
                var brokerId = User.TryGetBrokerId().Value;

                var requestGroup = await _dbContext.RequestGroups
                    .Include(r => r.OrderGroup).ThenInclude(o => o.CustomerUnit)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.CreatedByUser)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.Requirements)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.InterpreterLocations)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.CompetenceRequirements)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.Language)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.Attachments).ThenInclude(a => a.Attachment)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.ContactPersonUser)
                    .Include(o => o.Attachments).ThenInclude(a => a.Attachment)
                    .SingleOrDefaultAsync(r => r.OrderGroup.OrderGroupNumber == orderGroupNumber &&
                        //Must have a request connected to the order for the broker, any status...
                        r.Ranking.BrokerId == brokerId);
                if (requestGroup == null)
                {
                    return ReturnError(ErrorCodes.OrderGroupNotFound);
                }
                //End of service
                return Ok(_apiOrderService.GetResponseFromRequestGroup(requestGroup));
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
        public IActionResult File(string orderGroupNumber, int attachmentId, string callingUser)
        {
            _logger.LogInformation($"{callingUser} called {nameof(File)} to get the attachment {attachmentId} on order group {orderGroupNumber}");

            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var orderGroup = _dbContext.OrderGroups
                    .Include(o => o.Attachments).ThenInclude(a => a.Attachment)
                    .SingleOrDefault(o => o.OrderGroupNumber == orderGroupNumber &&
                        //Must have a request connected to the order for the broker, any status...
                        o.RequestGroups.Any(r => r.Ranking.BrokerId == brokerId));
                if (orderGroup == null)
                {
                    return ReturnError(ErrorCodes.OrderGroupNotFound);
                }

                var attachment = orderGroup.Attachments.Where(a => a.AttachmentId == attachmentId).SingleOrDefault()?.Attachment;
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

        private async Task<RequestGroup> GetConfirmedRequestGroup(string orderGroupNumber, int brokerId, IEnumerable<RequestStatus> expectedStatuses)
        {
            var requestGroup = await _dbContext.RequestGroups
                .Include(r => r.Ranking)
                .Include(r => r.StatusConfirmations)
                .SingleOrDefaultAsync(r => r.OrderGroup.OrderGroupNumber == orderGroupNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    r.Ranking.BrokerId == brokerId && expectedStatuses.Contains(r.Status));
            if (requestGroup == null)
            {
                throw new InvalidApiCallException(ErrorCodes.RequestGroupNotFound);
            }

            return requestGroup;
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
