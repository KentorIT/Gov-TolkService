using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Responses;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Api.Helpers;

namespace Tolk.Web.Api.Controllers
{
    public class RequestController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly TolkApiOptions _options;
        private readonly PriceCalculationService _priceCalculationService;

        public RequestController(TolkDbContext tolkDbContext, IOptions<TolkApiOptions> options, PriceCalculationService priceCalculationService)
        {
            _dbContext = tolkDbContext;
            _options = options.Value;
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
            var user = GetUser(model.CallingUser, apiUser.BrokerId.Value);
            var request = order.Requests.SingleOrDefault(r =>
                apiUser.BrokerId == r.Ranking.BrokerId &&
                //Possibly other statuses, but this code is only temporary. Should be coalesced with the controller code.
                (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received));
            if (request == null)
            {
                return ReturError("REQUEST_NOT_FOUND");
            }

            var interpreter = GetInterpreter(model.Interpreter);
            //Does not handle Tolk-Id
            if (interpreter == null)
            {
                //Possibly the interpreter should be added, if not found?? 
                return ReturError("INTERPRETER_NOT_FOUND");
            }
            var competenceLevel = EnumHelper.GetEnumByCustomName<CompetenceAndSpecialistLevel>(model.CompetenceLevel).Value;
            var now = GetTimeAsync().Result;
            //Add RequestService that does this, and additionally calls _notificationService
            //Add transaction here!!!
            if (request.Status == RequestStatus.Created)
            {
                request.Received(now, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null));
            }
            request.Accept(GetTimeAsync().Result, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null), interpreter,
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
            //Possibly the user should be added, if not found?? 
            var order = _dbContext.Orders
                .Include(o => o.Requests).ThenInclude(r => r.Ranking)
                .SingleOrDefault(o => o.OrderNumber == model.OrderNumber);
            if (order == null)
            {
                return ReturError("ORDER_NOT_FOUND");
            }
            var user = GetUser(model.CallingUser, apiUser.BrokerId.Value);
            var request = order.Requests.SingleOrDefault(r =>
                apiUser.BrokerId == r.Ranking.BrokerId &&
                (r.Status == RequestStatus.Created));
            if (request == null)
            {
                return ReturError("REQUEST_NOT_FOUND");
            }
            var now = GetTimeAsync().Result;
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
            X509Certificate2 clientCertInRequest = Request.HttpContext.Connection.ClientCertificate;
            if (Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-CallerSecret", out var type))
            {
                //Need a lot more security here
                return _dbContext.Users.SingleOrDefault(u => u.Claims.Any(c => c.ClaimType == "Secret" && c.ClaimValue == type));
            }
            else
            {
                return _dbContext.Users.SingleOrDefault(u => u.Claims.Any(c => c.ClaimType == "CertSerialNumber" && c.ClaimValue == clientCertInRequest.SerialNumber));
            }

        }

        //Break out to a Api User Service
        private AspNetUser GetUser(string caller, int? brokerId)
        {
            return !string.IsNullOrWhiteSpace(caller) ?
                _dbContext.Users.SingleOrDefault(u => u.NormalizedEmail == caller.ToUpper() && u.BrokerId == brokerId) :
                null;
        }

        //Break out to a Api User Service
        private Interpreter GetInterpreter(string interpreter)
        {
            return !string.IsNullOrWhiteSpace(interpreter) ?
                _dbContext.Users.Include(u => u.Interpreter).SingleOrDefault(u => u.NormalizedEmail == interpreter.ToUpper())?.Interpreter :
                null;
        }

        //Break out to a Time Service
        private async Task<DateTimeOffset> GetTimeAsync()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                //Also add cert to call
                var response = await client.GetAsync($"{_options.TolkWebBaseUrl}/Time/");
                return await response.Content.ReadAsAsync<DateTimeOffset>();
            }
        }

        //Break out, or fill cache at startup?
        // use this pattern: public const string UNAUTHORIZED = nameof(UNAUTHORIZED);
        private static IEnumerable<ErrorResponse> ErrorResponses
        {
            get
            {
#warning should move to cache!!
#warning should handle information from the call, i.e. Order number and the api method called
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
