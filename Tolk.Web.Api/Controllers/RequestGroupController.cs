using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Enums;
using Tolk.Api.Payloads.Responses;
using Tolk.Api.Payloads.WebHookPayloads;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Api.Exceptions;
using Tolk.Web.Api.Helpers;
using Tolk.Web.Api.Services;

namespace Tolk.Web.Api.Controllers
{
    public class RequestGroupController : Controller
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
        public async Task<JsonResult> Answer([FromBody] RequestGroupAnswerModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var apiUser = await GetApiUser();
                if (!await _dbContext.OrderGroups
                    .AnyAsync(o => o.OrderGroupNumber == model.OrderGroupNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.RequestGroups.Any(r => r.Ranking.BrokerId == apiUser.BrokerId)))
                {
                    return ReturnError(ErrorCodes.OrderGroupNotFound);
                }
                //Possibly the user should be added, if not found?? 
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
                var requestGroup = await _dbContext.RequestGroups
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.Requests).ThenInclude(r => r.RequirementAnswers)
                    .Include(r => r.Requests).ThenInclude(r => r.PriceRows)
                    .Include(r => r.Requests).ThenInclude(r => r.Order).ThenInclude(o => o.Requests)
                    //.Include(r => r.OrderGroup).ThenInclude(o => o.CustomerUnit)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.CreatedByUser)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.ContactPersonUser)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.Requirements)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.InterpreterLocations)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.CompetenceRequirements)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.Language)
                    .SingleOrDefaultAsync(r =>
                        r.OrderGroup.OrderGroupNumber == model.OrderGroupNumber &&
                        apiUser.BrokerId == r.Ranking.BrokerId &&
                        (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received));
                if (requestGroup == null)
                {
                    return ReturnError(ErrorCodes.RequestGroupNotFound);
                }
                var mainInterpreterAnswer = _apiUserService.GetInterpreterModel(model.InterpreterAnswer, apiUser.BrokerId.Value);

                InterpreterAnswerDto extraInterpreterAnswer = null;
                if (requestGroup.HasExtraInterpreter)
                {
                    extraInterpreterAnswer = _apiUserService.GetInterpreterModel(model.ExtraInterpreterAnswer, apiUser.BrokerId.Value, false);
                }
                var now = _timeService.SwedenNow;
                if (requestGroup.Status == RequestStatus.Created)
                {
                    _requestService.AcknowledgeGroup(requestGroup, now, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null));
                }
                try
                {
                    await _requestService.AcceptGroup(
                        requestGroup,
                        now,
                        user?.Id ?? apiUser.Id,
                        (user != null ? (int?)apiUser.Id : null),
                        EnumHelper.GetEnumByCustomName<InterpreterLocation>(model.InterpreterLocation).Value,
                        mainInterpreterAnswer,
                        extraInterpreterAnswer,
                        //Does not handle attachments yet.
                        new List<RequestGroupAttachment>()
                    );
                    await _dbContext.SaveChangesAsync();
                    //End of service
                    return Json(new GroupAnswerResponse
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
                return ReturnError(ErrorCodes.UnspecifiedProblem);
            }
        }

        [HttpPost]
        public async Task<JsonResult> Acknowledge([FromBody] RequestGroupAcknowledgeModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var apiUser = await GetApiUser();
                var order = await _apiOrderService.GetOrderGroupAsync(model.OrderGroupNumber, apiUser.BrokerId.Value);
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
                var requestGroup = await _dbContext.RequestGroups
                    .SingleOrDefaultAsync(r => r.OrderGroup.OrderGroupNumber == model.OrderGroupNumber && apiUser.BrokerId == r.Ranking.BrokerId && r.Status == RequestStatus.Created);
                if (requestGroup == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                _requestService.AcknowledgeGroup(requestGroup, _timeService.SwedenNow, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null));
                await _dbContext.SaveChangesAsync();
                //End of service
                return Json(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        public async Task<JsonResult> Decline([FromBody] RequestGroupDeclineModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var apiUser = await GetApiUser();
                var order = await _apiOrderService.GetOrderGroupAsync(model.OrderGroupNumber, apiUser.BrokerId.Value);
                //Possibly the user should be added, if not found?? 
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
                var request = await _dbContext.RequestGroups
                    .Include(r => r.OrderGroup).ThenInclude(o => o.RequestGroups).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.CreatedByUser)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.Requests)
                    .Include(r => r.OrderGroup).ThenInclude(o => o.Orders).ThenInclude(o => o.CustomerUnit)
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .SingleOrDefaultAsync(r => r.OrderGroup.OrderGroupNumber == model.OrderGroupNumber &&
                        //Must have a request connected to the order for the broker, any status...
                        r.Ranking.BrokerId == apiUser.BrokerId &&
                        //Possibly other statuses
                        (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received));
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestGroupNotFound);
                }
                await _requestService.DeclineGroup(request, _timeService.SwedenNow, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null), model.Message);
                await _dbContext.SaveChangesAsync();
                //End of service
                return Json(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        public async Task<JsonResult> ConfirmDenial([FromBody] ConfirmGroupDenialModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var apiUser = await GetApiUser();
                var order = await _apiOrderService.GetOrderGroupAsync(model.OrderGroupNumber, apiUser.BrokerId.Value);
                //Get User, if any...
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
                RequestGroup requestGroup = await GetConfirmedRequestGroup(model.OrderGroupNumber, apiUser.BrokerId.Value, new[] { RequestStatus.DeniedByCreator });
                await _requestService.ConfirmGroupDenial(
                    requestGroup,
                    _timeService.SwedenNow,
                    user?.Id ?? apiUser.Id,
                    (user != null ? (int?)apiUser.Id : null)
                );
                return Json(new ResponseBase());
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
        public async Task<JsonResult> View(string orderGroupNumber, string callingUser)
        {
            _logger.LogInformation($"'{callingUser ?? "Unspecified user"}' called {nameof(View)} for the active request for the order group {orderGroupNumber}");
            try
            {
                var apiUser = await GetApiUser();

                var requestGroup = await _dbContext.RequestGroups
                    .SingleOrDefaultAsync(r => r.OrderGroup.OrderGroupNumber == orderGroupNumber &&
                        //Must have a request connected to the order for the broker, any status...
                        r.Ranking.BrokerId == apiUser.BrokerId);
                if (requestGroup == null)
                {
                    return ReturnError(ErrorCodes.OrderGroupNotFound);
                }
                //End of service
                return Json(GetResponseFromRequestGroup(requestGroup));
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

        [HttpGet]
        public async Task<JsonResult> File(string orderGroupNumber, int attachmentId, string callingUser)
        {
            _logger.LogInformation($"{callingUser} called {nameof(File)} to get the attachment {attachmentId} on order group {orderGroupNumber}");

            try
            {
                var apiUser = await GetApiUser();
                var orderGroup = _dbContext.OrderGroups
                    .Include(o => o.Attachments).ThenInclude(a => a.Attachment)
                    .SingleOrDefault(o => o.OrderGroupNumber == orderGroupNumber &&
                        //Must have a request connected to the order for the broker, any status...
                        o.RequestGroups.Any(r => r.Ranking.BrokerId == apiUser.BrokerId));
                if (orderGroup == null)
                {
                    return ReturnError(ErrorCodes.OrderGroupNotFound);
                }

                var attachment = orderGroup.Attachments.Where(a => a.AttachmentId == attachmentId).SingleOrDefault()?.Attachment;
                if (attachment == null)
                {
                    return ReturnError(ErrorCodes.AttachmentNotFound);
                }

                return Json(new FileResponse
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
        private JsonResult ReturnError(string errorCode, string specifiedErrorMessage = null)
        {
            //TODO: Add to log, information...
            var message = TolkApiOptions.ErrorResponses.Single(e => e.ErrorCode == errorCode).Copy();
            Response.StatusCode = message.StatusCode;
            if (!string.IsNullOrEmpty(specifiedErrorMessage))
            {
                message.ErrorMessage = specifiedErrorMessage;
            }
            return Json(message);
        }

        //Break out to a auth pipline
        private async Task<AspNetUser> GetApiUser()
        {
            Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-UserName", out var userName);
            Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-ApiKey", out var key);
            return await _apiUserService.GetApiUser(Request.HttpContext.Connection.ClientCertificate, userName, key);
        }

        private static RequestGroupDetailsResponse GetResponseFromRequestGroup(RequestGroup requestGroup)
        {
            return new RequestGroupDetailsResponse
            {
                Status = requestGroup.Status.GetCustomName(),
            };
        }

        #endregion
    }
}
