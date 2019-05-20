using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;
using System.Collections.Generic;
using Tolk.BusinessLogic.Data;
using Tolk.Web.Authorization;
using Tolk.Web.Models;
using Tolk.Web.Helpers;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.BusinessLogic.Services;

namespace Tolk.Web.Controllers
{

    [Authorize(Policies.SystemCentralLocalAdmin)]
    public class StatisticsController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<StatisticsController> _logger;
        private readonly ISwedishClock _clock;
        private readonly StatisticsService _statService;

        public StatisticsController(
            TolkDbContext dbContext,
            ILogger<StatisticsController> logger,
            ISwedishClock clock,
            StatisticsService statService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _clock = clock;
            _statService = statService;
        }

        public ActionResult List()
        {
            return View();
        }

        [Authorize(Roles = Roles.SystemAdministrator)]
        public ActionResult Dashboard()
        {
            int totalNoOfOrders = _statService.TotalNoOfOrders;
            StatisticsDashboardModel model = new StatisticsDashboardModel
            {
                TotalNoOfOrders = totalNoOfOrders,
                WeeklyStatisticsModels = _statService.GetWeeklyStatistics(),
                OrderStatisticsModels = totalNoOfOrders > 0 ? _statService.GetOrderStatistics() : null
            };
            return View(model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult List(ReportSearchModel model)
        {
            if (ModelState.IsValid)
            {
                DateTimeOffset start = model.ReportDate.Start ?? new DateTime(2019, 01, 01);
                DateTimeOffset end = model.ReportDate.End ?? DateTime.MaxValue.Date;
                var brokerId = User.TryGetBrokerId();
                var organisationId = User.TryGetCustomerOrganisationId();
                var customerUnits = User.IsInRole(Roles.CentralAdministrator) ? null : User.TryGetLocalAdminCustomerUnits();
                switch (model.ReportType)
                {
                    case ReportType.OrdersForCustomer:
                        model.ReportItems = _statService.GetOrders(start, end, organisationId, customerUnits).Count();
                        break;
                    case ReportType.DeliveredOrdersCustomer:
                        model.ReportItems = _statService.GetDeliveredOrders(start, end, organisationId, customerUnits).Count();
                        break;
                    case ReportType.RequestsForBrokers:
                        model.ReportItems = _statService.GetRequestsForBroker(start, end, brokerId.Value).Count();
                        break;
                    case ReportType.DeliveredOrdersBrokers:
                        model.ReportItems = _statService.GetDeliveredRequestsForBroker(start, end, brokerId.Value).Count();
                        break;
                    case ReportType.OrdersForSystemAdministrator:
                        model.ReportItems = _statService.GetOrders(start, end, organisationId).Count();
                        break;
                    case ReportType.DeliveredOrdersSystemAdministrator:
                        model.ReportItems = _statService.GetDeliveredOrders(start, end, organisationId).Count();
                        break;
                    case ReportType.RequisitionsForCustomer:
                        model.ReportItems = _statService.GetRequisitionsForCustomerAndSysAdmin(start, end, organisationId, customerUnits).Count();
                        break;
                    case ReportType.RequisitionsForBroker:
                        model.ReportItems = _statService.GetRequisitionsForBroker(start, end, brokerId.Value).Count();
                        break;
                    case ReportType.RequisitionsForSystemAdministrator:
                        model.ReportItems = _statService.GetRequisitionsForCustomerAndSysAdmin(start, end, organisationId).Count();
                        break;
                    case ReportType.ComplaintsForCustomer:
                        model.ReportItems = _statService.GetComplaintsForCustomerAndSysAdmin(start, end, organisationId, customerUnits).Count();
                        break;
                    case ReportType.ComplaintsForBroker:
                        model.ReportItems = _statService.GetComplaintsForBroker(start, end, brokerId.Value).Count();
                        break;
                    case ReportType.ComplaintsForSystemAdministrator:
                        model.ReportItems = _statService.GetComplaintsForCustomerAndSysAdmin(start, end, organisationId).Count();
                        break;
                }
                model.StartDate = start.ToString();
                model.EndDate = end.ToString();
                return View(model);
            }
            return View(model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult GenerateExcelResult(GenerateExcelModel model)
        {
            DateTimeOffset start = Convert.ToDateTime(model.StartDate);
            DateTimeOffset end = Convert.ToDateTime(model.EndDate);
            var brokerId = User.TryGetBrokerId();
            var organisationId = User.TryGetCustomerOrganisationId();
            var customerUnits = User.IsInRole(Roles.CentralAdministrator) ? null : User.TryGetLocalAdminCustomerUnits();
            switch (model.ReportType)
            {
                case ReportType.OrdersForCustomer:
                    var orders = _statService.GetOrders(start, end, organisationId, customerUnits);
                    return CreateExcelFile(StatisticsService.GetOrderExcelFileRows(orders, model.ReportType), orders.First().CustomerOrganisation.Name, model.ReportType);
                case ReportType.DeliveredOrdersCustomer:
                    var deliveredOrders = _statService.GetDeliveredOrders(start, end, organisationId, customerUnits);
                    return CreateExcelFile(StatisticsService.GetOrderExcelFileRows(deliveredOrders, model.ReportType), deliveredOrders.First().CustomerOrganisation.Name, model.ReportType);
                case ReportType.DeliveredOrdersBrokers:
                    var deliveredOrdersBrokers = _statService.GetDeliveredRequestsForBroker(start, end, brokerId.Value);
                    return CreateExcelFile(StatisticsService.GetRequestExcelFileRows(deliveredOrdersBrokers, model.ReportType), deliveredOrdersBrokers.First().Ranking.Broker.Name, model.ReportType);
                case ReportType.RequestsForBrokers:
                    var requestsForBrokers = _statService.GetRequestsForBroker(start, end, brokerId.Value);
                    return CreateExcelFile(StatisticsService.GetRequestExcelFileRows(requestsForBrokers, model.ReportType), requestsForBrokers.First().Ranking.Broker.Name, model.ReportType);
                case ReportType.OrdersForSystemAdministrator:
                    var ordersForSystemAdministrator = _statService.GetOrders(start, end, organisationId);
                    return CreateExcelFile(StatisticsService.GetOrderExcelFileRows(ordersForSystemAdministrator, model.ReportType), string.Empty, model.ReportType);
                case ReportType.DeliveredOrdersSystemAdministrator:
                    var deliveredOrdersForSystemAdministrator = _statService.GetDeliveredOrders(start, end, organisationId);
                    return CreateExcelFile(StatisticsService.GetOrderExcelFileRows(deliveredOrdersForSystemAdministrator, model.ReportType), string.Empty, model.ReportType);
                case ReportType.RequisitionsForSystemAdministrator:
                    var requisitionsForSystemAdministrator = _statService.GetRequisitionsForCustomerAndSysAdmin(start, end, organisationId);
                    return CreateExcelFile(StatisticsService.GetRequisitionsExcelFileRows(requisitionsForSystemAdministrator, model.ReportType), string.Empty, model.ReportType);
                case ReportType.RequisitionsForBroker:
                    var requisitionsForBroker = _statService.GetRequisitionsForBroker(start, end, brokerId.Value);
                    return CreateExcelFile(StatisticsService.GetRequisitionsExcelFileRows(requisitionsForBroker, model.ReportType), requisitionsForBroker.First().Request.Ranking.Broker.Name, model.ReportType);
                case ReportType.RequisitionsForCustomer:
                    var requisitionsForCustomer = _statService.GetRequisitionsForCustomerAndSysAdmin(start, end, organisationId, customerUnits);
                    return CreateExcelFile(StatisticsService.GetRequisitionsExcelFileRows(requisitionsForCustomer, model.ReportType), requisitionsForCustomer.First().Request.Order.CustomerOrganisation.Name, model.ReportType);
                case ReportType.ComplaintsForSystemAdministrator:
                    var complaintsForSystemAdministrator = _statService.GetComplaintsForCustomerAndSysAdmin(start, end, organisationId);
                    return CreateExcelFile(StatisticsService.GetComplaintsExcelFileRows(complaintsForSystemAdministrator, model.ReportType), string.Empty, model.ReportType);
                case ReportType.ComplaintsForBroker:
                    var complaintsForBroker = _statService.GetComplaintsForBroker(start, end, brokerId.Value);
                    return CreateExcelFile(StatisticsService.GetComplaintsExcelFileRows(complaintsForBroker, model.ReportType), complaintsForBroker.First().Request.Ranking.Broker.Name, model.ReportType);
                case ReportType.ComplaintsForCustomer:
                    var complaintsForCustomer = _statService.GetComplaintsForCustomerAndSysAdmin(start, end, organisationId, customerUnits);
                    return CreateExcelFile(StatisticsService.GetComplaintsExcelFileRows(complaintsForCustomer, model.ReportType), complaintsForCustomer.First().Request.Order.CustomerOrganisation.Name, model.ReportType);
            }
            return RedirectToAction(nameof(List));
        }

        private ActionResult CreateExcelFile(IEnumerable<ReportRow> rows, string organisationName, ReportType reportType)
        {
            string fileName = $"{EnumHelper.GetDescription(reportType)}_{organisationName}_{_clock.SwedenNow.DateTime.ToString("yyyy-MM-dd HH:mm")}.xlsx";
            return File(_statService.CreateExcelFile(rows, organisationName, reportType), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [Authorize(Roles = Roles.CentralAdministrator)]
        [Authorize(Policy = Policies.Broker)]
        public ActionResult ListLanguages()
        {
            return View(
                _dbContext.Languages.Where(l => l.Active == true)
                .OrderBy(l => l.Name).Select(l => new LanguageListItem
                {
                    ISO_639_Code = l.ISO_639_Code,
                    Name = l.Name,
                    TellusName = l.TellusName
                }));
        }

        [Authorize(Roles = Roles.AdminRoles)]
        public ActionResult Reports()
        {
            return View();
        }
    }
}
