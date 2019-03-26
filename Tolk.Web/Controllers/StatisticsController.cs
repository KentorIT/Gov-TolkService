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


namespace Tolk.Web.Controllers
{

    [Authorize(Roles = Roles.AdminRoles)]
    public class StatisticsController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger<StatisticsController> _logger;

        public StatisticsController(
            TolkDbContext dbContext,
            ILogger<StatisticsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
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
                    case ReportType.Orders:
                        model.ReportItems = _dbContext.Orders.Where(o => o.CustomerOrganisationId == User.GetCustomerOrganisationId() && o.CreatedAt.Date >= start.Date && o.CreatedAt.Date <= end.Date).Count();
                        break;
                        //todo Swedish clock
                    case ReportType.DeliveredOrders:
                        model.ReportItems = _dbContext.Orders.Where(o => o.CustomerOrganisationId == User.GetCustomerOrganisationId() 
                        && o.EndAt <= DateTime.Now && o.StartAt.Date >= start.Date && o.StartAt.Date <= end.Date
                        && (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.DeliveryAccepted || o.Status == OrderStatus.ResponseAccepted)).Count();
                        break;
                    default:
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
                case ReportType.Orders:
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
                    return CreateCustomerOrderExcelFile(GetOrderExcelFileRows(orders), orders.First().CustomerOrganisation.Name, model.ReportType);
                case ReportType.DeliveredOrders:
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
                        && o.EndAt <= DateTime.Now && o.StartAt.Date >= start.Date && o.StartAt.Date <= end.Date
                        && (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.DeliveryAccepted || o.Status == OrderStatus.ResponseAccepted)).ToList();
                    return CreateCustomerOrderExcelFile(GetOrderExcelFileRows(deliveredOrders), deliveredOrders.First().CustomerOrganisation.Name, model.ReportType);
            }
            return RedirectToAction(nameof(List));
        }

        private ActionResult CreateCustomerOrderExcelFile(IEnumerable<ExcelReportRowModel> rows, string customerName, ReportType reportType)
        {
            using (var workbook = new XLWorkbook())
            {
                var rowsWorksheet = workbook.Worksheets.Add(EnumHelper.GetDescription(reportType));
                char columnLetter = 'A';
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "BokningsId";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.OrderNumber);
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Beställningsdatum";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.ReportDate);
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Förmedling";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.BrokerName);
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Språk";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.Language);
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Län";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.Region);
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Uppdragstyp";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.AssignmentType);
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Tolks kompetensnivå";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.InterpreterCompetenceLevel.GetDescription());
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Tolk-ID";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.InterpreterId);
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Beställd av";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CreatedBy);
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Tid för uppdrag";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.AssignmentDate.ToString());
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Ärendenummer";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.ReferenceNumber);
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Enhet/Avdelning";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.UnitName);
                switch (reportType)
                {
                    case ReportType.Orders:
                        rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Status";
                        rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.Status.ToString());
                        break;
                    case ReportType.DeliveredOrders:
                        rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Rekvisition finns";
                        rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.HasRequisition ? "Ja" : "Nej");
                        rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Reklamation finns";
                        rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.HasComplaint ? "Ja" : "Nej");
                        break;
                }
                        rowsWorksheet.Row(1).Style.Font.Bold = true;
                MemoryStream memoryStream = new MemoryStream();
                workbook.SaveAs(memoryStream);
                memoryStream.Flush();
                memoryStream.Position = 0;
                string fileName = $"{EnumHelper.GetDescription(reportType)}_{customerName}_{DateTime.Now.ToShortDateString()}.xlsx";
                return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        private static IEnumerable<ExcelReportRowModel> GetOrderExcelFileRows(List<Order> listItems)
        {
            return listItems
                    .Select(o => new ExcelReportRowModel
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
                        ReferenceNumber = o.CustomerReferenceNumber?? string.Empty,
                        UnitName = o.UnitName?? string.Empty,
                        HasRequisition = o.Requests.OrderBy(r => r.RequestId).Last().Requisitions.Any(),
                        HasComplaint = o.Requests.OrderBy(r => r.RequestId).Last().Complaints.Any(),

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
