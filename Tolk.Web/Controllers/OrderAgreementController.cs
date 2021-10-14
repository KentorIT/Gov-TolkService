using AutoMapper;
using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Enums;
using Tolk.Web.Helpers;
using Tolk.Web.Models;
using Tolk.Web.Services;

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.SystemCentralLocalAdmin)]
    public class OrderAgreementController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly IAuthorizationService _authorizationService;
        private readonly OrderService _orderService;
        private readonly ISwedishClock _clock;
        private readonly ILogger _logger;
        private readonly TolkOptions _options;
        private readonly OrderAgreementService _orderAgreementService;
        private readonly CacheService _cacheService;
        public OrderAgreementController(
            TolkDbContext dbContext,
            IAuthorizationService authorizationService,
            OrderService orderService,
            ISwedishClock clock,
            ILogger<OrderAgreementController> logger,
            IOptions<TolkOptions> options,
            OrderAgreementService orderAgreementService,
            CacheService cacheService
            )
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _orderService = orderService;
            _clock = clock;
            _logger = logger;
            _options = options?.Value;
            _orderAgreementService = orderAgreementService;
            _cacheService = cacheService;
        }

        public IActionResult List()
        {
            if (!User.IsInRole(Roles.SystemAdministrator) && !_cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == User.TryGetCustomerOrganisationId() && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseOrderAgreements)))
            {
                return Forbid();
            }

            return View(new OrderAgreementListModel
            {
                FilterModel = new OrderAgreementFilterModel()
            });
        }

        [HttpGet]
        public async Task<IActionResult> View(int requestId, int index)
        {
            var orderAgreementPayload = await _dbContext.OrderAgreementPayloads.GetByRequestId(requestId, index);
            if (orderAgreementPayload != null &&
                _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == orderAgreementPayload.Request.Order.CustomerOrganisationId && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseOrderAgreements)) &&
                (await _authorizationService.AuthorizeAsync(User, orderAgreementPayload, Policies.View)).Succeeded)
            {
                return View(OrderAgreementModel.GetModelFromOrderAgreement(orderAgreementPayload));
            }
            return Forbid();
        }
        public async Task<IActionResult> CreateFromOrderNumber(string orderNumber)
        {
            var order = await _dbContext.Orders.GetOrderByOrderNumber(orderNumber);
            if (order != null)
            {
                return await Create(order.OrderId);
            }
            return Forbid();
        }

        [HttpGet]
        public async Task<IActionResult> Create(int orderId)    
        {
            var request = await _dbContext.Requests.GetRequestForOrderAgreementCreation(orderId);
            if (!_cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == request.Order.CustomerOrganisationId && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseOrderAgreements)))
            {
                return Forbid();
            }
            request.Requisitions = await _dbContext.Requisitions.GetRequisitionsForRequest(request.RequestId).ToListAsync();
            request.OrderAgreementPayloads= await _dbContext.OrderAgreementPayloads.GetOrderAgreementPayloadsForRequest(request.RequestId).ToListAsync();
            if (request != null && (await _authorizationService.AuthorizeAsync(User, request, Policies.CreateOrderAgreement)).Succeeded)
            {
                if (request.AllowOrderAgreementCreation())
                {
                    using var memoryStream = new MemoryStream();
                    using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
                    var requisitionId = (await _dbContext.Requisitions.GetRequisitionsForOrder(orderId).Where(r => r.Status == RequisitionStatus.Approved ||
                        r.Status == RequisitionStatus.AutomaticGeneratedFromCancelledOrder ||
                        r.Status == RequisitionStatus.Created ||
                        r.Status == RequisitionStatus.Reviewed).SingleOrDefaultAsync())?.RequisitionId;
                    var previousIndex = request.OrderAgreementPayloads.Max(p => p.Index as int?);
                    int index = 1;
                    index++;
                    if (requisitionId.HasValue)
                    {
                        await _orderAgreementService.CreateOrderAgreementFromRequisition(requisitionId.Value, writer, previousIndex);
                        index = previousIndex.HasValue ? previousIndex.Value + 1 : 1;
                    }
                    else
                    {
                        //Create it from request instead.
                        //Should return error messages in all faulty cases
                        return Forbid();
                    }
                    memoryStream.Position = 0;
                    byte[] byteArray = new byte[memoryStream.Length];
                    memoryStream.Read(byteArray, 0, (int)memoryStream.Length);
                    memoryStream.Close();
                    //Save it to db
                    //TODO: ought to be added to the request's list instead...

                    request.OrderAgreementPayloads.Add(new OrderAgreementPayload
                    {
                        CreatedBy = User.GetUserId(),
                        ImpersonatingCreatedBy = User.TryGetImpersonatorId(),
                        Payload = byteArray,
                        RequisitionId = requisitionId,
                        CreatedAt = _clock.SwedenNow,
                        Index = index
                    });
                    await _dbContext.SaveChangesAsync();
                    //return a identifier for the saved agreement, to be able to retrieve it.
                    return RedirectToAction(nameof(View), new { requestId = request.RequestId, index });
                }
            }
            else
            {
                //Handle non-allow
            }
            return Forbid();
        }

        [HttpGet]
        public async Task<ActionResult> GetPayload(int requestId, int index)
        {
            var orderAgreementPayload = await _dbContext.OrderAgreementPayloads.GetByRequestId(requestId, index);
            if (orderAgreementPayload != null && (await _authorizationService.AuthorizeAsync(User, orderAgreementPayload, Policies.View)).Succeeded)
            {
                return File(orderAgreementPayload.Payload, System.Net.Mime.MediaTypeNames.Application.Octet, "OrderAgreement.xml");
            }
            return Forbid();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ListOrderAgreements(IDataTablesRequest request)
        {
            var model = new OrderAgreementFilterModel();
            await TryUpdateModelAsync(model);
            model.IsAdmin = User.IsInRole(Roles.SystemAdministrator);

            if (!model.IsAdmin)
            {
                model.CustomerOrganisationId = User.TryGetCustomerOrganisationId();
            }

            var payloads = _dbContext.OrderAgreementPayloads.Select(e => e);
            return AjaxDataTableHelper.GetData(request, payloads.Count(), model.Apply(payloads), x => x.Select(e =>
                    new OrderAgreementListItemModel
                    {
                        RequestId = e.RequestId,
                        Index = e.Index,
                        CreatedAt = e.CreatedAt,
                        OrderNumber = e.Request.Order.OrderNumber
                    }));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public JsonResult ListColumnDefinition()
        {
            return Json(AjaxDataTableHelper.GetColumnDefinitions<OrderAgreementListItemModel>());
        }
    }
}
