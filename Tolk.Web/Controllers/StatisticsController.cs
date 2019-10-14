﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;
using System.Collections.Generic;
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
        private readonly ILogger<StatisticsController> _logger;
        private readonly ISwedishClock _clock;
        private readonly StatisticsService _statService;

        public StatisticsController(
            ILogger<StatisticsController> logger,
            ISwedishClock clock,
            StatisticsService statService)
        {
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
                    case ReportType.OrdersForSystemAdministrator:
                        model.ReportItems = _statService.GetNoOfOrders(start, end, organisationId, customerUnits);
                        break;
                    case ReportType.DeliveredOrdersCustomer:
                    case ReportType.DeliveredOrdersSystemAdministrator:
                        model.ReportItems = _statService.GetNoOfDeliveredOrders(start, end, organisationId, customerUnits);
                        break;
                    case ReportType.RequestsForBrokers:
                        model.ReportItems = _statService.GetNoOfRequestsForBroker(start, end, brokerId.Value);
                        break;
                    case ReportType.DeliveredOrdersBrokers:
                        model.ReportItems = _statService.GetNoOfDeliveredRequestsForBroker(start, end, brokerId.Value);
                        break;
                    case ReportType.RequisitionsForCustomer:
                    case ReportType.RequisitionsForSystemAdministrator:
                        model.ReportItems = _statService.GetNoOfRequisitionsForCustomerAndSysAdmin(start, end, organisationId, customerUnits);
                        break;
                    case ReportType.RequisitionsForBroker:
                        model.ReportItems = _statService.GetNoOfRequisitionsForBroker(start, end, brokerId.Value);
                        break;
                    case ReportType.ComplaintsForCustomer:
                    case ReportType.ComplaintsForSystemAdministrator:
                        model.ReportItems = _statService.GetNoOfComplaintsForCustomerAndSysAdmin(start, end, organisationId, customerUnits);
                        break;
                    case ReportType.ComplaintsForBroker:
                        model.ReportItems = _statService.GetNoOfComplaintsForBroker(start, end, brokerId.Value);
                        break;
                }
                model.StartDate = start.ToSwedishString();
                model.EndDate = end.ToSwedishString();
                model.SelectedReportType = model.ReportType;
                return View(model);
            }
            return View(model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult GenerateExcelResult(GenerateExcelModel model)
        {
            DateTimeOffset start = model.StartDate.ToSwedishDateTime();
            DateTimeOffset end = model.EndDate.ToSwedishDateTime();
            var brokerId = User.TryGetBrokerId();
            var organisationId = User.TryGetCustomerOrganisationId();
            var customerUnits = User.IsInRole(Roles.CentralAdministrator) ? null : User.TryGetLocalAdminCustomerUnits();
            switch (model.SelectedReportType)
            {
                case ReportType.OrdersForCustomer:
                    var orders = _statService.GetOrders(start, end, organisationId, customerUnits).ToList();
                    return CreateExcelFile(StatisticsService.GetOrderExcelFileRows(orders, model.SelectedReportType), orders.First().CustomerOrganisation.Name, model.SelectedReportType);
                case ReportType.DeliveredOrdersCustomer:
                    var deliveredOrders = _statService.GetDeliveredOrders(start, end, organisationId, customerUnits).ToList();
                    return CreateExcelFile(StatisticsService.GetOrderExcelFileRows(deliveredOrders, model.SelectedReportType), deliveredOrders.First().CustomerOrganisation.Name, model.SelectedReportType);
                case ReportType.DeliveredOrdersBrokers:
                    var deliveredOrdersBrokers = _statService.GetDeliveredRequestsForBroker(start, end, brokerId.Value).ToList();
                    return CreateExcelFile(StatisticsService.GetRequestExcelFileRows(deliveredOrdersBrokers, model.SelectedReportType), deliveredOrdersBrokers.First().Ranking.Broker.Name, model.SelectedReportType);
                case ReportType.RequestsForBrokers:
                    var requestsForBrokers = _statService.GetRequestsForBroker(start, end, brokerId.Value).ToList();
                    return CreateExcelFile(StatisticsService.GetRequestExcelFileRows(requestsForBrokers, model.SelectedReportType), requestsForBrokers.First().Ranking.Broker.Name, model.SelectedReportType);
                case ReportType.OrdersForSystemAdministrator:
                    var ordersForSystemAdministrator = _statService.GetOrders(start, end, organisationId).ToList();
                    return CreateExcelFile(StatisticsService.GetOrderExcelFileRows(ordersForSystemAdministrator, model.SelectedReportType), string.Empty, model.SelectedReportType);
                case ReportType.DeliveredOrdersSystemAdministrator:
                    var deliveredOrdersForSystemAdministrator = _statService.GetDeliveredOrders(start, end, organisationId).ToList();
                    return CreateExcelFile(StatisticsService.GetOrderExcelFileRows(deliveredOrdersForSystemAdministrator, model.SelectedReportType), string.Empty, model.SelectedReportType);
                case ReportType.RequisitionsForSystemAdministrator:
                    var requisitionsForSystemAdministrator = _statService.GetRequisitionsForCustomerAndSysAdmin(start, end, organisationId).ToList();
                    return CreateExcelFile(StatisticsService.GetRequisitionsExcelFileRows(requisitionsForSystemAdministrator, model.SelectedReportType), string.Empty, model.SelectedReportType);
                case ReportType.RequisitionsForBroker:
                    var requisitionsForBroker = _statService.GetRequisitionsForBroker(start, end, brokerId.Value).ToList();
                    return CreateExcelFile(StatisticsService.GetRequisitionsExcelFileRows(requisitionsForBroker, model.SelectedReportType), requisitionsForBroker.First().Request.Ranking.Broker.Name, model.SelectedReportType);
                case ReportType.RequisitionsForCustomer:
                    var requisitionsForCustomer = _statService.GetRequisitionsForCustomerAndSysAdmin(start, end, organisationId, customerUnits).ToList();
                    return CreateExcelFile(StatisticsService.GetRequisitionsExcelFileRows(requisitionsForCustomer, model.SelectedReportType), requisitionsForCustomer.First().Request.Order.CustomerOrganisation.Name, model.SelectedReportType);
                case ReportType.ComplaintsForSystemAdministrator:
                    var complaintsForSystemAdministrator = _statService.GetComplaintsForCustomerAndSysAdmin(start, end, organisationId).ToList();
                    return CreateExcelFile(StatisticsService.GetComplaintsExcelFileRows(complaintsForSystemAdministrator, model.SelectedReportType), string.Empty, model.SelectedReportType);
                case ReportType.ComplaintsForBroker:
                    var complaintsForBroker = _statService.GetComplaintsForBroker(start, end, brokerId.Value).ToList();
                    return CreateExcelFile(StatisticsService.GetComplaintsExcelFileRows(complaintsForBroker, model.SelectedReportType), complaintsForBroker.First().Request.Ranking.Broker.Name, model.SelectedReportType);
                case ReportType.ComplaintsForCustomer:
                    var complaintsForCustomer = _statService.GetComplaintsForCustomerAndSysAdmin(start, end, organisationId, customerUnits).ToList();
                    return CreateExcelFile(StatisticsService.GetComplaintsExcelFileRows(complaintsForCustomer, model.SelectedReportType), complaintsForCustomer.First().Request.Order.CustomerOrganisation.Name, model.SelectedReportType);
            }
            return RedirectToAction(nameof(List));
        }

        private ActionResult CreateExcelFile(IEnumerable<ReportRow> rows, string organisationName, ReportType reportType)
        {
            string fileName = $"{EnumHelper.GetDescription(reportType)}_{organisationName}_{_clock.SwedenNow.DateTime.ToSwedishString("yyyy-MM-dd HH:mm")}.xlsx";
            return File(_statService.CreateExcelFile(rows, organisationName, reportType), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [Authorize(Roles = Roles.AdminRoles)]
        public ActionResult Reports()
        {
            return View();
        }
    }
}
