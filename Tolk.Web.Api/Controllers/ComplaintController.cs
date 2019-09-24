using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
using Tolk.Web.Api.Exceptions;
using Tolk.Web.Api.Helpers;
using Tolk.Web.Api.Services;

namespace Tolk.Web.Api.Controllers
{
    public class ComplaintController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly TolkApiOptions _options;
        private readonly ComplaintService _complaintService;
        private readonly ApiUserService _apiUserService;
        private readonly ISwedishClock _timeService;
        private readonly ApiOrderService _apiOrderService;

        public ComplaintController(
            TolkDbContext tolkDbContext,
            IOptions<TolkApiOptions> options,
            ComplaintService complaintService,
            ApiUserService apiUserService,
            ISwedishClock timeService,
            ApiOrderService apiOrderService)
        {
            _dbContext = tolkDbContext;
            _options = options.Value;
            _apiUserService = apiUserService;
            _timeService = timeService;
            _complaintService = complaintService;
            _apiOrderService = apiOrderService;
        }

        #region Updating Methods

        [HttpPost]
        public async Task<JsonResult> Accept([FromBody] ComplaintAcceptModel model)
        {
            try
            {
                var apiUser = await GetApiUser();
                var order = await _apiOrderService.GetOrderAsync(model.OrderNumber, apiUser.BrokerId.Value);
                //Get User, if any...
                var user = _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
                var complaint = await GetCreatedComplaint(model.OrderNumber, apiUser.BrokerId.Value);
                _complaintService.Accept(complaint, user?.Id ?? apiUser.Id, user != null ? (int?)apiUser.Id : null);

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
        public async Task<JsonResult> Dispute([FromBody] ComplaintDisputeModel model)
        {
            try
            {
                var apiUser = await GetApiUser();
                var order = await _apiOrderService.GetOrderAsync(model.OrderNumber, apiUser.BrokerId.Value);
                //Get User, if any...
                var user = _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
                var complaint = await GetCreatedComplaint(model.OrderNumber, apiUser.BrokerId.Value);
                _complaintService.Dispute(complaint, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null), model.Message);

                await _dbContext.SaveChangesAsync();
                //End of service
                return Json(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        #endregion

        #region getting methods

        public async Task<JsonResult> View(string orderNumber, string callingUser)
        {
            try
            {
                var apiUser = await GetApiUser();
                var complaint = await _dbContext.Complaints
                    .SingleOrDefaultAsync(c => c.Request.Order.OrderNumber == orderNumber &&
                        c.Request.Ranking.BrokerId == apiUser.BrokerId &&
                        c.Request.Status == RequestStatus.Approved);
            if (complaint == null)
            {
                return ReturnError(ErrorCodes.COMPLAINT_NOT_FOUND);
            }
            //End of service
            return Json(GetResponseFromComplaint(complaint, orderNumber));
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        #endregion

        #region private methods

        private async Task<Complaint> GetCreatedComplaint(string orderNumber, int brokerId)
        {
            var complaint = await _dbContext.Complaints
                .Include(c => c.CreatedByUser)
                .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerUnit)
                .SingleOrDefaultAsync(c => c.Request.Order.OrderNumber == orderNumber &&
                   c.Request.Ranking.BrokerId == brokerId && c.Status == ComplaintStatus.Created);
            if (complaint == null)
            {
                throw new InvalidApiCallException(ErrorCodes.COMPLAINT_NOT_FOUND);
            }
            return complaint;

        }

        //Break out to error generator service...
        private JsonResult ReturnError(string errorCode)
        {
            //TODO: Add to log, information...
            var message = _options.ErrorResponses.Single(e => e.ErrorCode == errorCode);
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
