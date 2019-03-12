using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
using Tolk.Web.Api.Helpers;
using Tolk.Web.Api.Services;

namespace Tolk.Web.Api.Controllers
{
    public class RequisitionController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly TolkApiOptions _options;
        private readonly RequisitionService _requisitionService;
        private readonly ApiUserService _apiUserService;
        private readonly ISwedishClock _timeService;

        public RequisitionController(
            TolkDbContext tolkDbContext,
            IOptions<TolkApiOptions> options,
            RequisitionService requisitionService,
            ApiUserService apiUserService,
            ISwedishClock timeService)
        {
            _dbContext = tolkDbContext;
            _options = options.Value;
            _apiUserService = apiUserService;
            _timeService = timeService;
            _requisitionService = requisitionService;
        }

        #region Updating Methods

        [HttpPost]
        public async Task<JsonResult> Create([FromBody] RequisitionModel model)
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

            var request = order.Requests.SingleOrDefault(r => apiUser.BrokerId == r.Ranking.BrokerId && r.Status == RequestStatus.Approved);
            if (request == null)
            {
                return ReturError("REQUEST_NOT_FOUND");
            }
            _requisitionService.Create(request, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null), model.Message,
                null, false, model.AcctualStartedAt, model.AcctualEndedAt, model.WasteTime, model.WasteTimeInconvenientHour, EnumHelper.GetEnumByCustomName<TaxCard>(model.TaxCard),
                new List<RequisitionAttachment>(), Guid.NewGuid(), model.MealBreaks.Select(m => new MealBreak
                {
                    StartAt = m.StartedAt,
                    EndAt = m.EndedAt,
                }).ToList(),
                model.CarCompensation,
                model.PerDiem);

            await _dbContext.SaveChangesAsync();
            //End of service
            return Json(new ResponseBase());
        }

        #endregion

        #region getting methods

        [HttpGet]
        public JsonResult View(string orderNumber, bool includePreviousRequisitions, string callingUser)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }

            var requisition = _dbContext.Requisitions
                .Include(r => r.Request).ThenInclude(r => r.Requisitions)
                .Include(r => r.MealBreaks)
                .SingleOrDefault(c => c.Request.Order.OrderNumber == orderNumber &&
                    c.Request.Ranking.BrokerId == apiUser.BrokerId &&
                    c.Request.ReplacingRequestId == null &&
                    c.ReplacedByRequisitionId == null);
            if (requisition == null)
            {
                return ReturError("ORDER_NOT_FOUND");
            }
            //Possibly the user should be added, if not found?? 
            var user = _apiUserService.GetBrokerUser(callingUser, apiUser.BrokerId.Value);
            //End of service
            return Json(GetResponseFromRequisition(requisition, orderNumber, includePreviousRequisitions));
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
                    new ErrorResponse { StatusCode = 401, ErrorCode = "REQUISITION_NOT_FOUND", ErrorMessage = "The provided order has no registered requisition." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = "REQUISITION_NOT_IN_CORRECT_STATE", ErrorMessage = "The requisition was not in a correct state." },
               };
            }
        }

        private static RequisitionDetailsResponse GetResponseFromRequisition(Requisition requisition, string orderNumber, bool includePreiviousRequisitions)
        {
            return new RequisitionDetailsResponse
            {
                OrderNumber = orderNumber,
                Status = requisition.Status.GetCustomName(),
                Message = requisition.Message,
                TaxCard = requisition.InterpretersTaxCard.GetValueOrDefault(TaxCard.TaxCardA).GetCustomName(),
                PreviousRequisitions = includePreiviousRequisitions ? requisition.Request.Requisitions.Select(r => new RequisitionDetailsResponse
                {
                    OrderNumber = orderNumber,
                    Status = r.Status.GetCustomName(),
                    Message = r.Message,
                    TaxCard = r.InterpretersTaxCard.GetValueOrDefault(TaxCard.TaxCardA).GetCustomName(),
                }) : Enumerable.Empty<RequisitionDetailsResponse>()
            };
        }

        #endregion
    }
}
