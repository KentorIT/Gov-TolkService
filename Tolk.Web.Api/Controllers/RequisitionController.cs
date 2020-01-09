using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    [Authorize(Policies.Broker)]
    public class RequisitionController : ControllerBase
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly RequisitionService _requisitionService;
        private readonly ApiUserService _apiUserService;
        private readonly ApiOrderService _apiOrderService;

        public RequisitionController(
            TolkDbContext tolkDbContext,
            ILogger<RequisitionController> logger,
            RequisitionService requisitionService,
            ApiUserService apiUserService,
            ApiOrderService apiOrderService
            )
        {
            _dbContext = tolkDbContext;
            _logger = logger;
            _apiUserService = apiUserService;
            _requisitionService = requisitionService;
            _apiOrderService = apiOrderService;
        }

        #region Updating Methods

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RequisitionModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var apiUserId = User.UserId();
                var order = await _dbContext.Orders
                    .Include(o => o.Requests)
                    .Include(o => o.Requests).ThenInclude(r => r.Requisitions)
                    .Include(o => o.Requests).ThenInclude(r => r.PriceRows)
                    .Include(o => o.Requests).ThenInclude(r => r.Ranking)
                    .Include(o => o.CreatedByUser)
                    .Include(o => o.CustomerOrganisation)
                    .Include(o => o.CustomerUnit)
                    .Include(o => o.ReplacingOrder)
                    .SingleOrDefaultAsync(o => o.OrderNumber == model.OrderNumber &&
                       //Must have a request connected to the order for the broker, any status...
                       o.Requests.Any(r => r.Ranking.BrokerId == brokerId));
                if (order == null)
                {
                    return ReturnError(ErrorCodes.OrderNotFound);
                }
                //Possibly the user should be added, if not found?? 
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);

                var request = order.Requests.SingleOrDefault(r => brokerId == r.Ranking.BrokerId && r.Status == RequestStatus.Approved);
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                try
                {
                    _requisitionService.Create(request, user?.Id ?? apiUserId, (user != null ? (int?)apiUserId : null), model.Message,
                        model.Outlay, model.AcctualStartedAt, model.AcctualEndedAt, model.WasteTime, model.WasteTimeInconvenientHour, EnumHelper.GetEnumByCustomName<TaxCardType>(model.TaxCard).Value,
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
                    return ReturnError(ErrorCodes.RequisitionNotInCorrectState);
                }
                //End of service
                return Ok(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        #endregion

        #region getting methods

        [HttpGet]
        public async Task<IActionResult> View(RequisitionGetDetailsModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var brokerId = User.TryGetBrokerId().Value;
                var order = await _apiOrderService.GetOrderAsync(model.OrderNumber, brokerId);

                var requisition = _dbContext.Requisitions
                    .Include(r => r.Request).ThenInclude(r => r.Ranking)
                    .Include(r => r.Request).ThenInclude(r => r.Requisitions).ThenInclude(r => r.MealBreaks)
                    .Include(r => r.Request).ThenInclude(r => r.Requisitions).ThenInclude(r => r.PriceRows)
                    .Include(r => r.MealBreaks)
                    .Include(r => r.PriceRows)
                    .SingleOrDefault(c => c.Request.Order.OrderNumber == model.OrderNumber &&
                        c.Request.Ranking.BrokerId == brokerId &&
                        c.ReplacedByRequisitionId == null);
                if (requisition == null)
                {
                    return ReturnError(ErrorCodes.RequisitionNotFound);
                }
                //Possibly the user should be added, if not found?? 
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, brokerId);
                //End of service
                return Ok(GetResponseFromRequisition(requisition, model.OrderNumber, model.IncludePreviousRequisitions));
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpGet]
        public async Task<IActionResult> File(string orderNumber, int attachmentId, string callingUser)
        {
            _logger.LogInformation($"{callingUser} called {nameof(File)} to get the attachment {attachmentId} connected to requisition on order {orderNumber}");
            try
            {
                var order = await _apiOrderService.GetOrderAsync(orderNumber, User.TryGetBrokerId().Value);

                var attachment = (await _dbContext.RequisitionAttachments.SingleOrDefaultAsync(a => a.AttachmentId == attachmentId &&
                    a.Requisition.Request.Order.OrderNumber == orderNumber))?.Attachment;
                if (attachment == null)
                {
                    return ReturnError(ErrorCodes.AttachmentNotFound);
                }

                return Ok(new FileResponse
                {
                    FileBase64 = Convert.ToBase64String(attachment.Blob)
                });
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        #endregion

        #region private methods

        //Break out to error generator service...
        private IActionResult ReturnError(string errorCode)
        {
            //TODO: Add to log, information...
            var message = TolkApiOptions.ErrorResponses.Single(e => e.ErrorCode == errorCode);
            return Ok(message);
        }

        private static RequisitionDetailsResponse GetResponseFromRequisition(Requisition requisition, string orderNumber, bool includePreiviousRequisitions)
        {
            return new RequisitionDetailsResponse
            {
                OrderNumber = orderNumber,
                Status = requisition.Status.GetCustomName(),
                Message = requisition.Message,
                TaxCard = requisition.InterpretersTaxCard.GetValueOrDefault(TaxCardType.TaxCardA).GetCustomName(),

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
                PriceInformation = requisition.PriceRows.GetPriceInformationModel(((CompetenceAndSpecialistLevel)requisition.Request.CompetenceLevel).GetCustomName(), requisition.Request.Ranking.BrokerFee),

                PreviousRequisitions = includePreiviousRequisitions ? requisition.Request.Requisitions.Select(r => new RequisitionDetailsResponse
                {
                    OrderNumber = orderNumber,
                    Status = r.Status.GetCustomName(),
                    Message = r.Message,
                    TaxCard = r.InterpretersTaxCard.GetValueOrDefault(TaxCardType.TaxCardA).GetCustomName(),
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
                    PriceInformation = r.PriceRows.GetPriceInformationModel(((CompetenceAndSpecialistLevel)requisition.Request.CompetenceLevel).GetCustomName(), requisition.Request.Ranking.BrokerFee),
                }) : Enumerable.Empty<RequisitionDetailsResponse>()
            };
        }

        #endregion
    }
}
