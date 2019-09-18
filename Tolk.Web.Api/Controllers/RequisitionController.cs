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

        public RequisitionController(
            TolkDbContext tolkDbContext,
            IOptions<TolkApiOptions> options,
            RequisitionService requisitionService,
            ApiUserService apiUserService
            )
        {
            _dbContext = tolkDbContext;
            _options = options.Value;
            _apiUserService = apiUserService;
            _requisitionService = requisitionService;
        }

        #region Updating Methods

        [HttpPost]
        public async Task<JsonResult> Create([FromBody] RequisitionModel model)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError(ErrorCodes.UNAUTHORIZED);
            }
            var order = _dbContext.Orders
                .Include(o => o.Requests)
                .Include(o => o.Requests).ThenInclude(r => r.Requisitions)
                .Include(o => o.Requests).ThenInclude(r => r.PriceRows)
                .Include(o => o.Requests).ThenInclude(r => r.Ranking)
                .Include(o => o.CreatedByUser)
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.CustomerUnit)
                .Include(o => o.ReplacingOrder)
                .SingleOrDefault(o => o.OrderNumber == model.OrderNumber &&
                   //Must have a request connected to the order for the broker, any status...
                   o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId));
            if (order == null)
            {
                return ReturError(ErrorCodes.ORDER_NOT_FOUND);
            }
            //Possibly the user should be added, if not found?? 
            var user = _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);

            var request = order.Requests.SingleOrDefault(r => apiUser.BrokerId == r.Ranking.BrokerId && r.Status == RequestStatus.Approved);
            if (request == null)
            {
                return ReturError(ErrorCodes.REQUEST_NOT_FOUND);
            }
            try
            {
                _requisitionService.Create(request, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null), model.Message,
                    model.Outlay, model.AcctualStartedAt, model.AcctualEndedAt, model.WasteTime, model.WasteTimeInconvenientHour, EnumHelper.GetEnumByCustomName<TaxCard>(model.TaxCard).Value,
                    new List<RequisitionAttachment>(), Guid.NewGuid(), model.MealBreaks.Select(m => new MealBreak
                    {
                        StartAt = m.StartedAt,
                        EndAt = m.EndedAt,
                    }).ToList(),
                    model.CarCompensation,
                    model.PerDiem);

                await _dbContext.SaveChangesAsync();
            }
            catch (InvalidOperationException)
            {
                //TODO: Should log the acctual exception here!!
                return ReturError(ErrorCodes.REQUISITION_NOT_IN_CORRECT_STATE);
            }
            //End of service
            return Json(new ResponseBase());
        }

        #endregion

        #region getting methods

        [HttpGet]
        public JsonResult View(RequisitionGetDetailsModel model)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError(ErrorCodes.UNAUTHORIZED);
            }

            if (!_dbContext.Orders
                .Any(o => o.OrderNumber == model.OrderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId)))
            {
                return ReturError(ErrorCodes.ORDER_NOT_FOUND);
            }

            var requisition = _dbContext.Requisitions
                .Include(r => r.Request).ThenInclude(r => r.Requisitions).ThenInclude(r => r.MealBreaks)
                .Include(r => r.Request).ThenInclude(r => r.Requisitions).ThenInclude(r => r.PriceRows)
                .Include(r => r.MealBreaks)
                .Include(r => r.PriceRows)
                .SingleOrDefault(c => c.Request.Order.OrderNumber == model.OrderNumber &&
                    c.Request.Ranking.BrokerId == apiUser.BrokerId &&
                    c.ReplacedByRequisitionId == null);
            if (requisition == null)
            {
                return ReturError(ErrorCodes.REQUISITION_NOT_FOUND);
            }
            //Possibly the user should be added, if not found?? 
            var user = _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
            //End of service
            return Json(GetResponseFromRequisition(requisition, model.OrderNumber, model.IncludePreviousRequisitions));
        }

        [HttpGet]
        public JsonResult File(string orderNumber, int attachmentId, string callingUser)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError(ErrorCodes.UNAUTHORIZED);
            }
            if (!_dbContext.Orders
                .Any(o => o.OrderNumber == orderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId)))
            {
                return ReturError(ErrorCodes.ORDER_NOT_FOUND);
            }

            var attachment = _dbContext.RequisitionAttachments.Where(a => a.AttachmentId == attachmentId &&
                a.Requisition.Request.Order.OrderNumber == orderNumber).SingleOrDefault()?.Attachment;
            if (attachment == null)
            {
                return ReturError(ErrorCodes.ATTACHMENT_NOT_FOUND);
            }

            return Json(new FileResponse
            {
                FileBase64 = Convert.ToBase64String(attachment.Blob)
            });
        }

        #endregion

        #region private methods

        //Break out to error generator service...
        private JsonResult ReturError(string errorCode)
        {
            //TODO: Add to log, information...
            var message =_options.ErrorResponses.Single(e => e.ErrorCode == errorCode);
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

        private static RequisitionDetailsResponse GetResponseFromRequisition(Requisition requisition, string orderNumber, bool includePreiviousRequisitions)
        {
            return new RequisitionDetailsResponse
            {
                OrderNumber = orderNumber,
                Status = requisition.Status.GetCustomName(),
                Message = requisition.Message,
                TaxCard = requisition.InterpretersTaxCard.GetValueOrDefault(TaxCard.TaxCardA).GetCustomName(),

                CarCompensation = requisition.CarCompensation,
                MealBreaks = requisition.MealBreaks.Select(m => new MealBreakModel
                {
                    StartedAt = m.StartAt,
                    EndedAt = m.EndAt
                }),
                Outlay = requisition.PriceRows.SingleOrDefault(p => p.PriceRowType == PriceRowType.Outlay)?.TotalPrice,
                PerDiem = requisition.PerDiem,
                WasteTime = requisition.TimeWasteNormalTime,
                WasteTimeInconvenientHour = requisition.TimeWasteIWHTime,
                PreviousRequisitions = includePreiviousRequisitions ? requisition.Request.Requisitions.Select(r => new RequisitionDetailsResponse
                {
                    OrderNumber = orderNumber,
                    Status = r.Status.GetCustomName(),
                    Message = r.Message,
                    TaxCard = r.InterpretersTaxCard.GetValueOrDefault(TaxCard.TaxCardA).GetCustomName(),
                    CarCompensation = r.CarCompensation,
                    MealBreaks = r.MealBreaks.Select(m => new MealBreakModel
                    {
                        StartedAt = m.StartAt,
                        EndedAt = m.EndAt
                    }),
                    Outlay = r.PriceRows.SingleOrDefault(p => p.PriceRowType == PriceRowType.Outlay).TotalPrice,
                    PerDiem = r.PerDiem,
                    WasteTime = r.TimeWasteNormalTime,
                    WasteTimeInconvenientHour = r.TimeWasteIWHTime,
                }) : Enumerable.Empty<RequisitionDetailsResponse>()
            };
        }

        #endregion
    }
}
