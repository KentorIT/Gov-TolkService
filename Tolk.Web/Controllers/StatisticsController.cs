using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{

    [Authorize(Policies.SystemCentralLocalAdmin)]
    public class StatisticsController : Controller
    {
        private readonly ISwedishClock _clock;
        private readonly StatisticsService _statService;

        public StatisticsController(
            ISwedishClock clock,
            StatisticsService statService)
        {
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
                    case ReportType.DeliveredOrdersBrokers:
                        model.ReportItems = _statService.GetNoOfDeliveredOrders(start, end, organisationId, customerUnits, brokerId);
                        break;
                    case ReportType.RequestsForBrokers:
                        model.ReportItems = _statService.GetNoOfRequestsForBroker(start, end, brokerId.Value);
                        break;
                    case ReportType.RequisitionsForCustomer:
                    case ReportType.RequisitionsForSystemAdministrator:
                    case ReportType.RequisitionsForBroker:
                        model.ReportItems = _statService.GetNoOfRequisitions(start, end, organisationId, customerUnits, brokerId);
                        break;
                    case ReportType.ComplaintsForCustomer:
                    case ReportType.ComplaintsForSystemAdministrator:
                    case ReportType.ComplaintsForBroker:
                        model.ReportItems = _statService.GetNoOfComplaints(start, end, organisationId, customerUnits, brokerId);
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
                    var orders = _statService.GetOrders(start, end, organisationId, customerUnits);
                    return CreateExcelFile(StatisticsService.GetOrderExcelFileRows(orders, model.SelectedReportType), orders.OrderRequests.First().CustomerName, model.SelectedReportType);
                case ReportType.DeliveredOrdersCustomer:
                    var deliveredOrders = _statService.GetDeliveredOrders(start, end, organisationId, customerUnits);
                    return CreateExcelFile(StatisticsService.GetOrderExcelFileRows(deliveredOrders, model.SelectedReportType), deliveredOrders.OrderRequests.First().CustomerName, model.SelectedReportType);
                case ReportType.DeliveredOrdersBrokers:
                    var deliveredOrdersBrokers = _statService.GetDeliveredRequestsForBroker(start, end, brokerId.Value);
                    return CreateExcelFile(StatisticsService.GetOrderExcelFileRows(deliveredOrdersBrokers, model.SelectedReportType), deliveredOrdersBrokers.OrderRequests.First().BrokerName, model.SelectedReportType);
                case ReportType.RequestsForBrokers:
                    var requestsForBrokers = _statService.GetRequestsForBroker(start, end, brokerId.Value);
                    return CreateExcelFile(StatisticsService.GetOrderExcelFileRows(requestsForBrokers, model.SelectedReportType), requestsForBrokers.OrderRequests.First().BrokerName, model.SelectedReportType);
                case ReportType.OrdersForSystemAdministrator:
                    var ordersForSystemAdministrator = _statService.GetOrders(start, end, organisationId);
                    return CreateExcelFile(StatisticsService.GetOrderExcelFileRows(ordersForSystemAdministrator, model.SelectedReportType), string.Empty, model.SelectedReportType);
                case ReportType.DeliveredOrdersSystemAdministrator:
                    var deliveredOrdersForSystemAdministrator = _statService.GetDeliveredOrders(start, end, organisationId);
                    return CreateExcelFile(StatisticsService.GetOrderExcelFileRows(deliveredOrdersForSystemAdministrator, model.SelectedReportType), string.Empty, model.SelectedReportType);
                case ReportType.RequisitionsForSystemAdministrator:
                    var requisitionsForSystemAdministrator = _statService.GetRequisitions(start, end, organisationId);
                    return CreateExcelFile(StatisticsService.GetRequisitionsExcelFileRows(requisitionsForSystemAdministrator, model.SelectedReportType), string.Empty, model.SelectedReportType);
                case ReportType.RequisitionsForBroker:
                    var requisitionsForBroker = _statService.GetRequisitions(start, end, null, null, brokerId.Value);
                    return CreateExcelFile(StatisticsService.GetRequisitionsExcelFileRows(requisitionsForBroker, model.SelectedReportType), requisitionsForBroker.Requisitions.First().BrokerName, model.SelectedReportType);
                case ReportType.RequisitionsForCustomer:
                    var requisitionsForCustomer = _statService.GetRequisitions(start, end, organisationId, customerUnits);
                    return CreateExcelFile(StatisticsService.GetRequisitionsExcelFileRows(requisitionsForCustomer, model.SelectedReportType), requisitionsForCustomer.Requisitions.First().CustomerName, model.SelectedReportType);
                case ReportType.ComplaintsForSystemAdministrator:
                    var complaintsForSystemAdministrator = _statService.GetComplaints(start, end, organisationId);
                    return CreateExcelFile(StatisticsService.GetComplaintsExcelFileRows(complaintsForSystemAdministrator, model.SelectedReportType), string.Empty, model.SelectedReportType);
                case ReportType.ComplaintsForBroker:
                    var complaintsForBroker = _statService.GetComplaints(start, end, null, null, brokerId.Value);
                    return CreateExcelFile(StatisticsService.GetComplaintsExcelFileRows(complaintsForBroker, model.SelectedReportType), complaintsForBroker.Complaints.First().BrokerName, model.SelectedReportType);
                case ReportType.ComplaintsForCustomer:
                    var complaintsForCustomer = _statService.GetComplaints(start, end, organisationId, customerUnits);
                    return CreateExcelFile(StatisticsService.GetComplaintsExcelFileRows(complaintsForCustomer, model.SelectedReportType), complaintsForCustomer.Complaints.First().CustomerName, model.SelectedReportType);
            }
            return RedirectToAction(nameof(List));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "notallowed to use using here, the code throws...")]
        private ActionResult CreateExcelFile(IEnumerable<ReportRow> rows, string organisationName, ReportType reportType)
        {
            string fileName = $"{EnumHelper.GetDescription(reportType)}_{organisationName}_{_clock.SwedenNow.DateTime.ToSwedishString("yyyy-MM-dd HH:mm")}.xlsx";
            return File(StatisticsService.CreateExcelFile(rows, reportType), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [Authorize(Roles = Roles.AdminRoles)]
        public ActionResult Reports()
        {
            return View();
        }
    }
}
