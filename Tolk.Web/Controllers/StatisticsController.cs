using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using Tolk.BusinessLogic.Data;
using Tolk.Web.Authorization;
using Tolk.Web.Models;
using Tolk.Web.Helpers;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Utilities;
using ClosedXML.Excel;
using Tolk.BusinessLogic.Services;

namespace Tolk.Web.Controllers
{

    [Authorize(Roles = Roles.AdminRoles)]
    public class StatisticsController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<StatisticsController> _logger;
        private readonly ISwedishClock _clock;

        public StatisticsController(
            TolkDbContext dbContext,
            ILogger<StatisticsController> logger,
            ISwedishClock clock)
        {
            _dbContext = dbContext;
            _logger = logger;
            _clock = clock;
        }

        [Authorize(Roles = Roles.AdminRoles)]
        public ActionResult List()
        {
            return View();
        }

        [Authorize(Roles = Roles.Admin)]
        public ActionResult Dashboard()
        {
            return View();
        }

        [Authorize(Roles = Roles.AdminRoles)]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult List(ReportSearchModel model)
        {
            if (ModelState.IsValid)
            {
                DateTimeOffset start = model.ReportDate.Start ?? new DateTime(2019, 01, 01);
                DateTimeOffset end = model.ReportDate.End ?? DateTime.MaxValue.Date;
                switch (model.ReportType)
                {
                    case ReportType.OrdersForCustomer:
                        model.ReportItems = GetOrdersForCustomer(start, end).Count();
                        break;
                    case ReportType.DeliveredOrdersCustomer:
                        model.ReportItems = GetDeliveredOrdersForCustomer(start, end).Count();
                        break;
                    case ReportType.RequestsForBrokers:
                        model.ReportItems = GetRequestsForBrokers(start, end).Count();
                        break;
                    case ReportType.DeliveredOrdersBrokers:
                        model.ReportItems = GetDeliveredOrderForBrokers(start, end).Count();
                        break;
                    case ReportType.OrdersForSystemAdministrator:
                        model.ReportItems = GetOrdersForSystemAdministrator(start, end).Count();
                        break;
                    case ReportType.DeliveredOrdersSystemAdministrator:
                        model.ReportItems = GetDeliveredOrdersForSystemAdministrator(start, end).Count();
                        break;
                    case ReportType.RequisitionsForCustomer:
                        model.ReportItems = GetRequisitionsForCustomer(start, end).Count();
                        break;
                    case ReportType.RequisitionsForBroker:
                        model.ReportItems = GetRequisitionsForBroker(start, end).Count();
                        break;
                    case ReportType.RequisitionsForSystemAdministrator:
                        model.ReportItems = GetRequisitionsForSystemAdministrator(start, end).Count();
                        break;
                    case ReportType.ComplaintsForCustomer:
                        model.ReportItems = GetComplaintsForCustomer(start, end).Count();
                        break;
                    case ReportType.ComplaintsForBroker:
                        model.ReportItems = GetComplaintsForBroker(start, end).Count();
                        break;
                    case ReportType.ComplaintsForSystemAdministrator:
                        model.ReportItems = GetComplaintsForSystemAdministrator(start, end).Count();
                        break;
                }
                model.StartDate = start.ToString();
                model.EndDate = end.ToString();
                return View(model);
            }
            return View(model);
        }

        [Authorize(Roles = Roles.AdminRoles)]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult GenerateExcelResult(GenerateExcelModel model)
        {
            DateTimeOffset start = Convert.ToDateTime(model.StartDate);
            DateTimeOffset end = Convert.ToDateTime(model.EndDate);
            switch (model.ReportType)
            {
                case ReportType.OrdersForCustomer:
                    var orders = GetOrdersForCustomer(start, end);
                    return CreateExcelFile(GetOrderExcelFileRows(orders, model.ReportType), orders.First().CustomerOrganisation.Name, model.ReportType);
                case ReportType.DeliveredOrdersCustomer:
                    var deliveredOrders = GetDeliveredOrdersForCustomer(start, end);
                    return CreateExcelFile(GetOrderExcelFileRows(deliveredOrders, model.ReportType), deliveredOrders.First().CustomerOrganisation.Name, model.ReportType);
                case ReportType.DeliveredOrdersBrokers:
                    var deliveredOrdersBrokers = GetDeliveredOrderForBrokers(start, end);
                    return CreateExcelFile(GetRequestExcelFileRows(deliveredOrdersBrokers, model.ReportType), deliveredOrdersBrokers.First().Ranking.Broker.Name, model.ReportType);
                case ReportType.RequestsForBrokers:
                    var requestsForBrokers = GetRequestsForBrokers(start, end);
                    return CreateExcelFile(GetRequestExcelFileRows(requestsForBrokers, model.ReportType), requestsForBrokers.First().Ranking.Broker.Name, model.ReportType);
                case ReportType.OrdersForSystemAdministrator:
                    var ordersForSystemAdministrator = GetOrdersForSystemAdministrator(start, end);
                    return CreateExcelFile(GetOrderExcelFileRows(ordersForSystemAdministrator, model.ReportType), string.Empty, model.ReportType);
                case ReportType.DeliveredOrdersSystemAdministrator:
                    var deliveredOrdersForSystemAdministrator = GetDeliveredOrdersForSystemAdministrator(start, end);
                    return CreateExcelFile(GetOrderExcelFileRows(deliveredOrdersForSystemAdministrator, model.ReportType), string.Empty, model.ReportType);
                case ReportType.RequisitionsForSystemAdministrator:
                    var requisitionsForSystemAdministrator = GetRequisitionsForSystemAdministrator(start, end);
                    return CreateExcelFile(GetRequisitionsExcelFileRows(requisitionsForSystemAdministrator), string.Empty, model.ReportType);
                case ReportType.RequisitionsForBroker:
                    var requisitionsForBroker= GetRequisitionsForBroker(start, end);
                    return CreateExcelFile(GetRequisitionsExcelFileRows(requisitionsForBroker), requisitionsForBroker.First().Request.Ranking.Broker.Name, model.ReportType);
                case ReportType.RequisitionsForCustomer:
                    var requisitionsForCustomer = GetRequisitionsForCustomer(start, end);
                    return CreateExcelFile(GetRequisitionsExcelFileRows(requisitionsForCustomer), requisitionsForCustomer.First().Request.Order.CustomerOrganisation.Name, model.ReportType);
                case ReportType.ComplaintsForSystemAdministrator:
                    var complaintsForSystemAdministrator = GetComplaintsForSystemAdministrator(start, end);
                    return CreateExcelFile(GetComplaintsExcelFileRows(complaintsForSystemAdministrator), string.Empty, model.ReportType);
                case ReportType.ComplaintsForBroker:
                    var complaintsForBroker = GetComplaintsForBroker(start, end);
                    return CreateExcelFile(GetComplaintsExcelFileRows(complaintsForBroker), complaintsForBroker.First().Request.Ranking.Broker.Name, model.ReportType);
                case ReportType.ComplaintsForCustomer:
                    var complaintsForCustomer = GetComplaintsForCustomer(start, end);
                    return CreateExcelFile(GetComplaintsExcelFileRows(complaintsForCustomer), complaintsForCustomer.First().Request.Order.CustomerOrganisation.Name, model.ReportType);
            }
            return RedirectToAction(nameof(List));
        }

        private List<Request> GetRequestsForBrokers(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Requests
                      .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                      .Include(r => r.AnsweringUser)
                      .Include(r => r.Interpreter)
                      .Include(r => r.Requisitions)
                      .Include(r => r.Complaints)
                      .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                      .Include(r => r.Order).ThenInclude(o => o.Language)
                      .Include(r => r.Order).ThenInclude(o => o.Region)
                      .Include(r => r.Order).ThenInclude(o => o.Requests)
                      .OrderBy(r => r.Order.OrderNumber)
                      .Where(r => r.Ranking.BrokerId == User.GetBrokerId() && r.CreatedAt.Date >= start.Date && r.CreatedAt.Date <= end.Date
                          && !r.StatusNotToBeDisplayedForBroker).ToList();
        }

        private List<Request> GetDeliveredOrderForBrokers(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Requests
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.AnsweringUser)
                    .Include(r => r.Interpreter)
                    .Include(r => r.Requisitions)
                    .Include(r => r.Complaints)
                    .Include(r => r.PriceRows)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Order).ThenInclude(o => o.Language)
                    .Include(r => r.Order).ThenInclude(o => o.Region)
                    .OrderBy(r => r.Order.OrderNumber)
                    .Where(r => r.Ranking.BrokerId == User.GetBrokerId()
                        && r.Order.EndAt <= _clock.SwedenNow && r.Order.StartAt.Date >= start.Date && r.Order.StartAt.Date <= end.Date
                        && (r.Order.Status == OrderStatus.Delivered || r.Order.Status == OrderStatus.DeliveryAccepted || r.Order.Status == OrderStatus.ResponseAccepted)).ToList();
        }

        private List<Order> GetDeliveredOrdersForCustomer(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Orders
                    .Include(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(o => o.Requests).ThenInclude(r => r.Interpreter)
                    .Include(o => o.Requests).ThenInclude(r => r.Requisitions)
                    .Include(o => o.Requests).ThenInclude(r => r.Complaints)
                    .Include(o => o.Requests).ThenInclude(r => r.PriceRows)
                    .Include(o => o.CustomerOrganisation)
                    .Include(o => o.Language)
                    .Include(o => o.Region)
                    .Include(o => o.CreatedByUser)
                    .OrderBy(o => o.OrderNumber)
                    .Where(o => o.CustomerOrganisationId == User.GetCustomerOrganisationId()
                        && o.EndAt <= _clock.SwedenNow && o.StartAt.Date >= start.Date && o.StartAt.Date <= end.Date
                        && (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.DeliveryAccepted || o.Status == OrderStatus.ResponseAccepted)).ToList();
        }

        private List<Order> GetOrdersForCustomer(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Orders
                    .Include(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(o => o.Requests).ThenInclude(r => r.Interpreter)
                    .Include(o => o.Requests).ThenInclude(r => r.Requisitions)
                    .Include(o => o.Requests).ThenInclude(r => r.Complaints)
                    .Include(o => o.CustomerOrganisation)
                    .Include(o => o.Language)
                    .Include(o => o.Region)
                    .Include(o => o.CreatedByUser)
                    .OrderBy(o => o.OrderNumber)
                    .Where(o => o.CustomerOrganisationId == User.GetCustomerOrganisationId() && o.CreatedAt.Date >= start.Date && o.CreatedAt.Date <= end.Date).ToList();
        }

        private List<Order> GetDeliveredOrdersForSystemAdministrator(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Orders
                    .Include(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(o => o.Requests).ThenInclude(r => r.Interpreter)
                    .Include(o => o.Requests).ThenInclude(r => r.Requisitions)
                    .Include(o => o.Requests).ThenInclude(r => r.Complaints)
                    .Include(o => o.Requests).ThenInclude(r => r.PriceRows)
                    .Include(o => o.CustomerOrganisation)
                    .Include(o => o.Language)
                    .Include(o => o.Region)
                    .Include(o => o.CreatedByUser)
                    .OrderBy(o => o.OrderNumber)
                    .Where(o => o.EndAt <= _clock.SwedenNow && o.StartAt.Date >= start.Date && o.StartAt.Date <= end.Date
                        && (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.DeliveryAccepted || o.Status == OrderStatus.ResponseAccepted)).ToList();
        }

        private List<Order> GetOrdersForSystemAdministrator(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Orders
                    .Include(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(o => o.Requests).ThenInclude(r => r.Interpreter)
                    .Include(o => o.Requests).ThenInclude(r => r.Requisitions)
                    .Include(o => o.Requests).ThenInclude(r => r.Complaints)
                    .Include(o => o.CustomerOrganisation)
                    .Include(o => o.Language)
                    .Include(o => o.Region)
                    .Include(o => o.CreatedByUser)
                    .OrderBy(o => o.OrderNumber)
                    .Where(o => o.CreatedAt.Date >= start.Date && o.CreatedAt.Date <= end.Date).ToList();
        }

        private List<Requisition> GetRequisitionsForCustomer(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Requisitions
                    .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                    .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Region)
                    .Include(r => r.Request).ThenInclude(r => r.Interpreter)
                    .Include(r => r.MealBreaks)
                    .Include(r => r.CreatedByUser)
                    .Include(r => r.PriceRows)
                    .OrderBy(r => r.Request.Order.OrderNumber)
                    .Where(r => r.CreatedAt.Date >= start.Date && r.CreatedAt.Date <= end.Date 
                    && r.Request.Order.CustomerOrganisationId == User.GetCustomerOrganisationId() && r.ReplacedByRequisitionId == null).ToList();
        }

        private List<Requisition> GetRequisitionsForBroker(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Requisitions
                    .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                    .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Region)
                    .Include(r => r.Request).ThenInclude(r => r.Interpreter)
                    .Include(r => r.MealBreaks)
                    .Include(r => r.CreatedByUser)
                    .Include(r => r.PriceRows)
                    .OrderBy(r => r.Request.Order.OrderNumber)
                    .Where(r => r.CreatedAt.Date >= start.Date && r.CreatedAt.Date <= end.Date 
                    && r.Request.Ranking.BrokerId == User.GetBrokerId() && r.ReplacedByRequisitionId == null).ToList();
        }

        private List<Requisition> GetRequisitionsForSystemAdministrator(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Requisitions
                    .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                    .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Region)
                    .Include(r => r.Request).ThenInclude(r => r.Interpreter)
                    .Include(r => r.MealBreaks)
                    .Include(r => r.CreatedByUser)
                    .Include(r => r.PriceRows)
                    .OrderBy(r => r.Request.Order.OrderNumber)
                    .Where(r => r.CreatedAt.Date >= start.Date && r.CreatedAt.Date <= end.Date && r.ReplacedByRequisitionId == null).ToList();
        }

        private List<Complaint> GetComplaintsForSystemAdministrator(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Complaints
                    .Include(c => c.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                    .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Region)
                    .Include(c => c.Request).ThenInclude(r => r.Interpreter)
                    .Include(c => c.Request).ThenInclude(r => r.Requisitions)
                    .Include(c => c.CreatedByUser)
                    .OrderBy(c => c.Request.Order.OrderNumber)
                    .Where(c => c.CreatedAt.Date >= start.Date && c.CreatedAt.Date <= end.Date).ToList();
        }

        private List<Complaint> GetComplaintsForCustomer(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Complaints
                    .Include(c => c.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                    .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Region)
                    .Include(c => c.Request).ThenInclude(r => r.Interpreter)
                    .Include(c => c.Request).ThenInclude(r => r.Requisitions)
                    .Include(c => c.CreatedByUser)
                    .OrderBy(c => c.Request.Order.OrderNumber)
                    .Where(c => c.CreatedAt.Date >= start.Date && c.CreatedAt.Date <= end.Date && c.Request.Order.CustomerOrganisationId == User.GetCustomerOrganisationId()).ToList();
        }

        private List<Complaint> GetComplaintsForBroker(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Complaints
                    .Include(c => c.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                    .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Region)
                    .Include(c => c.Request).ThenInclude(r => r.Interpreter)
                    .Include(c => c.Request).ThenInclude(r => r.Requisitions)
                    .Include(c => c.CreatedByUser)
                    .OrderBy(c => c.Request.Order.OrderNumber)
                    .Where(c => c.CreatedAt.Date >= start.Date && c.CreatedAt.Date <= end.Date && c.Request.Ranking.BrokerId == User.GetBrokerId()).ToList();
        }

        private ActionResult CreateExcelFile(IEnumerable<ReportRowModel> rows, string organisationName, ReportType reportType)
        {
            using (var workbook = new XLWorkbook())
            {
                var rowsWorksheet = workbook.Worksheets.Add(EnumHelper.GetDescription(reportType));
                char columnLetter = 'A';
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "BokningsId";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.OrderNumber);
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = reportType.GetCustomName();
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.ReportDate);
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Språk";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.Language);
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Län";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.Region);
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Uppdragstyp";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.AssignmentType);
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Tolkens kompetensnivå";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.InterpreterCompetenceLevel.GetDescription());
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Tolk-ID";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.InterpreterId);
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Tid för uppdrag";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.AssignmentDate.ToString());
                switch (reportType)
                {
                    case ReportType.RequestsForBrokers:
                    case ReportType.OrdersForCustomer:
                    case ReportType.OrdersForSystemAdministrator:
                    case ReportType.RequisitionsForSystemAdministrator:
                    case ReportType.RequisitionsForBroker:
                    case ReportType.RequisitionsForCustomer:
                    case ReportType.ComplaintsForCustomer:
                    case ReportType.ComplaintsForBroker:
                    case ReportType.ComplaintsForSystemAdministrator:
                        rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Status";
                        rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.Status.ToString());
                        break;
                    case ReportType.DeliveredOrdersBrokers:
                    case ReportType.DeliveredOrdersCustomer:
                    case ReportType.DeliveredOrdersSystemAdministrator:
                        rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Rekvisition finns";
                        rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.HasRequisition ? "Ja" : "Nej");
                        rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Reklamation finns";
                        rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.HasComplaint ? "Ja" : "Nej");
                        rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Preliminär kostnad (SEK)";
                        rowsWorksheet.Column(columnLetter.ToString()).Style.NumberFormat.Format = "#,##0.00";
                        rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.Price);
                        break;
                }
                if (rows.FirstOrDefault() is ReportRequisitionRowModel)
                {
                    CreateColumnsForRequisition(rowsWorksheet, (rows as IEnumerable<ReportRequisitionRowModel>).Select(r => r), ref columnLetter);
                }
                else if (rows.FirstOrDefault() is ReportComplaintRowModel)
                {
                    CreateColumnsForComplaint(rowsWorksheet, (rows as IEnumerable<ReportComplaintRowModel>).Select(r => r), ref columnLetter);
                }
                switch (reportType)
                {
                    case ReportType.OrdersForSystemAdministrator:
                    case ReportType.RequisitionsForSystemAdministrator:
                    case ReportType.ComplaintsForSystemAdministrator:
                    case ReportType.DeliveredOrdersSystemAdministrator:
                        CreateColumnsForSystemAdministrator(rowsWorksheet, rows, ref columnLetter);
                        break;
                    case ReportType.DeliveredOrdersBrokers:
                    case ReportType.RequestsForBrokers:
                    case ReportType.RequisitionsForBroker:
                    case ReportType.ComplaintsForBroker:
                        CreateColumnsForBroker(rowsWorksheet, rows, ref columnLetter, rows.FirstOrDefault() is ReportRequestRowModel);
                        break;
                    case ReportType.DeliveredOrdersCustomer:
                    case ReportType.OrdersForCustomer:
                    case ReportType.RequisitionsForCustomer:
                    case ReportType.ComplaintsForCustomer:
                        CreateColumnsForCustomer(rowsWorksheet, rows, ref columnLetter, rows.FirstOrDefault() is ReportOrderRowModel);
                        break;
                }

                rowsWorksheet.Row(1).Style.Font.Bold = true;
                rowsWorksheet.Columns().AdjustToContents();
                MemoryStream memoryStream = new MemoryStream();
                workbook.SaveAs(memoryStream);
                memoryStream.Flush();
                memoryStream.Position = 0;
                string fileName = $"{EnumHelper.GetDescription(reportType)}_{organisationName}_{_clock.SwedenNow.DateTime.ToString("yyyy-MM-dd HH:mm")}.xlsx";
                return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        private void CreateColumnsForCustomer(IXLWorksheet rowsWorksheet, IEnumerable<ReportRowModel> rows, ref char columnLetter, bool isOrder = false)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Förmedling";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.BrokerName);
            if (isOrder)
            {
                CreateColumnsForOrderCustomer(rowsWorksheet, (rows as IEnumerable<ReportOrderRowModel>).Select(r => r), ref columnLetter);
            }
        }

        private void CreateColumnsForOrderCustomer(IXLWorksheet rowsWorksheet, IEnumerable<ReportOrderRowModel> rows, ref char columnLetter)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Beställd av";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CreatedBy);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Ärendenummer";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.ReferenceNumber);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Enhet/Avdelning";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.UnitName);
        }

        private void CreateColumnsForBroker(IXLWorksheet rowsWorksheet, IEnumerable<ReportRowModel> rows, ref char columnLetter, bool isRequest = false)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Myndighet";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CustomerName);
            if (isRequest)
            {
                CreateColumnsForBrokerRequest(rowsWorksheet, (rows as IEnumerable<ReportRequestRowModel>).Select(r => r), ref columnLetter);
            }
        }
        private void CreateColumnsForBrokerRequest(IXLWorksheet rowsWorksheet, IEnumerable<ReportRequestRowModel> rows, ref char columnLetter)
        {
            //rows as 
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Besvarad av";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.AnsweredBy);
        }

        private void CreateColumnsForSystemAdministrator(IXLWorksheet rowsWorksheet, IEnumerable<ReportRowModel> rows, ref char columnLetter)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Myndighet";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CustomerName);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Förmedling";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.BrokerName);
        }

        private void CreateColumnsForRequisition(IXLWorksheet rowsWorksheet, IEnumerable<ReportRequisitionRowModel> rows, ref char columnLetter)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Måltidspauser finns";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.HasMealbreaks ? "Ja" : "Nej");
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Skapad av";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CreatedBy);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Spilltid normaltid (min)";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.WaisteTime);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Spilltid OB-tid (min)";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.WaisteTimeIWH);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Tolkens skattsedel";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.TaxCard);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Utlägg (SEK)";
            rowsWorksheet.Column(columnLetter.ToString()).Style.NumberFormat.Format = "#,##0.00";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.Outlay);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Bilersättning (km)";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CarCompensation);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Traktamente";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.PerDiem ?? string.Empty);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Total summa (SEK)";
            rowsWorksheet.Column(columnLetter.ToString()).Style.NumberFormat.Format = "#,##0.00";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.Price);
        }

        private void CreateColumnsForComplaint(IXLWorksheet rowsWorksheet, IEnumerable<ReportComplaintRowModel> rows, ref char columnLetter)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Skapad av";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CreatedBy);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Typ av reklamation";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.ComplaintType);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Reklamationsbeskrivning";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.ComplaintMessage);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Svar på reklamation";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.ComplaintAnswerMessage);
        }

        private static IEnumerable<ReportOrderRowModel> GetOrderExcelFileRows(List<Order> listItems, ReportType reportType)
        {
            return listItems
                    .Select(o => new ReportOrderRowModel
                    {
                        OrderNumber = o.OrderNumber,
                        ReportDate = (reportType == ReportType.DeliveredOrdersSystemAdministrator || reportType == ReportType.DeliveredOrdersCustomer) ? o.StartAt.ToString("yyyy-MM-dd HH:mm") : o.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        BrokerName = o.Requests.OrderBy(r => r.RequestId).Last().Ranking.Broker.Name,
                        Language = o.Language.Name,
                        Region = o.Region.Name,
                        AssignmentType = o.AssignentType.GetDescription(),
                        InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)o.Requests.OrderBy(r => r.RequestId).Last().CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        InterpreterId = o.Requests.OrderBy(r => r.RequestId).Last().Interpreter?.OfficialInterpreterId ?? string.Empty,
                        CreatedBy = o.CreatedByUser.FullName,
                        AssignmentDate = $"{o.StartAt.ToString("yyyy-MM-dd HH:mm")}-{o.EndAt.ToString("HH:mm")}",
                        Status = o.Status.GetDescription(),
                        ReferenceNumber = o.CustomerReferenceNumber ?? string.Empty,
                        UnitName = o.UnitName ?? string.Empty,
                        HasRequisition = o.Requests.OrderBy(r => r.RequestId).Last().Requisitions.Any(),
                        HasComplaint = o.Requests.OrderBy(r => r.RequestId).Last().Complaints.Any(),
                        CustomerName = o.CustomerOrganisation.Name,
                        Price = o.Requests.OrderBy(r => r.RequestId).Last().PriceRows != null ? o.Requests.OrderBy(r => r.RequestId).Last().PriceRows.Sum(p => p.TotalPrice) : 0,
                    }).ToList();
        }

        private static IEnumerable<ReportRequestRowModel> GetRequestExcelFileRows(List<Request> listItems, ReportType reportType)
        {
            return listItems
                    .Select(r => new ReportRequestRowModel
                    {
                        OrderNumber = r.Order.OrderNumber,
                        ReportDate = reportType == ReportType.DeliveredOrdersBrokers ? r.Order.StartAt.ToString("yyyy-MM-dd HH:mm") : r.Order.Requests.OrderBy(r1 => r1.RequestId).First(r1 => r.RankingId == r1.RankingId).CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        CustomerName = r.Order.CustomerOrganisation.Name,
                        Language = r.Order.Language.Name,
                        Region = r.Order.Region.Name,
                        AssignmentType = r.Order.AssignentType.GetDescription(),
                        InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        InterpreterId = r.Interpreter?.OfficialInterpreterId ?? string.Empty,
                        AnsweredBy = r.AnsweringUser?.FullName ?? string.Empty,
                        AssignmentDate = $"{r.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}-{r.Order.EndAt.ToString("HH:mm")}",
                        Status = r.Status.GetDescription(),
                        HasRequisition = r.Requisitions.Any(),
                        HasComplaint = r.Complaints.Any(),
                        Price = r.PriceRows != null ?  r.PriceRows.Sum(p => p.TotalPrice) : 0,
                    }).ToList();
        }

        private static IEnumerable<ReportRequisitionRowModel> GetRequisitionsExcelFileRows(List<Requisition> listItems)
        {
            return listItems
                    .Select(r => new ReportRequisitionRowModel
                    {
                        OrderNumber = r.Request.Order.OrderNumber,
                        ReportDate = r.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        BrokerName = r.Request.Ranking.Broker.Name,
                        Language = r.Request.Order.Language.Name,
                        Region = r.Request.Order.Region.Name,
                        InterpreterId = r.Request.Interpreter?.OfficialInterpreterId ?? string.Empty,
                        CreatedBy = r.Status == RequisitionStatus.AutomaticGeneratedFromCancelledOrder ? "Systemet" : r.CreatedByUser.FullName,
                        AssignmentDate = $"{r.Request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}-{r.Request.Order.EndAt.ToString("HH:mm")}",
                        Status = r.Status.GetDescription(),
                        CustomerName = r.Request.Order.CustomerOrganisation.Name,
                        AssignmentType = r.Request.Order.AssignentType.GetDescription(),
                        InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)r.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        HasMealbreaks = r.MealBreaks.Any(),
                        WaisteTime = r.TimeWasteNormalTime ?? 0,
                        WaisteTimeIWH = r.TimeWasteIWHTime ?? 0,
                        Outlay = r.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.Outlay)?.Price ?? 0,
                        CarCompensation = r.CarCompensation ?? 0,
                        PerDiem = r.PerDiem,
                        Price = r.PriceRows.Sum(p => p.TotalPrice),
                        TaxCard = r.InterpretersTaxCard == null ? string.Empty : r.InterpretersTaxCard.Value.GetDescription()
                    }).ToList();
        }

        private static IEnumerable<ReportComplaintRowModel> GetComplaintsExcelFileRows(List<Complaint> listItems)
        {
            return listItems
                    .Select(c => new ReportComplaintRowModel
                    {
                        OrderNumber = c.Request.Order.OrderNumber,
                        ReportDate = c.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        BrokerName = c.Request.Ranking.Broker.Name,
                        Language = c.Request.Order.Language.Name,
                        Region = c.Request.Order.Region.Name,
                        InterpreterId = c.Request.Interpreter?.OfficialInterpreterId ?? string.Empty,
                        CreatedBy = c.CreatedByUser.FullName,
                        AssignmentDate = $"{c.Request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}-{c.Request.Order.EndAt.ToString("HH:mm")}",
                        Status = c.Status.GetDescription(),
                        CustomerName = c.Request.Order.CustomerOrganisation.Name,
                        AssignmentType = c.Request.Order.AssignentType.GetDescription(),
                        InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)c.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        HasRequisition = c.Request.Requisitions.Any(),
                        ComplaintMessage = c.ComplaintMessage,
                        ComplaintAnswerMessage = c.AnswerMessage ?? string.Empty,
                        ComplaintType = c.ComplaintType.GetDescription(),

                    }).ToList();
        }

        private static string GetColumnName(char columnLetter, int index)
        {
            return $"{columnLetter}{index}";
        }

        [Authorize(Roles = Roles.SuperUser)]
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
