using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Enums;
using Tolk.Api.Payloads.Responses;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Api.Exceptions;
using Tolk.Web.Api.Helpers;
using Tolk.Web.Api.Services;

namespace Tolk.Web.Api.Controllers
{
    public class InterpreterController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ApiUserService _apiUserService;
        private readonly ILogger _logger;

        public InterpreterController(TolkDbContext tolkDbContext, ApiUserService apiUserService, ILogger<InterpreterController> logger)
        {
            _dbContext = tolkDbContext;
            _apiUserService = apiUserService;
            _logger = logger;
        }

        #region Updating Methods

        [HttpPost]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This is a public api, do not return 500")]
        public async Task<JsonResult> Create([FromBody] InterpreterDetailsModel interpreter)
        {
            if (interpreter == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }

            try
            {
                var apiUser = await GetApiUser();
                if (EnumHelper.GetEnumByCustomName<InterpreterInformationType>(interpreter.InterpreterInformationType) != InterpreterInformationType.NewInterpreter)
                {
                    ReturnError(ErrorCodes.InterpreterFaultyIntention);
                }
                var createdInterpreter = _apiUserService.GetInterpreter(interpreter, apiUser.BrokerId.Value);
                await _dbContext.SaveChangesAsync();
                var createdInterpreterResponse = ApiUserService.GetModelFromEntity(createdInterpreter);
                return Json(new CreateInterpreterResponse { Interpreter = createdInterpreterResponse });
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

        [HttpPost]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This is a public api, do not return 500")]
        public async Task<JsonResult> Update([FromBody] InterpreterDetailsModel interpreter)
        {
            if (interpreter == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }

            try
            {
                var apiUser = await GetApiUser();
                if (EnumHelper.GetEnumByCustomName<InterpreterInformationType>(interpreter.InterpreterInformationType) == InterpreterInformationType.NewInterpreter)
                {
                    ReturnError(ErrorCodes.InterpreterFaultyIntention);
                }
                var updatedInterpreter = _apiUserService.GetInterpreter(interpreter, apiUser.BrokerId.Value);
                await _dbContext.SaveChangesAsync();
                var updatedInterpreterResponse = ApiUserService.GetModelFromEntity(updatedInterpreter);
                return Json(new UpdateInterpreterResponse { Interpreter = updatedInterpreterResponse });
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

        #region getting methods

        [HttpGet]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This is a public api, do not return 500")]
        public async Task<JsonResult> View(int? interpreterId, string officialInterpreterId, string callingUser)
        {
            if (!interpreterId.HasValue && string.IsNullOrWhiteSpace(officialInterpreterId))
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }

            _logger.LogInformation($"'{callingUser ?? "Unspecified user"}' called {nameof(View)} to view the interpreter with {(interpreterId.HasValue ? $"interpreterId: {interpreterId}" : $"officialInterpreterId: {officialInterpreterId}")}");
            try
            {
                var apiUser = await GetApiUser();
                var interpreter = interpreterId.HasValue ?
                    await _apiUserService.GetInterpreterModelFromId(interpreterId.Value, apiUser.BrokerId.Value) :
                    await _apiUserService.GetInterpreterModelFromId(officialInterpreterId, apiUser.BrokerId.Value);
                return Json(new ViewInterpreterResponse { Interpreter = interpreter });
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

        #region SAME AS IN REQUEST, SHOULD BE MOVED

        //Break out to error generator service...
        private JsonResult ReturnError(string errorCode)
        {
            //TODO: Add to log, information...
            var message = TolkApiOptions.ErrorResponses.Single(e => e.ErrorCode == errorCode);
            Response.StatusCode = message.StatusCode;
            return Json(message);
        }

        //Break out to a auth pipline
        private async Task<AspNetUser> GetApiUser()
        {
            Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-UserName", out var userName);
            Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-ApiKey", out var key);
            return await _apiUserService.GetApiUser(Request.HttpContext.Connection.ClientCertificate, userName, key);
        }

        #endregion
    }
}
