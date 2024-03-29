﻿using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
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
    [Authorize(Policy = Policies.SystemOrApplicationOrCustomerCentralAdmin)]
    public class StandardBusinessDocumentController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly IAuthorizationService _authorizationService;
        private readonly TolkOptions _options;        
        private readonly StandardBusinessDocumentService _standardBusinessDocumentService;
        private readonly CacheService _cacheService;
        public StandardBusinessDocumentController(
            TolkDbContext dbContext,
            IAuthorizationService authorizationService,
            IOptions<TolkOptions> options,            
            CacheService cacheService,
            StandardBusinessDocumentService standardBusinessDocumentService
            )
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
            _options = options?.Value;            
            _cacheService = cacheService;
            _standardBusinessDocumentService = standardBusinessDocumentService;
        }

        public IActionResult List()
        {         
            if (!User.IsAppOrSysAdmin() && !_cacheService.CustomerSettings.Any(c => c.CustomerOrganisationId == User.TryGetCustomerOrganisationId() && c.UsedCustomerSettingTypes.Any(cs => cs == CustomerSettingType.UseOrderAgreements)))
            {
                return Forbid();
            }

            return View(new PeppolMessageListModel
            {
                FilterModel = new PeppolMessageFilterModel { 
                    IsAdmin = User.IsAppOrSysAdmin()
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
                return View(PeppolMessageModel.GetModelFromPeppolPayload(peppolPayload, _options.EnableMockInvoice));
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

        [HttpGet]
        public async Task<ActionResult> GetMockInvoiceForOrder(int id)
        {
            if (_options.EnableMockInvoice)
            {
                var request = await _dbContext.Requests.GetRequestForPeppolMessageCreation(id);
                var payload = await _standardBusinessDocumentService.CreateMockInvoiceFromRequest(request);
                if (payload != null && (await _authorizationService.AuthorizeAsync(User, payload, Policies.View)).Succeeded)
                {
                    return File(payload.Payload, System.Net.Mime.MediaTypeNames.Application.Octet, $"MOCKINVOICE-FOR-{request.Order.OrderNumber}.xml");
                }
            }
            return Forbid();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ListPeppolMessages(IDataTablesRequest request)
        {
            var model = new PeppolMessageFilterModel();
            await TryUpdateModelAsync(model);
            model.IsAdmin = User.IsAppOrSysAdmin();
            int? customerOrganisationId = !model.IsAdmin ? User.TryGetCustomerOrganisationId() : null;
            if(!model.IsAdmin && !customerOrganisationId.HasValue)
            {
                throw new InvalidOperationException($"{nameof(ListPeppolMessages)} User with ID: {User.GetUserId} is not linked to a customer organisation and is not a Sys- or  App Administrator");
            }
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
                        CustomerName = e.Request.Order.CustomerOrganisation.Name
                    }));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public JsonResult ListColumnDefinition()
        {
            var definition = AjaxDataTableHelper.GetColumnDefinitions<PeppolMessageListItemModel>().ToList();
            definition.Single(d => d.Name == nameof(PeppolMessageListItemModel.CustomerName)).Visible = User.IsAppOrSysAdmin();
            return Json(definition);
        }
    }
}
