using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        private readonly ApiUserService _apiUserService;
        private readonly ITolkBaseOptions _tolkBaseOptions;
        private readonly ILogger _logger;

        public OrderController(
            TolkDbContext tolkDbContext,
            OrderService orderService,
            ApiOrderService apiOrderService,
            ApiUserService apiUserService,
            ITolkBaseOptions tolkBaseOptions,
            ILogger<OrderController> logger)
        {
            _dbContext = tolkDbContext;
            _orderService = orderService;
            _apiOrderService = apiOrderService;
            _tolkBaseOptions = tolkBaseOptions;
            _logger = logger;
            _apiUserService = apiUserService;
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
                    return Ok(new CreateOrderResponse
                    {
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

        [HttpPost]
        [ProducesResponseType(200, Type = typeof(ResponseBase))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [OpenApiTag("Order")]
        [Description("Anropas för att godkänna ett svar med restid")]
        [OpenApiIgnore]//Not applicable for broker api, hence hiding it from swagger
        public async Task<IActionResult> ApproveAnswer([FromBody] ApproveAnswerModel model)
        {
            var method = $"{nameof(OrderController)}.{nameof(ApproveAnswer)}";
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
            _logger.LogInformation($"{model.CallingUser} is approving request answer on {model.OrderNumber} from {model.BrokerIdentifier} ");
            if (ModelState.IsValid)
            {
                try
                {
                    AspNetUser apiUser = await _dbContext.Users.GetUserWithCustomerOrganisationById(User.UserId());
                    var request = await _apiOrderService.GetRequestFromOrderAndBrokerIdentifier(model.OrderNumber, model.BrokerIdentifier);
                    if (request == null || request.Order.CustomerOrganisationId != apiUser.CustomerOrganisationId )
                    {
                        return ReturnError(ErrorCodes.OrderNotFound, method);
                    }
                    if (!request.CanApprove)
                    {
                        return ReturnError(ErrorCodes.RequestNotInCorrectState, method);
                    }
                    var user = await _apiUserService.GetCustomerUser(model.CallingUser, apiUser.CustomerOrganisationId);
                    if (user == null)
                    {
                        return ReturnError(ErrorCodes.CallingUserMissing, method);
                    }

                    _orderService.ApproveRequestAnswer(request, user.Id, apiUser.Id);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation($"{request.RequestId} was approved");
                    return Ok(new ResponseBase());
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
        
        [HttpPost]
        [ProducesResponseType(200, Type = typeof(ResponseBase))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [OpenApiTag("Order")]
        [Description("Anropas för att avslå ett svar med restid")]
        [OpenApiIgnore]//Not applicable for broker api, hence hiding it from swagger
        public async Task<IActionResult> DenyAnswer([FromBody] DenyAnswerModel model)
        {
            var method = $"{nameof(OrderController)}.{nameof(DenyAnswer)}";
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
            _logger.LogInformation($"{model.CallingUser} is denying request answer on {model.OrderNumber} from {model.BrokerIdentifier} ");
            if (ModelState.IsValid)
            {
                AspNetUser apiUser = await _dbContext.Users.GetUserWithCustomerOrganisationById(User.UserId());
                var request = await _apiOrderService.GetRequestFromOrderAndBrokerIdentifier(model.OrderNumber, model.BrokerIdentifier);
                if (request == null && request.Order.CustomerOrganisationId != apiUser.CustomerOrganisationId)
                {
                    return ReturnError(ErrorCodes.OrderNotFound, method);
                }
                if (!request.CanApprove)
                {
                    return ReturnError(ErrorCodes.RequestNotInCorrectState, method);
                }
                var user = await _apiUserService.GetCustomerUser(model.CallingUser, apiUser.CustomerOrganisationId);
                if (user == null)
                {
                    return ReturnError(ErrorCodes.CallingUserMissing, method);
                }

                await _orderService.DenyRequestAnswer(request, user.Id, apiUser.Id, model.Message);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"{request.RequestId} was denied");
                return Ok(new ResponseBase());
            }
            return ReturnError(ErrorCodes.OrderNotValid, method);
        }

        [HttpPost]
        [ProducesResponseType(200, Type = typeof(ResponseBase))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [OpenApiTag("Order")]
        [Description("Anropas för att acceptera att ingen svarat på avropet")]
        [OpenApiIgnore]//Not applicable for broker api, hence hiding it from swagger
        public async Task<IActionResult> ConfirmNoAnswer([FromBody] ConfirmNoAnswerModel model)
        {
            var method = $"{nameof(OrderController)}.{nameof(ConfirmNoAnswer)}";
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
            _logger.LogInformation($"{model.CallingUser} is confirming that no-one accepted {model.OrderNumber}");
            if (ModelState.IsValid)
            {
                AspNetUser apiUser = await _dbContext.Users.GetUserWithCustomerOrganisationById(User.UserId());
                Order order = await _dbContext.Orders.GetOrderByOrderNumber(model.OrderNumber);
                if (order.CustomerOrganisationId != apiUser.CustomerOrganisationId)
                {
                    return ReturnError(ErrorCodes.OrderNotFound, method);
                }
                if (order.Status != OrderStatus.NoBrokerAcceptedOrder)
                {
                    return ReturnError(ErrorCodes.OrderNotInCorrectState, method);
                }
                var user = await _apiUserService.GetCustomerUser(model.CallingUser, apiUser.CustomerOrganisationId);
                if (user == null)
                {
                    return ReturnError(ErrorCodes.CallingUserMissing, method);
                }
                Order fullOrder = await _dbContext.Orders.GetFullOrderById(order.OrderId);

                await _orderService.ConfirmNoAnswer(fullOrder, user.Id, apiUser.Id);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"{order.OrderId} was confirmed that no-one accepted");
                return Ok(new ResponseBase());
            }
            return ReturnError(ErrorCodes.OrderNotValid, method);
        }

        [HttpPost]
        [ProducesResponseType(200, Type = typeof(ResponseBase))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [OpenApiTag("Order")]
        [Description("Anropas för att verifiera att men sett att förmedlingen avbokade avropet")]
        [OpenApiIgnore]//Not applicable for broker api, hence hiding it from swagger
        public async Task<IActionResult> ConfirmCancellation([FromBody] ConfirmCancellationModel model)
        {
            var method = $"{nameof(OrderController)}.{nameof(ConfirmCancellation)}";
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
            _logger.LogInformation($"{model.CallingUser} is confirming that broker cancelled {model.OrderNumber}");
            if (ModelState.IsValid)
            {
                AspNetUser apiUser = await _dbContext.Users.GetUserWithCustomerOrganisationById(User.UserId());
                Order order = await _dbContext.Orders.GetOrderByOrderNumber(model.OrderNumber);
                if (order.CustomerOrganisationId != apiUser.CustomerOrganisationId)
                {
                    return ReturnError(ErrorCodes.OrderNotFound, method);
                }
                if (order.Status != OrderStatus.CancelledByBroker)
                {
                    return ReturnError(ErrorCodes.OrderNotInCorrectState, method);
                }
                var user = await _apiUserService.GetCustomerUser(model.CallingUser, apiUser.CustomerOrganisationId);
                if (user == null)
                {
                    return ReturnError(ErrorCodes.CallingUserMissing, method);
                }
                var request = await _dbContext.Requests.GetLastRequestForOrder(order.OrderId);

                await _orderService.ConfirmCancellationByBroker(request, user.Id, apiUser.Id);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"{order.OrderId} was confirmed that broker cancelled");
                return Ok(new ResponseBase());
            }
            return ReturnError(ErrorCodes.OrderNotValid, method);
        }

        [HttpPost]
        [ProducesResponseType(200, Type = typeof(ResponseBase))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [OpenApiTag("Order")]
        [Description("Anropas för att acceptera att ingen svarat på avropet")]
        [OpenApiIgnore]//Not applicable for broker api, hence hiding it from swagger
        public async Task<IActionResult> Cancel([FromBody] OrderCancelModel model)
        {
            var method = $"{nameof(OrderController)}.{nameof(Cancel)}";
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
            _logger.LogInformation($"{model.CallingUser} is confirming that no-one accepted {model.OrderNumber}");
            if (ModelState.IsValid)
            {
                AspNetUser apiUser = await _dbContext.Users.GetUserWithCustomerOrganisationById(User.UserId());
                Order order = await _dbContext.Orders.GetOrderByOrderNumber(model.OrderNumber);
                if (order.CustomerOrganisationId != apiUser.CustomerOrganisationId)
                {
                    return ReturnError(ErrorCodes.OrderNotFound, method);
                }
                var user = await _apiUserService.GetCustomerUser(model.CallingUser, apiUser.CustomerOrganisationId);
                if (user == null)
                {
                    return ReturnError(ErrorCodes.CallingUserMissing, method);
                }
                Order fullOrder = await _dbContext.Orders.GetFullOrderById(order.OrderId);
//NOTE Not handling central order handlers correctly (this is not a public api, just used in internal testing)
                if (!fullOrder.IsAuthorizedAsCreator(GetUnitsForUser(user.Id), apiUser.CustomerOrganisationId, user.Id, false))
                {
                    return ReturnError(ErrorCodes.Unauthorized, method, "The user does not have the right to cancel this order");
                }
                try
                {
                    await _orderService.CancelOrder(fullOrder, user.Id, apiUser.Id, model.Message);
                    await _dbContext.SaveChangesAsync();
                }
                catch (InvalidOperationException ex)
                {
                    return ReturnError(ErrorCodes.OrderNotInCorrectState, ex.Message);
                }
                _logger.LogInformation($"{order.OrderId} was denied");
                return Ok(new ResponseBase());
            }
            return ReturnError(ErrorCodes.OrderNotValid, method);
        }

        #endregion

        #region getting methods

        #endregion

        #region private methods

        private IEnumerable<int> GetUnitsForUser(int userId)
            => _dbContext.CustomerUnitUsers.Where(cu => cu.UserId == userId).Select(u => u.CustomerUnitId);

        //Break out to error generator service...
        private IActionResult ReturnError(string errorCode, string failingMethod, string specifiedErrorMessage = null)
        {
            _logger.LogInformation($"{errorCode} was returned from {failingMethod}");
            var message = TolkApiOptions.CustomerApiErrorResponses.Union(TolkApiOptions.CommonErrorResponses).Single(e => e.ErrorCode == errorCode).Copy();
            if (!string.IsNullOrEmpty(specifiedErrorMessage))
            {
                message.ErrorMessage = specifiedErrorMessage;
            }
            return Ok(message);
        }

        #endregion
    }
}
