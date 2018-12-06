using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Responses;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Api.Helpers;
using Tolk.Web.Api.Services;

namespace Tolk.Web.Api.Controllers
{
    public class RequestController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly TolkApiOptions _options;
        private readonly RequestService _requestService;
        private readonly ApiUserService _apiUserService;
        private readonly ISwedishClock _timeService;

        public RequestController(
            TolkDbContext tolkDbContext,
            IOptions<TolkApiOptions> options,
            RequestService requestService,
            ApiUserService apiUserService,
            ISwedishClock timeService)
        {
            _dbContext = tolkDbContext;
            _options = options.Value;
            _apiUserService = apiUserService;
            _timeService = timeService;
            _requestService = requestService;
        }

        #region Methods

        [HttpPost]
        public JsonResult Answer([FromBody] RequestAssignModel model)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }
            var order = _dbContext.Orders
                .Include(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(o => o.Requests).ThenInclude(r => r.RequirementAnswers)
                .Include(o => o.Requests).ThenInclude(r => r.PriceRows)
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.CreatedByUser)
                .Include(o => o.ContactPersonUser)
                .SingleOrDefault(o => o.OrderNumber == model.OrderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId));
            if (order == null)
            {
                return ReturError("ORDER_NOT_FOUND");
            }
            //Possibly the user should be added, if not found?? 
            var user = _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
            var request = order.Requests.SingleOrDefault(r =>
                apiUser.BrokerId == r.Ranking.BrokerId &&
                //Possibly other statuses, but this code is only temporary. Should be coalesced with the controller code.
                (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received));
            if (request == null)
            {
                return ReturError("REQUEST_NOT_FOUND");
            }

            var interpreter = _apiUserService.GetInterpreter(model.Interpreter);
            //Does not handle Tolk-Id
            if (interpreter == null)
            {
                //Possibly the interpreter should be added, if not found?? 
                return ReturError("INTERPRETER_NOT_FOUND");
            }
            var now = _timeService.SwedenNow;
            //Add transaction here!!!
            if (request.Status == RequestStatus.Created)
            {
                request.Received(now, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null));
            }
            _requestService.Accept(
                request,
                now,
                user?.Id ?? apiUser.Id,
                (user != null ? (int?)apiUser.Id : null),
                interpreter,
                EnumHelper.GetEnumByCustomName<InterpreterLocation>(model.Location).Value,
                EnumHelper.GetEnumByCustomName<CompetenceAndSpecialistLevel>(model.CompetenceLevel).Value,
                model.RequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                {
                    Answer = ra.Answer,
                    CanSatisfyRequirement = ra.CanMeetRequirement, 
                    OrderRequirementId = ra.RequirementId,
                }),
                //Does not handle attachments yet.
                new List<RequestAttachment>(),
                model.ExpectedTravelCosts
            );
            _dbContext.SaveChanges();
            //End of service
            return Json(new ResponseBase());
        }

        [HttpPost]
        public JsonResult Acknowledge([FromBody] RequestAcknowledgeModel model)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }
            var order = _dbContext.Orders
                .Include(o => o.Requests).ThenInclude(r => r.Ranking)
                .SingleOrDefault(o => o.OrderNumber == model.OrderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId));
            if (order == null)
            {
                return ReturError("ORDER_NOT_FOUND");
            }
            //Possibly the user should be added, if not found?? 
            var user = _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
            var request = order.Requests.SingleOrDefault(r =>
                apiUser.BrokerId == r.Ranking.BrokerId &&
                (r.Status == RequestStatus.Created));
            if (request == null)
            {
                return ReturError("REQUEST_NOT_FOUND");
            }
            //Add RequestService that does this, and additionally calls _notificationService
            request.Received(_timeService.SwedenNow, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null));
            _dbContext.SaveChanges();
            //End of service
            return Json(new ResponseBase());
        }

        [HttpPost]
        public async Task<JsonResult> Decline([FromBody] RequestDeclineModel model)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }
            var order = _dbContext.Orders
                .Include(o => o.Requests).ThenInclude(r => r.Ranking)
                .SingleOrDefault(o => o.OrderNumber == model.OrderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId));
            if (order == null)
            {
                return ReturError("ORDER_NOT_FOUND");
            }
            //Possibly the user should be added, if not found?? 
            var user = _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
            var request = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order.CreatedByUser)
                .Include(r => r.Order.ContactPersonUser)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder).ThenInclude(r => r.Requests)
                .Include(r => r.Interpreter).ThenInclude(i => i.User)
                .SingleOrDefault(r => r.Order.OrderNumber == model.OrderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    r.Ranking.BrokerId == apiUser.BrokerId &&
                    //Possibly other statuses, but this code is only temporary. Should be coalesced with the controller code.
                    (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received));
            if (request == null)
            {
                return ReturError("REQUEST_NOT_FOUND");
            }
            await _requestService.Decline(request, _timeService.SwedenNow, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null), model.Message);
            _dbContext.SaveChanges();
            //End of service
            return Json(new ResponseBase());
        }

        [HttpPost]
        public JsonResult Cancel([FromBody] RequestCancelModel model)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }
            if (!_dbContext.Orders
                .Any(o => o.OrderNumber == model.OrderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId)))
            {
                return ReturError("ORDER_NOT_FOUND");
            }
            //Possibly the user should be added, if not found?? 
            var user = _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
            var request = _dbContext.Requests
            .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
            .Include(r => r.Order.CreatedByUser)
            .Include(r => r.Order.ContactPersonUser)
            .Include(r => r.Interpreter).ThenInclude(i => i.User)
            .Include(r => r.Ranking).ThenInclude(r => r.Broker)
            .SingleOrDefault(r => r.Order.OrderNumber == model.OrderNumber &&
                //Must have a request connected to the order for the broker, any status...
                r.Ranking.BrokerId == apiUser.BrokerId &&
                //TODO: Possibly other statuses, but this code is only temporary. Should be coalesced with the controller code.
                (r.Status == RequestStatus.Approved));
            if (request == null)
            {
                return ReturError("REQUEST_NOT_FOUND");
            }
            try
            {
                _requestService.CancelByBroker(request, _timeService.SwedenNow, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null), model.Message);
                _dbContext.SaveChanges();

            }
            catch (InvalidOperationException)
            {
                //TODO: Should log the acctual exception here!!
                return ReturError("REQUEST_NOT_IN_CORRECT_STATE");
            }

            //End of service
            return Json(new ResponseBase());
        }

        [HttpGet]
        public JsonResult File(string orderNumber, int attachmentId)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }
            var order = _dbContext.Orders
                .Include(o => o.Requests).ThenInclude(r => r.Ranking)
                .Include(o => o.Attachments).ThenInclude(a => a.Attachment)
                .SingleOrDefault(o => o.OrderNumber == orderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId));
            if (order == null)
            {
                return ReturError("ORDER_NOT_FOUND");
            }

            var attachment = order.Attachments.Where(a => a.AttachmentId == attachmentId).SingleOrDefault()?.Attachment;
            if (attachment == null)
            {
                return ReturError("ATTACHMENT_NOT_FOUND");
            }

            return Json(new FileResponse
            {
                FileBase64 = Convert.ToBase64String(attachment.Blob)
            });
        }

        [HttpPost]
        public JsonResult ChangeInterpreter([FromBody] RequestAssignModel model)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }
            if (!_dbContext.Orders
                .Any(o => o.OrderNumber == model.OrderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId)))
            {
                return ReturError("ORDER_NOT_FOUND");
            }
            //Possibly the user should be added, if not found?? 
            var user = _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
            var request = _dbContext.Requests
            .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
            .Include(r => r.Order.CreatedByUser)
            .Include(r => r.Order.ContactPersonUser)
            .Include(r => r.Interpreter).ThenInclude(i => i.User)
            .Include(r => r.Ranking).ThenInclude(r => r.Broker)
            .SingleOrDefault(r => r.Order.OrderNumber == model.OrderNumber &&
                //Must have a request connected to the order for the broker, any status...
                r.Ranking.BrokerId == apiUser.BrokerId &&
                (r.Status == RequestStatus.Approved || 
                r.Status == RequestStatus.Created || 
                r.Status == RequestStatus.Received || 
                r.Status == RequestStatus.InterpreterReplaced || 
                r.Status == RequestStatus.Accepted));
            if (request == null)
            {
                return ReturError("REQUEST_NOT_FOUND");
            }
            var interpreter = _apiUserService.GetInterpreter(model.Interpreter);
            //Does not handle Tolk-Id
            if (interpreter == null)
            {
                //Possibly the interpreter should be added, if not found?? 
                return ReturError("INTERPRETER_NOT_FOUND");
            }
            var competenceLevel = EnumHelper.GetEnumByCustomName<CompetenceAndSpecialistLevel>(model.CompetenceLevel).Value;

            try
            {
                //TODO: ADD REQ ANSWERS HERE AND ON ANSWER.
                //requirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                //{
                //    RequestId = newRequest.RequestId,
                //    OrderRequirementId = ra.OrderRequirementId,
                //    Answer = ra.Answer,
                //    CanSatisfyRequirement = ra.CanSatisfyRequirement
                //}),

                //TODO: ADD FILES HERE AND ON ANSWER.

                _requestService.ChangeInterpreter(
                    request,
                    _timeService.SwedenNow,
                    user?.Id ?? apiUser.Id,
                    (user != null ? (int?)apiUser.Id : null),
                    interpreter,
                    EnumHelper.GetEnumByCustomName<InterpreterLocation>(model.Location).Value,
                    competenceLevel,
                    //Does not handle reqmts yet
                    new List<OrderRequirementRequestAnswer>(),
                    //Does not handle attachments yet.
                    new List<RequestAttachment>(),
                    model.ExpectedTravelCosts);
                _dbContext.SaveChanges();

            }
            catch (InvalidOperationException)
            {
                //TODO: Should log the acctual exception here!!
                return ReturError("REQUEST_NOT_IN_CORRECT_STATE");
            }
            return Json(new ResponseBase());
        }

        [HttpPost]
        public JsonResult AcceptReplacement([FromBody] ApiPayloadBaseModel model)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }
            return Json(new ResponseBase());
        }

        [HttpPost]
        public JsonResult DeclineReplacement([FromBody] RequestDeclineModel model)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }
            return Json(new ResponseBase());
        }

        #endregion

        #region private methods

        private JsonResult ReturError(string errorCode)
        {
            //TODO: Add to log, information...
            var message = ErrorResponses.Single(e => e.ErrorCode == errorCode);
            Response.StatusCode = message.StatusCode;
            return Json(message);
        }

        //Break out to a Api User Service
        private AspNetUser GetApiUser()
        {
            Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-CallerSecret", out var secret);
            return _apiUserService.GetApiUser(Request.HttpContext.Connection.ClientCertificate, secret);
        }

        //Break out, or fill cache at startup?
        // use this pattern: public const string UNAUTHORIZED = nameof(UNAUTHORIZED);
        private static IEnumerable<ErrorResponse> ErrorResponses
        {
            get
            {
                //TODO: should move to cache!!
                //TODO: should handle information from the call, i.e. Order number and the api method called
                return new List<ErrorResponse>
                {
                    new ErrorResponse { StatusCode = 403, ErrorCode = "UNAUTHORIZED", ErrorMessage = "The api user could not be authorized." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = "ORDER_NOT_FOUND", ErrorMessage = "The provided order number could not be found on a request connected to your organsation." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = "REQUEST_NOT_FOUND", ErrorMessage = "The provided order number has no request in the correct state for the call." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = "INTERPRETER_NOT_FOUND", ErrorMessage = "The provided interpreter was not found." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = "ATTACHMENT_NOT_FOUND", ErrorMessage = "The file coould not be found." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = "REQUEST_NOT_IN_CORRECT_STATE", ErrorMessage = "The request or the underlying order was not in a correct state." },
               };
            }
        }

        #endregion
    }
}
