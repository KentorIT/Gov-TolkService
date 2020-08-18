using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSwag.Annotations;
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
    [Authorize(Policies.Broker)]
    public class ComplaintController : ControllerBase
    {
        private readonly TolkDbContext _dbContext;
        private readonly ComplaintService _complaintService;
        private readonly ApiUserService _apiUserService;
        private readonly ApiOrderService _apiOrderService;
        private readonly ILogger _logger;

        public ComplaintController(
            TolkDbContext tolkDbContext,
            ComplaintService complaintService,
            ApiUserService apiUserService,
            ApiOrderService apiOrderService,
            ILogger<ComplaintController> logger
)
        {
            _dbContext = tolkDbContext;
            _apiUserService = apiUserService;
            _complaintService = complaintService;
            _apiOrderService = apiOrderService;
            _logger = logger;
        }

        #region Updating Methods

        [HttpPost]
        [ProducesResponseType(200, Type = typeof(ResponseBase))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Anropas för att acceptera en inkommen reklamation")]
        [OpenApiTag("Complaint", AddToDocument = true, Description = "Grupp av metoder för att hantera reklamationer")]
        public async Task<IActionResult> Accept([FromBody] ComplaintAcceptModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing, nameof(Accept));
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();
                _ = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);

                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                var complaint = await GetCreatedComplaint(model.OrderNumber, brokerId);
                _complaintService.Accept(complaint, user?.Id ?? apiUserId, user != null ? (int?)apiUserId : null);

                await _dbContext.SaveChangesAsync();
                //End of service
                return Ok(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode, nameof(Accept));
            }
        }

        [HttpPost]
        [ProducesResponseType(200, Type = typeof(ResponseBase))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Anropas för att bestrida en inkommen reklamation")]
        [OpenApiTag("Complaint")]
        public async Task<IActionResult> Dispute([FromBody] ComplaintDisputeModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing, nameof(Dispute));
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();
                _ = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);

                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                var complaint = await GetCreatedComplaint(model.OrderNumber, brokerId);
                _complaintService.Dispute(complaint, user?.Id ?? apiUserId, (user != null ? (int?)apiUserId : null), model.Message);

                await _dbContext.SaveChangesAsync();
                return Ok(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode, nameof(Dispute));
            }
        }

        #endregion

        #region getting methods

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(ComplaintDetailsResponse))]
        [ProducesResponseType(403, Type = typeof(ErrorResponse))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [Description("Returnerar detaljerad information om aktiv reklamation kopplad till beställningen (orderNumber)")]
        [OpenApiTag("Complaint")]
        public async Task<IActionResult> View(string orderNumber, string callingUser)
        {
            _logger.LogInformation($"'{callingUser ?? "Unspecified user"}' called {nameof(View)} for the active complaint for the order {orderNumber}");
            var brokerId = User.TryGetBrokerId().Value;
            var complaint = await _dbContext.Complaints
                .SingleOrDefaultAsync(c => c.Request.Order.OrderNumber == orderNumber &&
                    c.Request.Ranking.BrokerId == brokerId &&
                    (c.Request.Status == RequestStatus.Approved || c.Request.Status == RequestStatus.Delivered));
            if (complaint == null)
            {
                return ReturnError(ErrorCodes.ComplaintNotFound, nameof(View));
            }
            //End of service
            return Ok(GetResponseFromComplaint(complaint, orderNumber));
        }

        #endregion

        #region private methods

        private async Task<Complaint> GetCreatedComplaint(string orderNumber, int brokerId)
        {
            var complaint = await _dbContext.Complaints.GetComplaintWithBrokerAndOrderNumber(orderNumber, brokerId);
            if (complaint == null)
            {
                throw new InvalidApiCallException(ErrorCodes.ComplaintNotFound);
            }
            if (complaint.Status != ComplaintStatus.Created)
            {
                throw new InvalidApiCallException(ErrorCodes.ComplaintNotFound);
            }
            return complaint;
        }

        //Break out to something more generic...
        private IActionResult ReturnError(string errorCode, string action, string specifiedErrorMessage = null)
        {
            _logger.LogInformation($"{action} failed with this error: {errorCode} {(!string.IsNullOrEmpty(specifiedErrorMessage) ? $"Specific error message: {specifiedErrorMessage}" : string.Empty)}");
            var message = TolkApiOptions.BrokerApiErrorResponses.Single(e => e.ErrorCode == errorCode).Copy();
            if (!string.IsNullOrEmpty(specifiedErrorMessage))
            {
                message.ErrorMessage = specifiedErrorMessage;
            }
            return Ok(message);
        }

        private static ComplaintDetailsResponse GetResponseFromComplaint(Complaint complaint, string orderNumber)
        {
            return new ComplaintDetailsResponse
            {
                OrderNumber = orderNumber,
                Status = complaint.Status.GetCustomName(),
                ComplaintType = complaint.ComplaintType.GetCustomName(),
                Message = complaint.ComplaintMessage,
                AnswerMessage = complaint.AnswerMessage,
                AnswerDisputedMessage = complaint.AnswerDisputedMessage,
                TerminationMessage = complaint.TerminationMessage,
                CreatedAt = complaint.CreatedAt,
                AnsweredAt = complaint.AnsweredAt,
                AnswerDisputedAt = complaint.AnswerDisputedAt,
                TerminatedAt = complaint.TerminatedAt
            };
        }

        #endregion
    }
}

