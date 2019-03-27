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
                        model.ReportItems = _dbContext.Orders.Where(o => o.CustomerOrganisationId == User.GetCustomerOrganisationId() && o.CreatedAt.Date >= start.Date && o.CreatedAt.Date <= end.Date).Count();
                        break;
                    case ReportType.DeliveredOrdersCustomer:
                        model.ReportItems = _dbContext.Orders.Where(o => o.CustomerOrganisationId == User.GetCustomerOrganisationId()
                        && o.EndAt <= _clock.SwedenNow && o.StartAt.Date >= start.Date && o.StartAt.Date <= end.Date
                        && (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.DeliveryAccepted || o.Status == OrderStatus.ResponseAccepted)).Count();
                        break;
                    default:
                    case ReportType.RequestsForBrokers:
                        model.ReportItems = _dbContext.Requests.Where(r => r.Ranking.BrokerId == User.GetBrokerId() && r.CreatedAt.Date >= start.Date && r.CreatedAt.Date <= end.Date
                        && !(r.Status == RequestStatus.NoDeadlineFromCustomer || r.Status == RequestStatus.AwaitingDeadlineFromCustomer)).Count();
                        break;
                    case ReportType.DeliveredOrdersBrokers:
                        model.ReportItems = _dbContext.Requests.Where(r => r.Ranking.BrokerId == User.GetBrokerId()
                        && r.Order.EndAt <= _clock.SwedenNow && r.Order.StartAt.Date >= start.Date && r.Order.StartAt.Date <= end.Date
                        && (r.Order.Status == OrderStatus.Delivered || r.Order.Status == OrderStatus.DeliveryAccepted || r.Order.Status == OrderStatus.ResponseAccepted)).Count();
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
            switch (model.ReportType)
            {
                case ReportType.OrdersForCustomer:
                    var orders = _dbContext.Orders
                    .Include(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(o => o.Requests).ThenInclude(r => r.Interpreter)
                    .Include(o => o.Requests).ThenInclude(r => r.Requisitions)
                    .Include(o => o.Requests).ThenInclude(r => r.Complaints)
                    .Include(o => o.CustomerOrganisation)
                    .Include(o => o.Language)
                    .Include(o => o.Region)
                    .Include(o => o.CreatedByUser)
                    .Where(o => o.CustomerOrganisationId == User.GetCustomerOrganisationId() && o.CreatedAt.Date >= start.Date && o.CreatedAt.Date <= end.Date).ToList();
                    return CreateExcelFile(GetOrderExcelFileRows(orders), orders.First().CustomerOrganisation.Name, model.ReportType);
                case ReportType.DeliveredOrdersCustomer:
                    var deliveredOrders = _dbContext.Orders
                    .Include(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(o => o.Requests).ThenInclude(r => r.Interpreter)
                    .Include(o => o.Requests).ThenInclude(r => r.Requisitions)
                    .Include(o => o.Requests).ThenInclude(r => r.Complaints)
                    .Include(o => o.CustomerOrganisation)
                    .Include(o => o.Language)
                    .Include(o => o.Region)
                    .Include(o => o.CreatedByUser)
                    .Where(o => o.CustomerOrganisationId == User.GetCustomerOrganisationId()
                        && o.EndAt <= _clock.SwedenNow && o.StartAt.Date >= start.Date && o.StartAt.Date <= end.Date
                        && (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.DeliveryAccepted || o.Status == OrderStatus.ResponseAccepted)).ToList();
                    return CreateExcelFile(GetOrderExcelFileRows(deliveredOrders), deliveredOrders.First().CustomerOrganisation.Name, model.ReportType);
                case ReportType.DeliveredOrdersBrokers:
                    var deliveredOrdersBrokers = _dbContext.Requests
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.AnsweringUser)
                    .Include(r => r.Interpreter)
                    .Include(r => r.Requisitions)
                    .Include(r => r.Complaints)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Order).ThenInclude(o => o.Language)
                    .Include(r => r.Order).ThenInclude(o => o.Region)
                    .Where(r => r.Ranking.BrokerId == User.GetBrokerId()
                        && r.Order.EndAt <= _clock.SwedenNow && r.Order.StartAt.Date >= start.Date && r.Order.StartAt.Date <= end.Date
                        && (r.Order.Status == OrderStatus.Delivered || r.Order.Status == OrderStatus.DeliveryAccepted || r.Order.Status == OrderStatus.ResponseAccepted)).ToList();
                    return CreateExcelFile(GetRequestExcelFileRows(deliveredOrdersBrokers), deliveredOrdersBrokers.First().Ranking.Broker.Name, model.ReportType);
                case ReportType.RequestsForBrokers:
                    var requestsForBrokers = _dbContext.Requests
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.AnsweringUser)
                    .Include(r => r.Interpreter)
                    .Include(r => r.Requisitions)
                    .Include(r => r.Complaints)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Order).ThenInclude(o => o.Language)
                    .Include(r => r.Order).ThenInclude(o => o.Region)
                    .Where(r => r.Ranking.BrokerId == User.GetBrokerId() && r.CreatedAt.Date >= start.Date && r.CreatedAt.Date <= end.Date
                        && !(r.Status == RequestStatus.NoDeadlineFromCustomer || r.Status == RequestStatus.AwaitingDeadlineFromCustomer)).ToList();
                    return CreateExcelFile(GetRequestExcelFileRows(requestsForBrokers), requestsForBrokers.First().Ranking.Broker.Name, model.ReportType);
            }
            return RedirectToAction(nameof(List));
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
                        rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Status";
                        rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.Status.ToString());
                        break;
                    case ReportType.DeliveredOrdersBrokers:
                    case ReportType.DeliveredOrdersCustomer:
                        rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Rekvisition finns";
                        rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.HasRequisition ? "Ja" : "Nej");
                        rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Reklamation finns";
                        rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.HasComplaint ? "Ja" : "Nej");
                        break;
                }
                if (rows.FirstOrDefault() is ReportRequestRowModel)
                {
                    CreateRowsForBroker(rowsWorksheet, (rows as IEnumerable<ReportRequestRowModel>).Select(r => r), columnLetter);
                }
                else if (rows.FirstOrDefault() is ReportOrderRowModel)
                {
                    CreateRowsForCustomer(rowsWorksheet, (rows as IEnumerable<ReportOrderRowModel>).Select(r => r), columnLetter);
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

        private void CreateRowsForBroker(IXLWorksheet rowsWorksheet, IEnumerable<ReportRequestRowModel> rows, char columnLetter)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Myndighet";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CustomerName);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Besvarad av";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.AnsweredBy);
        }

        private void CreateRowsForCustomer(IXLWorksheet rowsWorksheet, IEnumerable<ReportOrderRowModel> rows, char columnLetter)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Förmedling";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.BrokerName);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Beställd av";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CreatedBy);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Ärendenummer";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.ReferenceNumber);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Enhet/Avdelning";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.UnitName);
        }

        private static IEnumerable<ReportOrderRowModel> GetOrderExcelFileRows(List<Order> listItems)
        {
            return listItems
                    .Select(o => new ReportOrderRowModel
                    {
                        OrderNumber = o.OrderNumber,
                        ReportDate = o.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
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
                    }).ToList();
        }

        private static IEnumerable<ReportRequestRowModel> GetRequestExcelFileRows(List<Request> listItems)
        {
            return listItems
                    .Select(r => new ReportRequestRowModel
                    {
                        OrderNumber = r.Order.OrderNumber,
                        ReportDate = r.Order.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
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
