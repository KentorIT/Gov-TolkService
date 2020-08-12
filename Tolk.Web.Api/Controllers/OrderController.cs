using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
using Tolk.Web.Api.Authorization;
using Tolk.Web.Api.Exceptions;
using Tolk.Web.Api.Helpers;
using Tolk.Web.Api.Services;

namespace Tolk.Web.Api.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    [Authorize(Policies.Customer)]
    public class OrderController : ControllerBase
    {
        private readonly TolkDbContext _dbContext;
        private readonly RequestService _requestService;
        private readonly ApiUserService _apiUserService;
        private readonly ISwedishClock _timeService;
        private readonly OrderService _orderService;
        private readonly ApiOrderService _apiOrderService;
        private readonly ILogger _logger;

        public OrderController(
            TolkDbContext tolkDbContext,
            RequestService requestService,
            ApiUserService apiUserService,
            ISwedishClock timeService,
            OrderService orderService,
            ApiOrderService apiOrderService,
            ILogger<OrderController> logger)
        {
            _dbContext = tolkDbContext;
            _apiUserService = apiUserService;
            _timeService = timeService;
            _requestService = requestService;
            _orderService = orderService;
            _apiOrderService = apiOrderService;
            _logger = logger;
        }

        #region Updating Methods

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
#warning Add a lot more validations
            //Start and end date checks?
            //More?
            //Must there be an lastestanswerby set?
            //Move as many as possible to service, to get the checks in both cases...
                try
                {
                    var order = await _apiOrderService.GetOrderFromModel(model, User.UserId(), User.TryGetCustomerId().Value);
                    await _orderService.Create(order, model.LatestAnswerBy);
                    await _dbContext.SaveChangesAsync();
                    return Ok(new CreateOrderResponse { 
                        OrderNumber = order.OrderNumber,
#warning Return the calculated price model too...
                        //PriceInformation = _apiOrderService.get
                    });
                }
                catch (InvalidOperationException ex)
                {
#warning Add new error codes
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, ex.Message);
                }
                catch (ArgumentNullException ex)
                {
#warning Add a lot more validations
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, ex.Message);
                }
        }

        #endregion

        #region getting methods

        #endregion

        #region private methods

        //Break out to error generator service...
        private IActionResult ReturnError(string errorCode, string specifiedErrorMessage = null)
        {
            //TODO: Add to log, information...
            var message = TolkApiOptions.BrokerApiErrorResponses.Single(e => e.ErrorCode == errorCode).Copy();
            if (!string.IsNullOrEmpty(specifiedErrorMessage))
            {
                message.ErrorMessage = specifiedErrorMessage;
            }
            return Ok(message);
        }

        #endregion
    }
}
