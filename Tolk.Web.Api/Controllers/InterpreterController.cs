using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Enums;
using Tolk.Api.Payloads.Responses;
using Tolk.BusinessLogic.Data;
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
    public class InterpreterController : ControllerBase
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
        public async Task<IActionResult> Create([FromBody] InterpreterDetailsModel interpreter)
        {
            if (interpreter == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }

            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                if (EnumHelper.GetEnumByCustomName<InterpreterInformationType>(interpreter.InterpreterInformationType) != InterpreterInformationType.NewInterpreter)
                {
                    ReturnError(ErrorCodes.InterpreterFaultyIntention);
                }
                var createdInterpreter = _apiUserService.GetInterpreter(interpreter, brokerId);
                await _dbContext.SaveChangesAsync();
                var createdInterpreterResponse = ApiUserService.GetModelFromEntity(createdInterpreter);
                return Ok(new CreateInterpreterResponse { Interpreter = createdInterpreterResponse });
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
        public async Task<IActionResult> Update([FromBody] InterpreterDetailsModel interpreter)
        {
            if (interpreter == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }

            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                if (EnumHelper.GetEnumByCustomName<InterpreterInformationType>(interpreter.InterpreterInformationType) == InterpreterInformationType.NewInterpreter)
                {
                    ReturnError(ErrorCodes.InterpreterFaultyIntention);
                }
                var updatedInterpreter = _apiUserService.GetInterpreter(interpreter, brokerId);
                await _dbContext.SaveChangesAsync();
                var updatedInterpreterResponse = ApiUserService.GetModelFromEntity(updatedInterpreter);
                return Ok(new UpdateInterpreterResponse { Interpreter = updatedInterpreterResponse });
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
        public async Task<IActionResult> View(int? interpreterId, string officialInterpreterId, string callingUser)
        {
            if (!interpreterId.HasValue && string.IsNullOrWhiteSpace(officialInterpreterId))
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }

            _logger.LogInformation($"'{callingUser ?? "Unspecified user"}' called {nameof(View)} to view the interpreter with {(interpreterId.HasValue ? $"interpreterId: {interpreterId}" : $"officialInterpreterId: {officialInterpreterId}")}");
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var interpreter = interpreterId.HasValue ?
                    await _apiUserService.GetInterpreterModelFromId(interpreterId.Value, brokerId) :
                    await _apiUserService.GetInterpreterModelFromId(officialInterpreterId, brokerId);
                return Ok(new ViewInterpreterResponse { Interpreter = interpreter });
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
        private IActionResult ReturnError(string errorCode)
        {
            //TODO: Add to log, information...
            return Ok(TolkApiOptions.ErrorResponses.Single(e => e.ErrorCode == errorCode));
        }

        #endregion
    }
}
