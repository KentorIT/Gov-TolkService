using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSwag;
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
    [Authorize(Policies.Customer)]
    public class OrderController : ControllerBase
    {
        private readonly TolkDbContext _dbContext;
        private readonly OrderService _orderService;
        private readonly ApiOrderService _apiOrderService;
        private readonly ITolkBaseOptions _tolkBaseOptions;
        private readonly ILogger _logger;

        public OrderController(
            TolkDbContext tolkDbContext,
            OrderService orderService,
            ApiOrderService apiOrderService,
            ITolkBaseOptions tolkBaseOptions,
            ILogger<OrderController> logger)
        {
            _dbContext = tolkDbContext;
            _orderService = orderService;
            _apiOrderService = apiOrderService;
            _tolkBaseOptions = tolkBaseOptions;
            _logger = logger;
        }

        #region Updating Methods

        [HttpPost]
        [ProducesResponseType(200, Type = typeof(CreateOrderResponse))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [OpenApiTag("Order", AddToDocument = true, Description = "Grupp av metoder för att hantera avrop som myndighet")]
        [Description("Anropas för att skapa ett avrop")]
        [OpenApiIgnore]//Not applicable for broker api, hence hiding it from swagger
        public async Task<IActionResult> Create([FromBody] CreateOrderModel model)
        {
            var method = $"{nameof(OrderController)}.{nameof(Create)}";
            _logger.LogDebug($"{method} was called");
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing, method);
            }
            if (!_tolkBaseOptions.EnableCustomerApi)
            {
                _logger.LogWarning($"{model.CallingUser} called {method}, but CustomerApi is not enabled!");
                return BadRequest(new ValidationProblemDetails { Title = "CustomerApi is not enabled!" });
            }
            if (string.IsNullOrEmpty(model.CallingUser))
            {
                return ReturnError(ErrorCodes.CallingUserMissing, method);
            }
            _logger.LogInformation($"{model.CallingUser} is creating a new order");
            if (ModelState.IsValid)
            {
                try
                {
                    var order = await _apiOrderService.GetOrderFromModel(model, User.UserId(), User.TryGetCustomerId().Value);
                    await _orderService.Create(order, model.LatestAnswerBy);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation($"{order.OrderNumber} was created");
                    return Ok(new CreateOrderResponse {
                        OrderNumber = order.OrderNumber,
                        PriceInformation = order.PriceRows.GetPriceInformationModel(
                            order.PriceCalculatedFromCompetenceLevel.GetCustomName(),
                            (await _dbContext.Requests.GetActiveRequestByOrderId(order.OrderId)).Ranking.BrokerFee),

                    });
                }
                catch (InvalidOperationException ex)
                {
                    return ReturnError(ErrorCodes.OrderNotValid, method, ex.Message);
                }
                catch (ArgumentNullException ex)
                {
                    return ReturnError(ErrorCodes.OrderNotValid, method, ex.Message);
                } 
            }
            return ReturnError(ErrorCodes.OrderNotValid, method);
        }

        #endregion

        #region getting methods

        #endregion

        #region private methods

        //Break out to error generator service...
        private IActionResult ReturnError(string errorCode, string failingMethod, string specifiedErrorMessage = null)
        {
            _logger.LogInformation($"{errorCode} was returned from {failingMethod}");
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
