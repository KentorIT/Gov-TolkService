using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
using Tolk.Web.Helpers;
using Tolk.Web.Models;

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
                IsApplicationAdmin = User.IsInRole(Roles.ApplicationAdministrator),
                FilterModel = new OrderAgreementFilterModel { 
                    IsAdmin = User.IsInRole(Roles.SystemAdministrator),
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> View(int id)
        {
            var orderAgreementPayload = await _dbContext.OrderAgreementPayloads.GetById(id);
            if (orderAgreementPayload != null &&
                _cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == orderAgreementPayload.Request.Order.CustomerOrganisationId && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseOrderAgreements)) &&
                (await _authorizationService.AuthorizeAsync(User, orderAgreementPayload, Policies.View)).Succeeded)
            {
                return View(OrderAgreementModel.GetModelFromOrderAgreement(orderAgreementPayload));
            }
            return Forbid();
        }

        [Authorize(Roles = Roles.ApplicationAdministrator)]
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
        [Authorize(Roles = Roles.ApplicationAdministrator)]
        public async Task<IActionResult> Create(int orderId)
        {
            var request = await _dbContext.Requests.GetRequestForOrderAgreementCreation(orderId);
            if (!_cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == request.Order.CustomerOrganisationId && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseOrderAgreements)))
            {
                return Forbid();
            }
            request.Requisitions = await _dbContext.Requisitions.GetRequisitionsForRequest(request.RequestId).ToListAsync();
            request.OrderAgreementPayloads = await _dbContext.OrderAgreementPayloads.GetOrderAgreementPayloadsForRequest(request.RequestId).ToListAsync();
            if (request != null && (await _authorizationService.AuthorizeAsync(User, request, Policies.CreateOrderAgreement)).Succeeded)
            {
                if (request.AllowOrderAgreementCreation())
                {
                    var requisitionId = (await _dbContext.Requisitions.GetRequisitionsForOrder(orderId).Where(r => r.Status == RequisitionStatus.Approved ||
                        r.Status == RequisitionStatus.AutomaticGeneratedFromCancelledOrder ||
                        r.Status == RequisitionStatus.Created ||
                        r.Status == RequisitionStatus.Reviewed).SingleOrDefaultAsync())?.RequisitionId;
                    var previousIndex = request.OrderAgreementPayloads.Max(p => p.Index as int?);
                    int index = 1;
                    string payloadIdentifier = "";
                    using var memoryStream = new MemoryStream();
                    using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
                    if (requisitionId.HasValue)
                    {
                        payloadIdentifier = await _orderAgreementService.CreateOrderAgreementFromRequisition(requisitionId.Value, writer, previousIndex);
                        index = previousIndex.HasValue ? previousIndex.Value + 1 : 1;
                    }
                    else
                    {
                        payloadIdentifier = await _orderAgreementService.CreateOrderAgreementFromRequest(request.RequestId, writer);
                    }
                    memoryStream.Position = 0;
                    byte[] byteArray = new byte[memoryStream.Length];
                    memoryStream.Read(byteArray, 0, (int)memoryStream.Length);
                    memoryStream.Close();
                    //Save it to db
                    var payload = new OrderAgreementPayload
                    {
                        CreatedBy = User.GetUserId(),
                        ImpersonatingCreatedBy = User.TryGetImpersonatorId(),
                        Payload = byteArray,
                        RequisitionId = requisitionId,
                        CreatedAt = _clock.SwedenNow,
                        Index = index,
                        IdentificationNumber = payloadIdentifier
                    };
                    request.OrderAgreementPayloads.Add(payload);
                    if (previousIndex.HasValue)
                    {
                        var previous = request.OrderAgreementPayloads.Single(p => p.Index == previousIndex);
                        previous.ReplacedByPayload = payload;
                    }
                    await _dbContext.SaveChangesAsync();
                    //return a identifier for the saved agreement, to be able to retrieve it.
                    return RedirectToAction(nameof(View), new { id = payload.OrderAgreementPayloadId });
                }
            }
            else
            {
                //Handle non-allow
            }
            return Forbid();
        }

        [HttpGet]
        public async Task<ActionResult> GetPayload(int id)
        {
            var orderAgreementPayload = await _dbContext.OrderAgreementPayloads.GetById(id);
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
            int? customerOrganisationId = !model.IsAdmin ? User.TryGetCustomerOrganisationId() : null;

            var payloads = _dbContext.OrderAgreementPayloads.ListOrderAgreements(customerOrganisationId);
            return AjaxDataTableHelper.GetData(request, payloads.Count(), model.Apply(payloads), x => x.Select(e =>
                    new OrderAgreementListItemModel
                    {
                        OrderAgreementPayloadId = e.OrderAgreementPayloadId,
                        IdentificationNumber = e.IdentificationNumber,
                        Index = e.Index,
                        CreatedAt = e.CreatedAt,
                        OrderNumber = e.Request.Order.OrderNumber,
                        IsLatest = e.ReplacedById == null,
                        CreatedBy = e.CreatedBy != null ? e.CreatedByUser.NameFamily + ", " + e.CreatedByUser.NameFirst : "Systemet",
                        CustomerName = e.Request.Order.CustomerOrganisation.Name
                    }));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public JsonResult ListColumnDefinition()
        {
            var definition = AjaxDataTableHelper.GetColumnDefinitions<OrderAgreementListItemModel>().ToList();
            definition.Single(d => d.Name == nameof(OrderAgreementListItemModel.CustomerName)).Visible = User.IsInRole(Roles.SystemAdministrator);
            return Json(definition);
        }
    }
}
