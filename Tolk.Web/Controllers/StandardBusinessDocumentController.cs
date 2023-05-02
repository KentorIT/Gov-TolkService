using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
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
    public class StandardBusinessDocumentController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly IAuthorizationService _authorizationService;
        private readonly OrderService _orderService;
        private readonly ISwedishClock _clock;
        private readonly ILogger _logger;
        private readonly TolkOptions _options;        
        private readonly StandardBusinessDocumentService _standardBusinessDocumentService;
        private readonly CacheService _cacheService;
        public StandardBusinessDocumentController(
            TolkDbContext dbContext,
            IAuthorizationService authorizationService,
            OrderService orderService,
            ISwedishClock clock,
            ILogger<StandardBusinessDocumentController> logger,
            IOptions<TolkOptions> options,            
            CacheService cacheService,
            StandardBusinessDocumentService standardBusinessDocumentService
            )
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _orderService = orderService;
            _clock = clock;
            _logger = logger;
            _options = options?.Value;            
            _cacheService = cacheService;
            _standardBusinessDocumentService = standardBusinessDocumentService;
        }

        public IActionResult List()
        {
            if (!User.IsInRole(Roles.SystemAdministrator) && !_cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == User.TryGetCustomerOrganisationId() && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseOrderAgreements)))
            {
                return Forbid();
            }

            return View(new PeppolMessageListModel
            {
                IsApplicationAdmin = User.IsInRole(Roles.ApplicationAdministrator),
                FilterModel = new PeppolMessageFilterModel { 
                    IsAdmin = User.IsInRole(Roles.SystemAdministrator),
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> View(int id)
        {
            var peppolPayload = await _dbContext.PeppolPayloads.GetById(id);
            if (peppolPayload != null &&
                (await _authorizationService.AuthorizeAsync(User, peppolPayload, Policies.View)).Succeeded)
            {
                return View(PeppolMessageModel.GetModelFromPeppolPayload(peppolPayload));
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
            
            if (request != null &&
                (await _authorizationService.AuthorizeAsync(User, request, Policies.CreateOrderAgreement)).Succeeded &&
                request.AllowOrderAgreementCreation())
            {                                                                
                    var payload = await _standardBusinessDocumentService.CreateAndStoreStandardDocument(request.RequestId);
                    await _dbContext.SaveChangesAsync();

                    //return a identifier for the saved agreement, to be able to retrieve it.
                    return RedirectToAction(nameof(View), new { id = payload.PeppolPayloadId });                
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
            var payload = await _dbContext.PeppolPayloads.GetById(id);
            if (payload != null && (await _authorizationService.AuthorizeAsync(User, payload, Policies.View)).Succeeded)
            {
                return File(payload.Payload, System.Net.Mime.MediaTypeNames.Application.Octet, $"{payload.PeppolMessageType}-{payload.IdentificationNumber}.xml");
            }
            return Forbid();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ListPeppolMessages(IDataTablesRequest request)
        {
            var model = new PeppolMessageFilterModel();
            await TryUpdateModelAsync(model);
            model.IsAdmin = User.IsInRole(Roles.SystemAdministrator);
            int? customerOrganisationId = !model.IsAdmin ? User.TryGetCustomerOrganisationId() : null;

            var payloads = _dbContext.PeppolPayloads.ListOrderAgreements(customerOrganisationId);
            return AjaxDataTableHelper.GetData(request, payloads.Count(), model.Apply(payloads), x => x.Select(e =>
                    new PeppolMessageListItemModel
                    {
                        PeppolPayloadId = e.PeppolPayloadId,
                        IdentificationNumber = e.IdentificationNumber,                        
                        CreatedAt = e.CreatedAt,
                        OrderNumber = e.Request.Order.OrderNumber,
                        PeppolMessageType = e.PeppolMessageType.ToString(),
                        IsLatest = e.ReplacedById == null,
                        CreatedBy = e.CreatedBy != null ? e.CreatedByUser.NameFamily + ", " + e.CreatedByUser.NameFirst : "Systemet",
                        CustomerName = e.Request.Order.CustomerOrganisation.Name
                    }));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public JsonResult ListColumnDefinition()
        {
            var definition = AjaxDataTableHelper.GetColumnDefinitions<PeppolMessageListItemModel>().ToList();
            definition.Single(d => d.Name == nameof(PeppolMessageListItemModel.CustomerName)).Visible = User.IsInRole(Roles.SystemAdministrator);
            return Json(definition);
        }
    }
}
