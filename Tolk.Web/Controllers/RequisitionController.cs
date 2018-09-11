using Microsoft.AspNetCore.Mvc;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Services;
using Tolk.Web.Helpers;
using Tolk.Web.Authorization;
using Tolk.BusinessLogic.Services;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Utilities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Tolk.Web.Controllers
{
    public class RequisitionController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly OrderService _orderService;
        private readonly IAuthorizationService _authorizationService;
        private readonly PriceCalculationService _priceCalculationService;
        private readonly ILogger _logger;

        public RequisitionController(
            TolkDbContext dbContext,
            PriceCalculationService priceCalculationService,
            ISwedishClock clock,
            OrderService orderService,
            IAuthorizationService authorizationService,
            ILogger<RequisitionController> logger
            )
        {
            _dbContext = dbContext;
            _priceCalculationService = priceCalculationService;
            _clock = clock;
            _orderService = orderService;
            _authorizationService = authorizationService;
            _logger = logger;
        }

        public async Task<IActionResult> View(int id)
        {
            var requisition = _dbContext.Requisitions
                .Include(r => r.CreatedByUser)
                .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(r => r.Request).ThenInclude(r => r.Requisitions)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CompetenceRequirements)
                .Include(r => r.Request).ThenInclude(r => r.Interpreter).ThenInclude(i => i.User)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Request).ThenInclude(r => r.PriceRows).ThenInclude(r => r.PriceListRow)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Region)
              .Single(o => o.RequisitionId == id);
            if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.View)).Succeeded)
            {
                var competenceLevel = EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>((CompetenceAndSpecialistLevel)requisition.Request.CompetenceLevel.Value);
                var request = requisition.Request;
                var order = request.Order;
                var listType = order.CustomerOrganisation.PriceListType;
                var model = RequisitionViewModel.GetViewModelFromRequisition(requisition);
                var customerId = User.TryGetCustomerOrganisationId();
                model.AllowCreation = !customerId.HasValue && requisition.Request.Requisitions.All(r => r.Status == RequisitionStatus.DeniedByCustomer);
                model.ResultPriceInformationModel = GetRequisitionPriceInformation(requisition);
                model.RequestPriceInformationModel = GetRequisitionPriceInformation(requisition.Request);
                return View(model);
            }
            return Forbid();
        }

        public async Task<IActionResult> Process(int id)
        {
            var requisition = _dbContext.Requisitions
                .Include(r => r.CreatedByUser)
                .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(r => r.Request).ThenInclude(r => r.Requisitions)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CompetenceRequirements)
                .Include(r => r.Request).ThenInclude(r => r.Interpreter).ThenInclude(i => i.User)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Region)
              .Single(o => o.RequisitionId == id);
            if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.Accept)).Succeeded)
            {
                var competenceLevel = EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>((CompetenceAndSpecialistLevel)requisition.Request.CompetenceLevel.Value);
                var request = requisition.Request;
                var order = request.Order;
                var listType = order.CustomerOrganisation.PriceListType;
                var model = RequisitionProcessModel.GetProcessViewModelFromRequisition(requisition);
                model.ResultPriceInformationModel = GetRequisitionPriceInformation(requisition);
                return View(model);
            }
            return Forbid();
        }

        /// <summary>
        /// Create a requisition
        /// </summary>
        /// <param name="id">The Request to connect the requisition to</param>
        /// <returns></returns>
        public async Task<IActionResult> Create(int id)
        {
            var request = _dbContext.Requests
                .Include(r => r.Requisitions)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Interpreter).ThenInclude(i => i.User)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Ranking).ThenInclude(r => r.Region)
                .Single(o => o.RequestId == id);

            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.CreateRequisition)).Succeeded)
            {
                //Get request model from db
                return View(RequisitionViewModel.GetModelFromRequest(request));
            }
            return Forbid();
        }

        public IActionResult List(RequisitionFilterModel model)
        {
            var requisitions = _dbContext.Requisitions
                .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                .Where(r => !r.ReplacedByRequisitionId.HasValue);
            // The list of Requests should differ, if the user is an interpreter, or is a broker-user.
            var customerId = User.TryGetCustomerOrganisationId();
            var interpreterId = User.TryGetInterpreterId();
            var brokerId = User.TryGetBrokerId();
            bool isCustomer = false;
            if (customerId.HasValue)
            {
                if (!model.FilterByContact.HasValue)
                {
                    requisitions = requisitions.Where(r => r.Request.Order.CreatedBy == User.GetUserId() ||
                        r.Request.Order.ContactPersonId == User.GetUserId());
                }
                else if (model.FilterByContact.Value)
                {
                    requisitions = requisitions.Where(r => r.Request.Order.ContactPersonId == User.GetUserId());
                }
                else
                {
                    requisitions = requisitions.Where(r => r.Request.Order.CreatedBy == User.GetUserId());
                }
                isCustomer = true;
            }
            else if (brokerId.HasValue)
            {
                requisitions = requisitions.Where(r => r.Request.Ranking.BrokerId == brokerId);
            }
            else if (interpreterId.HasValue)
            {
                requisitions = requisitions.Where(r => r.Request.InterpreterId == interpreterId);
            }
            else
            {
                return Forbid();
            }

            // Filters
            if (model != null)
            {
                requisitions = model.Apply(requisitions);
            }

            model.IsCustomer = isCustomer;

            return View(
                new RequisitionListModel
                {
                    FilterModel = model,
                    Items = requisitions.Select(r => new RequisitionListItemModel
                    {
                        RequisitionId = r.RequisitionId,
                        Language = r.Request.Order.OtherLanguage ?? r.Request.Order.Language.Name ?? "(Tolkanvändarutbildning)",
                        OrderNumber = r.Request.Order.OrderNumber.ToString(),
                        Start = r.Request.Order.StartAt,
                        End = r.Request.Order.EndAt,
                        Status = r.Status,
                        Action = isCustomer && r.Status == RequisitionStatus.Created ? nameof(Process) : nameof(View),
                    })
                });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(RequisitionModel model)
        {
            if (ModelState.IsValid)
            {
                using (var transaction = _dbContext.Database.BeginTransaction())
                {
                    var request = _dbContext.Requests
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Order.CreatedByUser)
                    .Include(r => r.Requisitions)
                    .Include(r => r.Ranking)
                    .Single(o => o.RequestId == model.RequestId);
                    if ((await _authorizationService.AuthorizeAsync(User, request, Policies.CreateRequisition)).Succeeded)
                    {
                        var requisition = new Requisition
                        {
                            Status = RequisitionStatus.Created,
                            CreatedBy = User.GetUserId(),
                            CreatedAt = _clock.SwedenNow.DateTime,
                            ImpersonatingCreatedBy = User.TryGetImpersonatorId(),
                            TravelCosts = model.TravelCosts,
                            Message = model.Message,
                            SessionStartedAt = model.SessionStartedAt,
                            SessionEndedAt = model.SessionEndedAt,
                            TimeWasteBeforeStartedAt = model.TimeWasteBeforeStartedAt,
                            TimeWasteAfterEndedAt = model.TimeWasteAfterEndedAt,
                            PriceRows = new List<RequisitionPriceRow>()
                        };
                        var priceInformation = _priceCalculationService.GetPrices(
                            model.SessionStartedAt,
                            model.SessionEndedAt,
                            EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>((CompetenceAndSpecialistLevel)request.CompetenceLevel),
                            request.Order.CustomerOrganisation.PriceListType,
                            request.Ranking.BrokerFee,
                            model.TimeWasteBeforeStartedAt,
                            model.TimeWasteAfterEndedAt
                        );

                        foreach (var row in priceInformation.PriceRows)
                        {
                            requisition.PriceRows.Add(new RequisitionPriceRow
                            {
                                StartAt = row.StartAt,
                                EndAt = row.EndAt,
                                IsBrokerFee = row.IsBrokerFee,
                                PriceListRowId = row.PriceListRowId,
                                TotalPrice = row.TotalPrice
                            });
                        }

                        request.CreateRequisition(requisition);
                        _dbContext.SaveChanges();
                        var replacingRequisition = request.Requisitions.SingleOrDefault(r => r.Status == RequisitionStatus.DeniedByCustomer &&
                            !r.ReplacedByRequisitionId.HasValue);
                        if (replacingRequisition != null)
                        {
                            replacingRequisition.ReplacedByRequisitionId = requisition.RequisitionId;
                            _dbContext.SaveChanges();
                        }
                        transaction.Commit();
                        CreateEmailOnRequisitionAction(requisition);
                        return RedirectToAction(nameof(View), new { id = requisition.RequisitionId });
                    }
                }
                return Forbid();
            }
            return View("Create", model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Approve(int requisitionId)
        {
            if (ModelState.IsValid)
            {
                var requisition = _dbContext.Requisitions
                    .Include(r => r.Request).ThenInclude(r => r.Order)
                    .Include(r => r.CreatedByUser)
                    .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                    .Single(r => r.RequisitionId == requisitionId);
                if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.Accept)).Succeeded)
                {
                    requisition.Approve(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId());
                    _dbContext.SaveChanges();
                    CreateEmailOnRequisitionAction(requisition);
                    return RedirectToAction(nameof(View), new { id = requisition.RequisitionId });
                }
                return Forbid();
            }
            return RedirectToAction(nameof(View), new { id = requisitionId });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Deny(DenyMessageDialogModel model)
        {
            if (ModelState.IsValid)
            {
                var requisition = _dbContext.Requisitions
                    .Include(r => r.Request).ThenInclude(r => r.Order)
                    .Include(r => r.CreatedByUser)
                    .Single(r => r.RequisitionId == model.ParentId);
                if ((await _authorizationService.AuthorizeAsync(User, requisition, Policies.Accept)).Succeeded)
                {
                    requisition.Deny(_clock.SwedenNow, User.GetUserId(), User.TryGetImpersonatorId(), model.Message);
                    _dbContext.SaveChanges();
                    CreateEmailOnRequisitionAction(requisition);
                    return RedirectToAction(nameof(View), new { id = requisition.RequisitionId });
                }
                return Forbid();
            }
            return RedirectToAction(nameof(Process), new { id = model.ParentId });
        }

        private void CreateEmailOnRequisitionAction(Requisition requisition)
        {
            string receipent;
            string subject;
            string body;
            string orderNumber = requisition.Request.Order.OrderNumber;
            switch (requisition.Status)
            {
                case RequisitionStatus.Created:
                    receipent = requisition.Request.Order.CreatedByUser.Email;
                    subject = body = $"En rekvisition har registrerats på avrop {orderNumber}";
                    break;
                case RequisitionStatus.Approved:
                    receipent = requisition.CreatedByUser.Email;
                    subject = body = $"Rekvisition för avrop {orderNumber} har godkänts";
                    body += "\n\nKostnader att fakturera:\n\n" + GetRequisitionPriceInformationForMail(requisition);
                    break;
                case RequisitionStatus.DeniedByCustomer:
                    receipent = requisition.CreatedByUser.Email;
                    subject = $"Rekvisition för avrop {orderNumber} har underkänts";
                    body = $"Rekvisition för avrop {orderNumber} har underkänts med följande meddelande:\n{requisition.DenyMessage}";
                    break;
                default:
                    throw new NotImplementedException();
            }
            if (!string.IsNullOrEmpty(receipent))
            {
                _dbContext.Add(new OutboundEmail(
                    receipent,
                    subject,
                    body +
                    "\n\nDetta mejl går inte att svara på.",
                    _clock.SwedenNow));
                _dbContext.SaveChanges();
            }
            else
            {
                _logger.LogInformation($"No email sent for requisition action {requisition.Status.GetDescription()} for ordernumber {orderNumber}, no email is set for user.");
            }
        }

        private string GetRequisitionPriceInformationForMail(Requisition requisition)
        {
            if (requisition.PriceRows == null)
            {
                return string.Empty;
            }
            else
            {
                DisplayPriceInformation priceInfo = _priceCalculationService.GetPriceInformationToDisplay(requisition.PriceRows.OfType<PriceRowBase>().ToList(), requisition.TravelCosts);
                string invoiceInfo = string.Empty;
                invoiceInfo += $"{priceInfo.TaxTypeAndCompetenceLevelDescription}\n\n";
                foreach (DisplayPriceRow dpr in priceInfo.DisplayPriceRows)
                {
                    invoiceInfo += $"{dpr.ShortDescription}:\n{dpr.Price.ToString("#,0.00 SEK")}\n\n";
                }
                invoiceInfo += $"Summa totalt att fakturera: {priceInfo.TotalPrice.ToString("#,0.00 SEK")}";
                return invoiceInfo;
            }
        }

        private PriceInformationModel GetRequisitionPriceInformation(Requisition requisition)
        {
            if (requisition.PriceRows == null)
            {
                return null;
            }
            PriceInformationModel model = new PriceInformationModel();
            model.PriceInformationToDisplay =_priceCalculationService.GetPriceInformationToDisplay(requisition.PriceRows.OfType<PriceRowBase>().ToList(), requisition.TravelCosts);
            model.Header = "Fakturainformation";
            model.UseDisplayHideInfo = false;
            return model;
        }

        private PriceInformationModel GetRequisitionPriceInformation(Request request)
        {
            if (request.PriceRows == null)
            {
                return null;
            }
            PriceInformationModel model = new PriceInformationModel();
            model.PriceInformationToDisplay = _priceCalculationService.GetPriceInformationToDisplay(request.PriceRows.OfType<PriceRowBase>().ToList(), request.ExpectedTravelCosts);
            model.Header = "Beräknat pris för avropssvar";
            model.UseDisplayHideInfo = false;
            return model;
        }

    }
}
