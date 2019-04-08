using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using System.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Tolk.BusinessLogic.Utilities;
using ClosedXML.Excel;

namespace Tolk.BusinessLogic.Services
{
    public class StatisticsService
    {

        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;

        public StatisticsService(
            TolkDbContext dbContext,
            ISwedishClock clock
            )
        {
            _dbContext = dbContext;
            _clock = clock;
        }

        #region Statistics dashboard

        public WeeklyStatisticsModel GetWeeklyOrderStatistics()
        {
            int lastWeek = GetOrders(StartDate, BreakDate);
            int thisWeek = GetOrders(BreakDate, _clock.SwedenNow);
            return GetWeeklyStatistics(lastWeek, thisWeek, "Bokningar");
        }

        public WeeklyStatisticsModel GetWeeklyDeliveredOrderStatistics()
        {
            int lastWeek = GetDeliveredOrders(StartDate, BreakDate);
            int thisWeek = GetDeliveredOrders(BreakDate, _clock.SwedenNow);
            return GetWeeklyStatistics(lastWeek, thisWeek, "Utförda uppdrag");
        }

        public WeeklyStatisticsModel GetWeeklyRequisitionStatistics()
        {
            int lastWeek = GetRequisitions(StartDate, BreakDate);
            int thisWeek = GetRequisitions(BreakDate, _clock.SwedenNow);
            return GetWeeklyStatistics(lastWeek, thisWeek, "Rekvisitioner");
        }

        public WeeklyStatisticsModel GetWeeklyComplaintStatistics()
        {
            int lastWeek = GetComplaints(StartDate, BreakDate);
            int thisWeek = GetComplaints(BreakDate, _clock.SwedenNow);
            return GetWeeklyStatistics(lastWeek, thisWeek, "Reklamationer");
        }

        public WeeklyStatisticsModel GetWeeklyLoggedOnUsers()
        {
            int lastWeek = GetLoggedOnUsers(StartDate, BreakDate);
            int thisWeek = GetLoggedOnUsers(BreakDate, _clock.SwedenNow);
            return GetWeeklyStatistics(lastWeek, thisWeek, "Inloggade anv.");
        }

        public WeeklyStatisticsModel GetWeeklyNewUsers()
        {
            int lastWeek = GetNewUsers(StartDate, BreakDate);
            int thisWeek = GetNewUsers(BreakDate, _clock.SwedenNow);
            return GetWeeklyStatistics(lastWeek, thisWeek, "Nya användare");
        }

        private DateTimeOffset StartDate { get => _clock.SwedenNow.AddDays(-14); }

        private DateTimeOffset BreakDate { get => _clock.SwedenNow.AddDays(-7); }

        private int GetOrders(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Orders.Where(o => o.CreatedAt >= start && o.CreatedAt < end).Count();
        }

        private int GetDeliveredOrders(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Orders.Where(o => o.EndAt >= start && o.EndAt < end
                    && (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.DeliveryAccepted || o.Status == OrderStatus.ResponseAccepted)).Count();
        }

        private int GetRequisitions(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Requisitions.Where(r => r.CreatedAt >= start && r.CreatedAt < end
                    && !r.ReplacedByRequisitionId.HasValue).Count();
        }

        private int GetComplaints(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Complaints.Where(c => c.CreatedAt >= start && c.CreatedAt < end).Count();
        }

        private int GetLoggedOnUsers(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.UserLoginLogEntries.Where(u => u.LoggedInAt >= start && u.LoggedInAt < end).Select(u => u.UserId).Distinct().Count();
        }

        private int GetNewUsers(DateTimeOffset start, DateTimeOffset end)
        {
            var userLoggedOnBeforeWeekStarted = _dbContext.UserLoginLogEntries.Where(u => u.LoggedInAt < start).Select(u => u.UserId).Distinct();
            return _dbContext.UserLoginLogEntries.Where(u => u.LoggedInAt >= start && u.LoggedInAt < end && !userLoggedOnBeforeWeekStarted.Contains(u.UserId)).Select(u => u.UserId).Distinct().Count();
        }

        private WeeklyStatisticsModel GetWeeklyStatistics(int lastWeek, int thisWeek, string name)
        {
            decimal diff = (lastWeek == 0 || thisWeek == 0) ? 0 : (Convert.ToDecimal(thisWeek) - Convert.ToDecimal(lastWeek)) * 100/ lastWeek;
            return new WeeklyStatisticsModel
            {
                NoOfItems = thisWeek,
                DiffPercentage = Math.Round(Math.Abs(diff), 2),
                ChangeType = diff == 0 ? lastWeek == thisWeek ? StatisticsChangeType.Unchanged : StatisticsChangeType.NotApplicable : thisWeek > lastWeek ? StatisticsChangeType.Increasing : StatisticsChangeType.Decreasing,
                Name = name
            };
        }

        #endregion

        #region Broker reports

        public IEnumerable<Request> GetRequestsForBroker(DateTimeOffset start, DateTimeOffset end, int brokerId)
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
                      .Where(r => r.Ranking.BrokerId == brokerId && r.CreatedAt.Date >= start.Date && r.CreatedAt.Date <= end.Date
                          && !r.StatusNotToBeDisplayedForBroker);
        }

        public IEnumerable<Request> GetDeliveredRequestsForBroker(DateTimeOffset start, DateTimeOffset end, int brokerId)
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
                    .Where(r => r.Ranking.BrokerId == brokerId && !r.StatusNotToBeDisplayedForBroker
                        && r.Order.EndAt <= _clock.SwedenNow && r.Order.StartAt.Date >= start.Date && r.Order.StartAt.Date <= end.Date
                        && (r.Order.Status == OrderStatus.Delivered || r.Order.Status == OrderStatus.DeliveryAccepted || r.Order.Status == OrderStatus.ResponseAccepted));
        }

        public IEnumerable<Requisition> GetRequisitionsForBroker(DateTimeOffset start, DateTimeOffset end, int brokerId)
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
                        && r.Request.Ranking.BrokerId == brokerId && r.ReplacedByRequisitionId == null);
        }

        public IEnumerable<Complaint> GetComplaintsForBroker(DateTimeOffset start, DateTimeOffset end, int brokerId)
        {
            return _dbContext.Complaints
                    .Include(c => c.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                    .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Region)
                    .Include(c => c.Request).ThenInclude(r => r.Interpreter)
                    .Include(c => c.Request).ThenInclude(r => r.Requisitions)
                    .Include(c => c.AnsweringUser)
                    .OrderBy(c => c.Request.Order.OrderNumber)
                    .Where(c => c.CreatedAt.Date >= start.Date && c.CreatedAt.Date <= end.Date && c.Request.Ranking.BrokerId == brokerId);
        }

        #endregion

        #region Customer and SysAdmin reports

        public IEnumerable<Order> GetOrders(DateTimeOffset start, DateTimeOffset end, int? organisationId)
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
                    .Where(o => o.CreatedAt.Date >= start.Date && o.CreatedAt.Date <= end.Date
                        && (organisationId.HasValue ? o.CustomerOrganisationId == organisationId : !organisationId.HasValue));
        }

        public IEnumerable<Order> GetDeliveredOrders(DateTimeOffset start, DateTimeOffset end, int? organisationId)
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
                        && (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.DeliveryAccepted || o.Status == OrderStatus.ResponseAccepted)
                        && (organisationId.HasValue ? o.CustomerOrganisationId == organisationId : !organisationId.HasValue));
        }

        public IEnumerable<Requisition> GetRequisitionsForCustomerAndSysAdmin(DateTimeOffset start, DateTimeOffset end, int? organisationId)
        {
            return _dbContext.Requisitions
                    .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                    .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Region)
                    .Include(r => r.Request).ThenInclude(r => r.Interpreter)
                    .Include(r => r.MealBreaks)
                    .Include(r => r.ProcessedUser)
                    .Include(r => r.CreatedByUser)
                    .Include(r => r.PriceRows)
                    .OrderBy(r => r.Request.Order.OrderNumber)
                    .Where(r => r.CreatedAt.Date >= start.Date && r.CreatedAt.Date <= end.Date && r.ReplacedByRequisitionId == null
                        && (organisationId.HasValue ? r.Request.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue));
        }

        public IEnumerable<Complaint> GetComplaintsForCustomerAndSysAdmin(DateTimeOffset start, DateTimeOffset end, int? organisationId)
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
                    .Where(c => c.CreatedAt.Date >= start.Date && c.CreatedAt.Date <= end.Date
                        && (organisationId.HasValue ? c.Request.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue));
        }


        #endregion

        #region Generate Excel

        public MemoryStream CreateExcelFile(IEnumerable<ReportRow> rows, string organisationName, ReportType reportType)
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
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Myndighetens ärendenummer";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.ReferenceNumber);
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
                if (rows.FirstOrDefault() is ReportRequisitionRow)
                {
                    CreateColumnsForRequisition(rowsWorksheet, (rows as IEnumerable<ReportRequisitionRow>).Select(r => r), ref columnLetter, reportType);
                }
                else if (rows.FirstOrDefault() is ReportRequisitionRow)
                {
                    CreateColumnsForComplaint(rowsWorksheet, (rows as IEnumerable<ReportComplaintRow>).Select(r => r), ref columnLetter, reportType);
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
                        CreateColumnsForBroker(rowsWorksheet, rows, ref columnLetter, !(rows.FirstOrDefault() is ReportRequisitionRow || rows.FirstOrDefault() is ReportComplaintRow));
                        break;
                    case ReportType.DeliveredOrdersCustomer:
                    case ReportType.OrdersForCustomer:
                    case ReportType.RequisitionsForCustomer:
                    case ReportType.ComplaintsForCustomer:
                        CreateColumnsForCustomer(rowsWorksheet, rows, ref columnLetter, rows.FirstOrDefault() is ReportOrderRow);
                        break;
                }
                rowsWorksheet.Row(1).Style.Font.Bold = true;
                rowsWorksheet.Columns().AdjustToContents();
                MemoryStream memoryStream = new MemoryStream();
                workbook.SaveAs(memoryStream);
                memoryStream.Flush();
                memoryStream.Position = 0;
                return memoryStream;
            }
        }

        private void CreateColumnsForCustomer(IXLWorksheet rowsWorksheet, IEnumerable<ReportRow> rows, ref char columnLetter, bool isOrder = false)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Förmedling";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.BrokerName);
            if (isOrder)
            {
                CreateColumnsForOrderCustomer(rowsWorksheet, (rows as IEnumerable<ReportOrderRow>).Select(r => r), ref columnLetter);
            }
        }

        private void CreateColumnsForOrderCustomer(IXLWorksheet rowsWorksheet, IEnumerable<ReportOrderRow> rows, ref char columnLetter)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Beställd av";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.ReportPersonToDisplay);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Enhet/Avdelning";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.UnitName);
        }

        private void CreateColumnsForBroker(IXLWorksheet rowsWorksheet, IEnumerable<ReportRow> rows, ref char columnLetter, bool isRequest = false)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Myndighet";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CustomerName);
            if (isRequest)
            {
                CreateColumnsForBrokerRequest(rowsWorksheet, rows, ref columnLetter);
            }
        }
        private void CreateColumnsForBrokerRequest(IXLWorksheet rowsWorksheet, IEnumerable<ReportRow> rows, ref char columnLetter)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Besvarad av";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.ReportPersonToDisplay);
        }

        private void CreateColumnsForSystemAdministrator(IXLWorksheet rowsWorksheet, IEnumerable<ReportRow> rows, ref char columnLetter)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Myndighet";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CustomerName);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Förmedling";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.BrokerName);
        }

        private void CreateColumnsForRequisition(IXLWorksheet rowsWorksheet, IEnumerable<ReportRequisitionRow> rows, ref char columnLetter, ReportType reportType)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Måltidspauser finns";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.HasMealbreaks ? "Ja" : "Nej");
            switch (reportType)
            {
                case ReportType.RequisitionsForBroker:
                    rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Skapad av";
                    rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.ReportPersonToDisplay);
                    break;
                case ReportType.RequisitionsForCustomer:
                    rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Granskad/kommenterad av";
                    rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.ReportPersonToDisplay ?? "");
                    break;
            }
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

        private void CreateColumnsForComplaint(IXLWorksheet rowsWorksheet, IEnumerable<ReportComplaintRow> rows, ref char columnLetter, ReportType reportType)
        {
            switch (reportType)
            {
                case ReportType.ComplaintsForBroker:
                    rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Besvarad av";
                    rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.ReportPersonToDisplay ?? "");
                    break;
                case ReportType.ComplaintsForCustomer:
                    rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Skapad av";
                    rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.ReportPersonToDisplay);
                    break;
            }
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Typ av reklamation";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.ComplaintType);
        }

        private static string GetColumnName(char columnLetter, int index)
        {
            return $"{columnLetter}{index}";
        }

        public static IEnumerable<ReportOrderRow> GetOrderExcelFileRows(IEnumerable<Order> listItems, ReportType reportType)
        {
            return listItems
                    .Select(o => new ReportOrderRow
                    {
                        OrderNumber = o.OrderNumber,
                        ReportDate = (reportType == ReportType.DeliveredOrdersSystemAdministrator || reportType == ReportType.DeliveredOrdersCustomer) ? o.StartAt.ToString("yyyy-MM-dd HH:mm") : o.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        BrokerName = o.Requests.OrderBy(r => r.RequestId).Last().Ranking.Broker.Name,
                        Language = o.Language.Name,
                        Region = o.Region.Name,
                        AssignmentType = o.AssignentType.GetDescription(),
                        InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)o.Requests.OrderBy(r => r.RequestId).Last().CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        InterpreterId = o.Requests.OrderBy(r => r.RequestId).Last().Interpreter?.OfficialInterpreterId ?? string.Empty,
                        ReportPersonToDisplay = o.CreatedByUser.FullName,
                        AssignmentDate = $"{o.StartAt.ToString("yyyy-MM-dd HH:mm")}-{o.EndAt.ToString("HH:mm")}",
                        Status = o.Status.GetDescription(),
                        ReferenceNumber = o.CustomerReferenceNumber ?? string.Empty,
                        UnitName = o.UnitName ?? string.Empty,
                        HasRequisition = o.Requests.OrderBy(r => r.RequestId).Last().Requisitions.Any(),
                        HasComplaint = o.Requests.OrderBy(r => r.RequestId).Last().Complaints.Any(),
                        CustomerName = o.CustomerOrganisation.Name,
                        Price = o.Requests.OrderBy(r => r.RequestId).Last().PriceRows != null ? o.Requests.OrderBy(r => r.RequestId).Last().PriceRows.Sum(p => p.TotalPrice) : 0,
                    });
        }

        public static IEnumerable<ReportRow> GetRequestExcelFileRows(IEnumerable<Request> listItems, ReportType reportType)
        {
            return listItems
                    .Select(r => new ReportRow
                    {
                        OrderNumber = r.Order.OrderNumber,
                        ReportDate = reportType == ReportType.DeliveredOrdersBrokers ? r.Order.StartAt.ToString("yyyy-MM-dd HH:mm") : r.Order.Requests.OrderBy(r1 => r1.RequestId).First(r1 => r.RankingId == r1.RankingId).CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        CustomerName = r.Order.CustomerOrganisation.Name,
                        Language = r.Order.Language.Name,
                        Region = r.Order.Region.Name,
                        AssignmentType = r.Order.AssignentType.GetDescription(),
                        InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        InterpreterId = r.Interpreter?.OfficialInterpreterId ?? string.Empty,
                        ReportPersonToDisplay = r.AnsweringUser?.FullName ?? string.Empty,
                        AssignmentDate = $"{r.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}-{r.Order.EndAt.ToString("HH:mm")}",
                        Status = r.Status.GetDescription(),
                        ReferenceNumber = r.Order.CustomerReferenceNumber ?? string.Empty,
                        HasRequisition = r.Requisitions.Any(),
                        HasComplaint = r.Complaints.Any(),
                        Price = r.PriceRows != null ? r.PriceRows.Sum(p => p.TotalPrice) : 0,
                    });
        }

        public static IEnumerable<ReportRequisitionRow> GetRequisitionsExcelFileRows(IEnumerable<Requisition> listItems, ReportType reportType)
        {
            return listItems
                    .Select(r => new ReportRequisitionRow
                    {
                        OrderNumber = r.Request.Order.OrderNumber,
                        ReportDate = r.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        BrokerName = r.Request.Ranking.Broker.Name,
                        Language = r.Request.Order.Language.Name,
                        Region = r.Request.Order.Region.Name,
                        InterpreterId = r.Request.Interpreter?.OfficialInterpreterId ?? string.Empty,
                        ReportPersonToDisplay = r.Status == RequisitionStatus.AutomaticGeneratedFromCancelledOrder ? "Systemet" : reportType == ReportType.RequisitionsForCustomer ? r.ProcessedUser?.FullName : r.CreatedByUser?.FullName,
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
                        TaxCard = r.InterpretersTaxCard == null ? string.Empty : r.InterpretersTaxCard.Value.GetDescription(),
                        ReferenceNumber = r.Request.Order.CustomerReferenceNumber ?? string.Empty,
                    });
        }

        public static IEnumerable<ReportComplaintRow> GetComplaintsExcelFileRows(IEnumerable<Complaint> listItems, ReportType reportType)
        {
            return listItems
                    .Select(c => new ReportComplaintRow
                    {
                        OrderNumber = c.Request.Order.OrderNumber,
                        ReportDate = c.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        BrokerName = c.Request.Ranking.Broker.Name,
                        Language = c.Request.Order.Language.Name,
                        Region = c.Request.Order.Region.Name,
                        InterpreterId = c.Request.Interpreter?.OfficialInterpreterId ?? string.Empty,
                        ReportPersonToDisplay = reportType == ReportType.ComplaintsForCustomer ? c.CreatedByUser.FullName : c.AnsweringUser?.FullName,
                        AssignmentDate = $"{c.Request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}-{c.Request.Order.EndAt.ToString("HH:mm")}",
                        Status = c.Status.GetDescription(),
                        CustomerName = c.Request.Order.CustomerOrganisation.Name,
                        AssignmentType = c.Request.Order.AssignentType.GetDescription(),
                        InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)c.Request.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        HasRequisition = c.Request.Requisitions.Any(),
                        ComplaintType = c.ComplaintType.GetDescription(),
                        ReferenceNumber = c.Request.Order.CustomerReferenceNumber ?? string.Empty,
                    });
        }

        #endregion
    }
}
