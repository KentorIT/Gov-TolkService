using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        private readonly PriceCalculationService _priceCalculationService;
        private readonly ApiUserService _apiUserService;
        private readonly TimeService _timeService;

        public RequestController(
            TolkDbContext tolkDbContext, 
            IOptions<TolkApiOptions> options, 
            PriceCalculationService priceCalculationService, 
            ApiUserService apiUserService, 
            TimeService timeService)
        {
            _dbContext = tolkDbContext;
            _options = options.Value;
            _apiUserService = apiUserService;
            _timeService = timeService;
            // will probably be removed, and replaced by a RequestService (used by both this(api) and web)
            _priceCalculationService = priceCalculationService;
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
            //Possibly the user should be added, if not found?? 
            var order = _dbContext.Orders
                .Include(o => o.Requests).ThenInclude(r => r.Ranking)
                .Include(o => o.Requests).ThenInclude(r => r.RequirementAnswers)
                .Include(o => o.Requests).ThenInclude(r => r.PriceRows)
                .Include(o => o.CustomerOrganisation)
                .SingleOrDefault(o => o.OrderNumber == model.OrderNumber);
            if (order == null)
            {
                return ReturError("ORDER_NOT_FOUND");
            }
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
            var competenceLevel = EnumHelper.GetEnumByCustomName<CompetenceAndSpecialistLevel>(model.CompetenceLevel).Value;
            var now = _timeService.GetTimeAsync().Result;
            //Add RequestService that does this, and additionally calls _notificationService
            //Add transaction here!!!
            if (request.Status == RequestStatus.Created)
            {
                request.Received(now, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null));
            }
            request.Accept(_timeService.GetTimeAsync().Result, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null), interpreter,
                EnumHelper.GetEnumByCustomName<InterpreterLocation>(model.Location).Value,
                competenceLevel,
                //Does not handle reqmts yet
                new OrderRequirementRequestAnswer[] { },
                //Does not handle attachments yet.
                new List<RequestAttachment>(),
                //Does not handle price info yet, either...
                _priceCalculationService.GetPrices(request, competenceLevel, model.ExpectedTravelCosts)
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
                .SingleOrDefault(o => o.OrderNumber == model.OrderNumber);
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
            var now = _timeService.GetTimeAsync().Result;
            //Add RequestService that does this, and additionally calls _notificationService
            request.Received(now, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null));
            _dbContext.SaveChanges();
            //End of service
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
               };
            }
        }

        #endregion
    }
}
