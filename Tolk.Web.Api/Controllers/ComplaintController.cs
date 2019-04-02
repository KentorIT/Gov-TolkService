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

        public ComplaintController(
            TolkDbContext tolkDbContext,
            IOptions<TolkApiOptions> options,
            ComplaintService complaintService,
            ApiUserService apiUserService,
            ISwedishClock timeService)
        {
            _dbContext = tolkDbContext;
            _options = options.Value;
            _apiUserService = apiUserService;
            _timeService = timeService;
            _complaintService = complaintService;
        }

        #region Updating Methods

        [HttpPost]
        public async Task<JsonResult> Accept([FromBody] ComplaintAcceptModel model)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }
            var order = _dbContext.Orders
                .Include(o => o.Requests).ThenInclude(r => r.Complaints)
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

            var complaint = _dbContext.Complaints
                .Include(c => c.CreatedByUser)
                .Include(c => c.Request).ThenInclude(r => r.Order)
                .Where(c => c.Request.Order.OrderNumber == model.OrderNumber &&
                   c.Request.Ranking.BrokerId == apiUser.BrokerId).ToList()
                .SingleOrDefault(c => c.Status == ComplaintStatus.Created);
            if (complaint == null)
            {
                return ReturError("COMPLAINT_NOT_FOUND");
            }
            _complaintService.Accept(complaint, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null));

            await _dbContext.SaveChangesAsync();
            //End of service
            return Json(new ResponseBase());
        }

        [HttpPost]
        public async Task<JsonResult> Dispute([FromBody] ComplaintDisputeModel model)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }
            var order = _dbContext.Orders.SingleOrDefault(o => o.OrderNumber == model.OrderNumber &&
                   //Must have a request connected to the order for the broker, any status...
                   o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId));
            if (order == null)
            {
                return ReturError("ORDER_NOT_FOUND");
            }
            //Possibly the user should be added, if not found?? 
            var user = _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);

            var complaint = _dbContext.Complaints
                .Include(c => c.CreatedByUser)
                .Include(c => c.Request).ThenInclude(r => r.Order)
                .Where(c => c.Request.Order.OrderNumber == model.OrderNumber &&
                   c.Request.Ranking.BrokerId == apiUser.BrokerId).ToList()
                .SingleOrDefault(c => c.Status == ComplaintStatus.Created);
            if (complaint == null)
            {
                return ReturError("COMPLAINT_NOT_FOUND");
            }
            _complaintService.Dispute(complaint, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null), model.Message);

            await _dbContext.SaveChangesAsync();
            //End of service
            return Json(new ResponseBase());
        }

        #endregion

        #region getting methods

        public JsonResult View(string orderNumber, string callingUser)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }

            var complaint = _dbContext.Complaints
                .SingleOrDefault(c => c.Request.Order.OrderNumber == orderNumber &&
                    c.Request.Ranking.BrokerId == apiUser.BrokerId &&
                    c.Request.ReplacingRequestId == null);
            if (complaint == null)
            {
                return ReturError("ORDER_NOT_FOUND");
            }
            //Possibly the user should be added, if not found?? 
            var user = _apiUserService.GetBrokerUser(callingUser, apiUser.BrokerId.Value);
            //End of service
            return Json(GetResponseFromComplaint(complaint, orderNumber));
        }

        #endregion

        #region private methods

        //Break out to error generator service...
        private JsonResult ReturError(string errorCode)
        {
            //TODO: Add to log, information...
            var message = ErrorResponses.Single(e => e.ErrorCode == errorCode);
            Response.StatusCode = message.StatusCode;
            return Json(message);
        }

        //Break out to a auth pipline
        private AspNetUser GetApiUser()
        {
            Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-UserName", out var userName);
            Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-ApiKey", out var key);
            return _apiUserService.GetApiUserByCertificate(Request.HttpContext.Connection.ClientCertificate) ??
                _apiUserService.GetApiUserByApiKey(userName, key);
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
                    new ErrorResponse { StatusCode = 401, ErrorCode = "COMPLAINT_NOT_FOUND", ErrorMessage = "The provided order has no registered complaint." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = "COMPLAINT_NOT_IN_CORRECT_STATE", ErrorMessage = "The complaint was not in a correct state." },
               };
            }
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
