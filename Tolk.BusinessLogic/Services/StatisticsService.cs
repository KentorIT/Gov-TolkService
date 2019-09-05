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

        #region Dashboard Weekly Statistics

        public IEnumerable<WeeklyStatisticsModel> GetWeeklyStatistics()
        {
            DateTimeOffset breakDate = BreakDate;
            yield return GetWeeklyOrderStatistics(BreakDate);
            yield return GetWeeklyDeliveredOrderStatistics(BreakDate);
            yield return GetWeeklyRequisitionStatistics(BreakDate);
            yield return GetWeeklyComplaintStatistics(BreakDate);
            yield return GetWeeklyUserLogins(BreakDate);
            yield return GetWeeklyNewUsers(BreakDate);
        }

        public WeeklyStatisticsModel GetWeeklyOrderStatistics(DateTimeOffset breakDate)
        {
            var orders = GetOrders(StartDate, _clock.SwedenNow);
            return GetWeeklyStatistics(orders.Where(o => o.CreatedAt < breakDate).Count(), orders.Where(o => o.CreatedAt >= breakDate).Count(), "Bokningar");
        }

        public WeeklyStatisticsModel GetWeeklyDeliveredOrderStatistics(DateTimeOffset breakDate)
        {
            var deliveredOrders = GetDeliveredOrders(StartDate, _clock.SwedenNow);
            return GetWeeklyStatistics(deliveredOrders.Where(o => o.EndAt < breakDate).Count(), deliveredOrders.Where(o => o.EndAt >= breakDate).Count(), "Utförda uppdrag");
        }

        public WeeklyStatisticsModel GetWeeklyRequisitionStatistics(DateTimeOffset breakDate)
        {
            var requisitions = GetRequisitions(StartDate, _clock.SwedenNow);
            return GetWeeklyStatistics(requisitions.Where(r => r.CreatedAt < breakDate).Count(), requisitions.Where(r => r.CreatedAt >= breakDate).Count(), "Rekvisitioner");
        }

        public WeeklyStatisticsModel GetWeeklyComplaintStatistics(DateTimeOffset breakDate)
        {
            var complaints = GetComplaints(StartDate, _clock.SwedenNow);
            return GetWeeklyStatistics(complaints.Where(c => c.CreatedAt < breakDate).Count(), complaints.Where(c => c.CreatedAt >= breakDate).Count(), "Reklamationer");
        }

        public WeeklyStatisticsModel GetWeeklyUserLogins(DateTimeOffset breakDate)
        {
            var userLogins = GetUserLogins(StartDate, _clock.SwedenNow);
            int lastWeek = userLogins.Where(u => u.LoggedInAt < breakDate).Select(u => u.UserId).Distinct().Count();
            int thisWeek = userLogins.Where(u => u.LoggedInAt >= breakDate).Select(u => u.UserId).Distinct().Count();
            return GetWeeklyStatistics(lastWeek, thisWeek, "Inloggade anv.");
        }

        public WeeklyStatisticsModel GetWeeklyNewUsers(DateTimeOffset breakDate)
        {
            var newUsers = GetNewUsers(StartDate, _clock.SwedenNow);
            int lastWeek = newUsers.Where(u => u.LoggedAt < breakDate).Select(u => u.UserId).Distinct().Count();
            int thisWeek = newUsers.Where(u => u.LoggedAt >= breakDate).Select(u => u.UserId).Distinct().Count();
            return GetWeeklyStatistics(lastWeek, thisWeek, "Nya användare");
        }

        private DateTimeOffset StartDate => _clock.SwedenNow.AddDays(-14);

        private DateTimeOffset BreakDate => _clock.SwedenNow.AddDays(-7);

        private List<Order> GetOrders(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Orders.Where(o => o.CreatedAt >= start && o.CreatedAt < end).ToList();
        }

        private List<Order> GetDeliveredOrders(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Orders.Where(o => o.EndAt >= start && o.EndAt < end
                    && (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.DeliveryAccepted || o.Status == OrderStatus.ResponseAccepted)).ToList();
        }

        private List<Requisition> GetRequisitions(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Requisitions.Where(r => r.CreatedAt >= start && r.CreatedAt < end
                    && !r.ReplacedByRequisitionId.HasValue).ToList();
        }

        private List<Complaint> GetComplaints(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.Complaints.Where(c => c.CreatedAt >= start && c.CreatedAt < end).ToList();
        }

        private List<UserLoginLogEntry> GetUserLogins(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.UserLoginLogEntries.Where(u => u.LoggedInAt >= start
                && u.LoggedInAt < end).ToList();
        }

        private List<UserAuditLogEntry> GetNewUsers(DateTimeOffset start, DateTimeOffset end)
        {
            return _dbContext.UserAuditLogEntries.Where(u => u.LoggedAt >= start
                && u.LoggedAt < end && u.UserChangeType == UserChangeType.Created).ToList();
        }

        public WeeklyStatisticsModel GetWeeklyStatistics(int lastWeek, int thisWeek, string name)
        {
            decimal diff = (lastWeek == 0 || thisWeek == 0) ? 0 : (Convert.ToDecimal(thisWeek) - Convert.ToDecimal(lastWeek)) * 100 / lastWeek;
            return new WeeklyStatisticsModel
            {
                NoOfItems = thisWeek,
                DiffPercentage = Math.Round(Math.Abs(diff), 1),
                ChangeType = diff == 0 ? lastWeek == thisWeek ? StatisticsChangeType.Unchanged : lastWeek == 0 ? StatisticsChangeType.NA_NoDataLastWeek : StatisticsChangeType.NA_NoDataThisWeek : thisWeek > lastWeek ? StatisticsChangeType.Increasing : StatisticsChangeType.Decreasing,
                Name = name
            };
        }

        #endregion

        #region Dashboard Order Statistics

        public IEnumerable<OrderStatisticsModel> GetOrderStatistics()
        {
            IQueryable<Order> orders = _dbContext.Orders;

            yield return GetOrderRegionStatistics(orders);
            yield return GetOrderLanguageStatistics(orders);
            yield return GetOrderCustomerStatistics(orders);
        }

        public OrderStatisticsModel GetOrderRegionStatistics(IQueryable<Order> orders)
        {
            return GetOrderStats("Mest beställda län", orders.GroupBy(o => o.Region.Name));
        }

        public OrderStatisticsModel GetOrderLanguageStatistics(IQueryable<Order> orders)
        {
            return GetOrderStats("Mest beställda språk", orders.GroupBy(o => o.Language.Name));
        }

        public OrderStatisticsModel GetOrderCustomerStatistics(IQueryable<Order> orders)
        {
            return GetOrderStats("Myndigheter", orders.GroupBy(o => o.CustomerOrganisation.Name));
        }

        private OrderStatisticsModel GetOrderStats(string name, IQueryable<IGrouping<string, Order>> orders)
        {
            return new OrderStatisticsModel
            {
                Name = name,
                TotalListItems = orders.OrderByDescending(o => o.Count()).Select(n => new OrderStatisticsListItemModel { Name = n.Key, NoOfItems = n.Count(), PercentageValueToDisplay = Math.Round((double)n.Count() * 100 / orders.Sum(o => o.Count()), 1) })
            };
        }

        public int TotalNoOfOrders => _dbContext.Orders.Count();

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
                      .Include(r => r.Order).ThenInclude(o => o.Requirements).ThenInclude(r => r.RequirementAnswers)
                      .Include(r => r.Order).ThenInclude(o => o.InterpreterLocations)
                      .Include(r => r.Order).ThenInclude(o => o.CompetenceRequirements)
                      .OrderBy(r => r.Order.OrderNumber)
                      .Where(r => r.Ranking.BrokerId == brokerId && r.CreatedAt.Date >= start.Date && r.CreatedAt.Date <= end.Date
                          && !(r.Status == RequestStatus.NoDeadlineFromCustomer || r.Status == RequestStatus.AwaitingDeadlineFromCustomer || r.Status == RequestStatus.InterpreterReplaced));
        }

        public int GetNoOfRequestsForBroker(DateTimeOffset start, DateTimeOffset end, int brokerId)
        {
            return _dbContext.Requests.Where(r => r.Ranking.BrokerId == brokerId
                      && r.CreatedAt.Date >= start.Date && r.CreatedAt.Date <= end.Date
                      && !(r.Status == RequestStatus.NoDeadlineFromCustomer
                      || r.Status == RequestStatus.AwaitingDeadlineFromCustomer
                      || r.Status == RequestStatus.InterpreterReplaced)).Count();
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
                    .Include(r => r.Order).ThenInclude(o => o.Requirements).ThenInclude(r => r.RequirementAnswers)
                    .Include(r => r.Order).ThenInclude(o => o.InterpreterLocations)
                    .Include(r => r.Order).ThenInclude(o => o.CompetenceRequirements)
                    .OrderBy(r => r.Order.OrderNumber)
                    .Where(r => r.Ranking.BrokerId == brokerId &&
                        !(r.Status == RequestStatus.NoDeadlineFromCustomer || r.Status == RequestStatus.AwaitingDeadlineFromCustomer || r.Status == RequestStatus.InterpreterReplaced)
                        && r.Order.EndAt <= _clock.SwedenNow && r.Order.StartAt.Date >= start.Date && r.Order.StartAt.Date <= end.Date
                        && (r.Order.Status == OrderStatus.Delivered || r.Order.Status == OrderStatus.DeliveryAccepted || r.Order.Status == OrderStatus.ResponseAccepted));
        }

        public int GetNoOfDeliveredRequestsForBroker(DateTimeOffset start, DateTimeOffset end, int brokerId)
        {
            return _dbContext.Requests
                    .Where(r => r.Ranking.BrokerId == brokerId &&
                        !(r.Status == RequestStatus.NoDeadlineFromCustomer || r.Status == RequestStatus.AwaitingDeadlineFromCustomer || r.Status == RequestStatus.InterpreterReplaced)
                        && r.Order.EndAt <= _clock.SwedenNow && r.Order.StartAt.Date >= start.Date && r.Order.StartAt.Date <= end.Date
                        && (r.Order.Status == OrderStatus.Delivered || r.Order.Status == OrderStatus.DeliveryAccepted || r.Order.Status == OrderStatus.ResponseAccepted)).Count();
        }

        public IEnumerable<Requisition> GetRequisitionsForBroker(DateTimeOffset start, DateTimeOffset end, int brokerId)
        {
            return _dbContext.Requisitions
                    .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                    .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Region)
                    .Include(r => r.Request).ThenInclude(r => r.Interpreter)
                    .Include(r => r.Request).ThenInclude(r => r.PriceRows)
                    .Include(r => r.MealBreaks)
                    .Include(r => r.CreatedByUser)
                    .Include(r => r.PriceRows)
                    .OrderBy(r => r.Request.Order.OrderNumber)
                    .Where(r => r.CreatedAt.Date >= start.Date && r.CreatedAt.Date <= end.Date
                        && r.Request.Ranking.BrokerId == brokerId && r.ReplacedByRequisitionId == null);
        }

        public int GetNoOfRequisitionsForBroker(DateTimeOffset start, DateTimeOffset end, int brokerId)
        {
            return _dbContext.Requisitions.Where(r => r.CreatedAt.Date >= start.Date && r.CreatedAt.Date <= end.Date
                        && r.Request.Ranking.BrokerId == brokerId && r.ReplacedByRequisitionId == null).Count();
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
                    .Where(c => c.CreatedAt.Date >= start.Date &&
                    c.CreatedAt.Date <= end.Date && c.Request.Ranking.BrokerId == brokerId);
        }

        public int GetNoOfComplaintsForBroker(DateTimeOffset start, DateTimeOffset end, int brokerId)
        {
            return _dbContext.Complaints.Where(c => c.CreatedAt.Date >= start.Date
                && c.CreatedAt.Date <= end.Date && c.Request.Ranking.BrokerId == brokerId).Count();
        }

        #endregion

        #region Customer and SysAdmin reports

        public IEnumerable<Order> GetOrders(DateTimeOffset start, DateTimeOffset end, int? organisationId, IEnumerable<int> localAdminCustomerUnits = null)
        {
            return _dbContext.Orders
                    .Include(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(o => o.Requests).ThenInclude(r => r.Interpreter)
                    .Include(o => o.Requests).ThenInclude(r => r.Requisitions)
                    .Include(o => o.Requests).ThenInclude(r => r.Complaints)
                    .Include(o => o.CustomerOrganisation)
                    .Include(o => o.CustomerUnit)
                    .Include(o => o.Language)
                    .Include(o => o.Region)
                    .Include(o => o.CreatedByUser)
                    .Include(o => o.Requirements).ThenInclude(r => r.RequirementAnswers)
                    .Include(o => o.InterpreterLocations)
                    .Include(o => o.CompetenceRequirements)
                    .OrderBy(o => o.OrderNumber)
                    .Where(o => o.CreatedAt.Date >= start.Date && o.CreatedAt.Date <= end.Date
                        && (organisationId.HasValue ? o.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                        && (localAdminCustomerUnits == null || (o.CustomerUnitId.HasValue && localAdminCustomerUnits.Contains(o.CustomerUnitId.Value))));
        }

        public int GetNoOfOrders(DateTimeOffset start, DateTimeOffset end, int? organisationId, IEnumerable<int> localAdminCustomerUnits = null)
        {
            return _dbContext.Orders.Where(o => o.CreatedAt.Date >= start.Date && o.CreatedAt.Date <= end.Date
                        && (organisationId.HasValue ? o.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                        && (localAdminCustomerUnits == null || (o.CustomerUnitId.HasValue && localAdminCustomerUnits.Contains(o.CustomerUnitId.Value)))).Count();
        }

        public IEnumerable<Order> GetDeliveredOrders(DateTimeOffset start, DateTimeOffset end, int? organisationId, IEnumerable<int> localAdminCustomerUnits = null)
        {
            return _dbContext.Orders
                    .Include(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(o => o.Requests).ThenInclude(r => r.Interpreter)
                    .Include(o => o.Requests).ThenInclude(r => r.Requisitions)
                    .Include(o => o.Requests).ThenInclude(r => r.Complaints)
                    .Include(o => o.Requests).ThenInclude(r => r.PriceRows)
                    .Include(o => o.CustomerOrganisation)
                    .Include(o => o.CustomerUnit)
                    .Include(o => o.Language)
                    .Include(o => o.Region)
                    .Include(o => o.CreatedByUser)
                    .Include(o => o.Requirements).ThenInclude(r => r.RequirementAnswers)
                    .Include(o => o.InterpreterLocations)
                    .Include(o => o.CompetenceRequirements)
                    .OrderBy(o => o.OrderNumber)
                    .Where(o => o.EndAt <= _clock.SwedenNow && o.StartAt.Date >= start.Date && o.StartAt.Date <= end.Date
                        && (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.DeliveryAccepted || o.Status == OrderStatus.ResponseAccepted)
                        && (organisationId.HasValue ? o.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                        && (localAdminCustomerUnits == null || (o.CustomerUnitId.HasValue && localAdminCustomerUnits.Contains(o.CustomerUnitId.Value))));
        }

        public int GetNoOfDeliveredOrders(DateTimeOffset start, DateTimeOffset end, int? organisationId, IEnumerable<int> localAdminCustomerUnits = null)
        {
            return _dbContext.Orders.Where(o => o.EndAt <= _clock.SwedenNow && o.StartAt.Date >= start.Date && o.StartAt.Date <= end.Date
                        && (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.DeliveryAccepted || o.Status == OrderStatus.ResponseAccepted)
                        && (organisationId.HasValue ? o.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                        && (localAdminCustomerUnits == null || (o.CustomerUnitId.HasValue && localAdminCustomerUnits.Contains(o.CustomerUnitId.Value)))).Count();
        }

        public IEnumerable<Requisition> GetRequisitionsForCustomerAndSysAdmin(DateTimeOffset start, DateTimeOffset end, int? organisationId, IEnumerable<int> localAdminCustomerUnits = null)
        {
            return _dbContext.Requisitions
                    .Include(r => r.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerUnit)
                    .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                    .Include(r => r.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Region)
                    .Include(r => r.Request).ThenInclude(r => r.Interpreter)
                    .Include(r => r.Request).ThenInclude(r => r.PriceRows)
                    .Include(r => r.MealBreaks)
                    .Include(r => r.ProcessedUser)
                    .Include(r => r.CreatedByUser)
                    .Include(r => r.PriceRows)
                    .OrderBy(r => r.Request.Order.OrderNumber)
                    .Where(r => r.CreatedAt.Date >= start.Date && r.CreatedAt.Date <= end.Date && r.ReplacedByRequisitionId == null
                        && (organisationId.HasValue ? r.Request.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                        && (localAdminCustomerUnits == null || (r.Request.Order.CustomerUnitId.HasValue && localAdminCustomerUnits.Contains(r.Request.Order.CustomerUnitId.Value))));
        }

        public int GetNoOfRequisitionsForCustomerAndSysAdmin(DateTimeOffset start, DateTimeOffset end, int? organisationId, IEnumerable<int> localAdminCustomerUnits = null)
        {
            return _dbContext.Requisitions.Where(r => r.CreatedAt.Date >= start.Date && r.CreatedAt.Date <= end.Date && r.ReplacedByRequisitionId == null
                        && (organisationId.HasValue ? r.Request.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                        && (localAdminCustomerUnits == null || (r.Request.Order.CustomerUnitId.HasValue && localAdminCustomerUnits.Contains(r.Request.Order.CustomerUnitId.Value)))).Count();
        }

        public IEnumerable<Complaint> GetComplaintsForCustomerAndSysAdmin(DateTimeOffset start, DateTimeOffset end, int? organisationId, IEnumerable<int> localAdminCustomerUnits = null)
        {
            return _dbContext.Complaints
                    .Include(c => c.Request).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.CustomerUnit)
                    .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Language)
                    .Include(c => c.Request).ThenInclude(r => r.Order).ThenInclude(o => o.Region)
                    .Include(c => c.Request).ThenInclude(r => r.Interpreter)
                    .Include(c => c.Request).ThenInclude(r => r.Requisitions)
                    .Include(c => c.CreatedByUser)
                    .OrderBy(c => c.Request.Order.OrderNumber)
                    .Where(c => c.CreatedAt.Date >= start.Date && c.CreatedAt.Date <= end.Date
                        && (organisationId.HasValue ? c.Request.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                        && (localAdminCustomerUnits == null || (c.Request.Order.CustomerUnitId.HasValue && localAdminCustomerUnits.Contains(c.Request.Order.CustomerUnitId.Value))));
        }

        public int GetNoOfComplaintsForCustomerAndSysAdmin(DateTimeOffset start, DateTimeOffset end, int? organisationId, IEnumerable<int> localAdminCustomerUnits = null)
        {
            return _dbContext.Complaints.Where(c => c.CreatedAt.Date >= start.Date && c.CreatedAt.Date <= end.Date
                        && (organisationId.HasValue ? c.Request.Order.CustomerOrganisationId == organisationId : !organisationId.HasValue)
                        && (localAdminCustomerUnits == null || (c.Request.Order.CustomerUnitId.HasValue && localAdminCustomerUnits.Contains(c.Request.Order.CustomerUnitId.Value)))).Count();
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
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Inställelsesätt";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.InterpreterLocation);
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Tid för uppdrag";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.AssignmentDate.ToString());
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Myndighetens ärendenummer";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.ReferenceNumber);
                rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Accepterar restid";
                rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.AllowExceedingTravelCost);
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
                        rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Belopp enligt bekräftelse (SEK)";
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
                else if (rows.FirstOrDefault() is ReportOrderRow)
                {
                    CreateColumnsForOrder(rowsWorksheet, (rows as IEnumerable<ReportOrderRow>).Select(r => r), ref columnLetter, reportType);
                }
                switch (EnumHelper.Parent<ReportType, ReportGroup>(reportType))
                {
                    case ReportGroup.SystemAdminReport:
                        CreateColumnsForSystemAdministrator(rowsWorksheet, rows, ref columnLetter);
                        break;
                    case ReportGroup.BrokerReport:
                        CreateColumnsForBroker(rowsWorksheet, rows, ref columnLetter, rows.FirstOrDefault() is ReportOrderRow);
                        break;
                    case ReportGroup.CustomerReport:
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

        private void CreateColumnsForOrder(IXLWorksheet rowsWorksheet, IEnumerable<ReportOrderRow> rows, ref char columnLetter, ReportType reportType)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Dialekt";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.Dialect);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Dialekt är krav";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => string.IsNullOrWhiteSpace(r.Dialect) ? string.Empty : r.DialectIsRequirement ? "Ja" : "Nej");
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Uppfyllt krav/önskemål om dialekt";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => string.IsNullOrWhiteSpace(r.Dialect) ? string.Empty : r.FulfilledDialectRequirement ? "Ja" : "Nej");
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Inställelsesätt 1:a hand";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.OrderedInterpreterLocation1);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Inställelsesätt 2:a hand";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.OrderedInterpreterLocation2);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Inställelsesätt 3:e hand";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.OrderedInterpreterLocation3);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Önskad kompetensnivå 1:a hand";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CompetenceLevelDesired1);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Önskad kompetensnivå 2:a hand";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CompetenceLevelDesired2);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Krav på kompetensnivå";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CompetenceLevelRequired1);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Ytterligare krav på kompetensnivå";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CompetenceLevelRequired2);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Antal övriga krav";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.OrderRequirements);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Antal uppfyllda övriga krav";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.FulfilledOrderRequirements);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Antal övriga önskemål";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.OrderDesiredRequirements);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Antal uppfyllda övriga önskemål";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.FulfilledOrderDesiredRequirements);
        }

        private void CreateColumnsForCustomer(IXLWorksheet rowsWorksheet, IEnumerable<ReportRow> rows, ref char columnLetter, bool isOrder = false)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Enhet";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CustomerUnitName);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Avdelning";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.Department);
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
        }

        private void CreateColumnsForBroker(IXLWorksheet rowsWorksheet, IEnumerable<ReportRow> rows, ref char columnLetter, bool isRequest = false)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Myndighet";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CustomerName);
            if (isRequest)
            {
                CreateColumnsForBrokerRequest(rowsWorksheet, (rows as IEnumerable<ReportOrderRow>).Select(r => r), ref columnLetter);
            }
        }

        private void CreateColumnsForBrokerRequest(IXLWorksheet rowsWorksheet, IEnumerable<ReportOrderRow> rows, ref char columnLetter)
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
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Belopp enligt bekräftelse (SEK)";
            rowsWorksheet.Column(columnLetter.ToString()).Style.NumberFormat.Format = "#,##0.00";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.PreliminaryCost);
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
            if (columnLetter > 'Z')
            {
                char charLevel2 = 'A';

                for (int i = 1; i < columnLetter - 'Z'; ++i)
                {
                    charLevel2++;
                }
                return $"A{charLevel2}{index}";
            }
            return $"{columnLetter}{index}";
        }

        public static IEnumerable<ReportRow> GetOrderExcelFileRows(IEnumerable<Order> listItems, ReportType reportType)
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
                        Department = o.UnitName ?? string.Empty,
                        CustomerUnitName = o.CustomerUnit?.Name ?? string.Empty,
                        HasRequisition = o.Requests.OrderBy(r => r.RequestId).Last().Requisitions.Any(),
                        HasComplaint = o.Requests.OrderBy(r => r.RequestId).Last().Complaints.Any(),
                        CustomerName = o.CustomerOrganisation.Name,
                        Price = o.Requests.OrderBy(r => r.RequestId).Last().PriceRows != null ? o.Requests.OrderBy(r => r.RequestId).Last().PriceRows.Sum(p => p.TotalPrice) : 0,
                        Dialect = o.Requirements.Where(r => r.RequirementType == RequirementType.Dialect).FirstOrDefault()?.Description ?? string.Empty,
                        DialectIsRequirement = o.Requirements.Where(r => r.RequirementType == RequirementType.Dialect).FirstOrDefault()?.IsRequired ?? false,
                        FulfilledDialectRequirement = o.Requirements.Where(r => r.RequirementType == RequirementType.Dialect && r.RequirementAnswers.Any(ra => ra.OrderRequirementId == r.OrderRequirementId && ra.CanSatisfyRequirement)).FirstOrDefault() != null ? true : false,
                        OrderedInterpreterLocation1 = o.InterpreterLocations.Where(i => i.Rank == 1).FirstOrDefault()?.InterpreterLocation.GetDescription() ?? string.Empty,
                        OrderedInterpreterLocation2 = o.InterpreterLocations.Where(i => i.Rank == 2).FirstOrDefault()?.InterpreterLocation.GetDescription() ?? string.Empty,
                        OrderedInterpreterLocation3 = o.InterpreterLocations.Where(i => i.Rank == 3).FirstOrDefault()?.InterpreterLocation.GetDescription() ?? string.Empty,
                        InterpreterLocation = o.Requests.OrderBy(r => r.RequestId).Last().InterpreterLocation.HasValue ? ((InterpreterLocation)o.Requests.OrderBy(r => r.RequestId).Last().InterpreterLocation.Value).GetDescription() : string.Empty,
                        AllowExceedingTravelCost = o.AllowExceedingTravelCost.HasValue ? o.AllowExceedingTravelCost.Value.GetDescription() : string.Empty,
                        CompetenceLevelDesired1 = (o.LanguageHasAuthorizedInterpreter && !o.SpecificCompetenceLevelRequired && o.CompetenceRequirements.Any()) ? o.CompetenceRequirements.Where(c => c.Rank == 1).FirstOrDefault()?.CompetenceLevel.GetDescription() ?? string.Empty : string.Empty,
                        CompetenceLevelDesired2 = (o.LanguageHasAuthorizedInterpreter && !o.SpecificCompetenceLevelRequired && o.CompetenceRequirements.Any()) ? o.CompetenceRequirements.Where(c => c.Rank == 2).FirstOrDefault()?.CompetenceLevel.GetDescription() ?? string.Empty : string.Empty,
                        CompetenceLevelRequired1 = (o.LanguageHasAuthorizedInterpreter && o.SpecificCompetenceLevelRequired && o.CompetenceRequirements.Any()) ? o.CompetenceRequirements.OrderBy(c => c.OrderCompetenceRequirementId).First().CompetenceLevel.GetDescription() : string.Empty,
                        CompetenceLevelRequired2 = (o.LanguageHasAuthorizedInterpreter && o.SpecificCompetenceLevelRequired && o.CompetenceRequirements.Any() && o.CompetenceRequirements.Count() > 1) ? o.CompetenceRequirements.OrderBy(c => c.OrderCompetenceRequirementId).Last().CompetenceLevel.GetDescription() : string.Empty,
                        OrderRequirements = o.Requirements.Where(r => r.RequirementType != RequirementType.Dialect && r.IsRequired).Count(),
                        OrderDesiredRequirements = o.Requirements.Where(r => r.RequirementType != RequirementType.Dialect && !r.IsRequired).Count(),
                        FulfilledOrderDesiredRequirements = o.Requirements.Where(r => r.RequirementType != RequirementType.Dialect && !r.IsRequired && r.RequirementAnswers.Any(ra => ra.OrderRequirementId == r.OrderRequirementId && ra.CanSatisfyRequirement)).Count()
                    });
        }

        public static IEnumerable<ReportRow> GetRequestExcelFileRows(IEnumerable<Request> listItems, ReportType reportType)
        {
            return listItems
                    .Select(r => new ReportOrderRow
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
                        Dialect = r.Order.Requirements.Where(req => req.RequirementType == RequirementType.Dialect).FirstOrDefault()?.Description ?? string.Empty,
                        DialectIsRequirement = r.Order.Requirements.Where(req => req.RequirementType == RequirementType.Dialect).FirstOrDefault()?.IsRequired ?? false,
                        FulfilledDialectRequirement = r.Order.Requirements.Where(req => req.RequirementType == RequirementType.Dialect && req.RequirementAnswers.Any(ra => ra.OrderRequirementId == req.OrderRequirementId && ra.CanSatisfyRequirement)).FirstOrDefault() != null ? true : false,
                        OrderedInterpreterLocation1 = r.Order.InterpreterLocations.Where(i => i.Rank == 1).FirstOrDefault()?.InterpreterLocation.GetDescription() ?? string.Empty,
                        OrderedInterpreterLocation2 = r.Order.InterpreterLocations.Where(i => i.Rank == 2).FirstOrDefault()?.InterpreterLocation.GetDescription() ?? string.Empty,
                        OrderedInterpreterLocation3 = r.Order.InterpreterLocations.Where(i => i.Rank == 3).FirstOrDefault()?.InterpreterLocation.GetDescription() ?? string.Empty,
                        InterpreterLocation = r.InterpreterLocation.HasValue ? ((InterpreterLocation)r.InterpreterLocation.Value).GetDescription() : string.Empty,
                        AllowExceedingTravelCost = r.Order.AllowExceedingTravelCost.HasValue ? EnumHelper.Parent<AllowExceedingTravelCost, TrueFalse>(r.Order.AllowExceedingTravelCost.Value).GetDescription() : string.Empty,
                        CompetenceLevelDesired1 = (r.Order.LanguageHasAuthorizedInterpreter && !r.Order.SpecificCompetenceLevelRequired && r.Order.CompetenceRequirements.Any()) ? r.Order.CompetenceRequirements.Where(c => c.Rank == 1).FirstOrDefault()?.CompetenceLevel.GetDescription() ?? string.Empty : string.Empty,
                        CompetenceLevelDesired2 = (r.Order.LanguageHasAuthorizedInterpreter && !r.Order.SpecificCompetenceLevelRequired && r.Order.CompetenceRequirements.Any()) ? r.Order.CompetenceRequirements.Where(c => c.Rank == 2).FirstOrDefault()?.CompetenceLevel.GetDescription() ?? string.Empty : string.Empty,
                        CompetenceLevelRequired1 = (r.Order.LanguageHasAuthorizedInterpreter && r.Order.SpecificCompetenceLevelRequired && r.Order.CompetenceRequirements.Any()) ? r.Order.CompetenceRequirements.OrderBy(c => c.OrderCompetenceRequirementId).First().CompetenceLevel.GetDescription() : string.Empty,
                        CompetenceLevelRequired2 = (r.Order.LanguageHasAuthorizedInterpreter && r.Order.SpecificCompetenceLevelRequired && r.Order.CompetenceRequirements.Any() && r.Order.CompetenceRequirements.Count() > 1) ? r.Order.CompetenceRequirements.OrderBy(c => c.OrderCompetenceRequirementId).Last().CompetenceLevel.GetDescription() : string.Empty,
                        OrderRequirements = r.Order.Requirements.Where(req => req.RequirementType != RequirementType.Dialect && req.IsRequired).Count(),
                        OrderDesiredRequirements = r.Order.Requirements.Where(req => req.RequirementType != RequirementType.Dialect && !req.IsRequired).Count(),
                        FulfilledOrderDesiredRequirements = r.Order.Requirements.Where(req => req.RequirementType != RequirementType.Dialect && !req.IsRequired && req.RequirementAnswers.Any(ra => ra.OrderRequirementId == req.OrderRequirementId && ra.CanSatisfyRequirement)).Count()
                    });
        }

        public static IEnumerable<ReportRequisitionRow> GetRequisitionsExcelFileRows(IEnumerable<Requisition> listItems, ReportType reportType)
        {
            var isBroker = EnumHelper.Parent<ReportType, ReportGroup>(reportType) == ReportGroup.BrokerReport;

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
                        Department = r.Request.Order.UnitName ?? string.Empty,
                        CustomerUnitName = r.Request.Order.CustomerUnit?.Name ?? string.Empty,
                        TaxCard = r.InterpretersTaxCard == null ? string.Empty : r.InterpretersTaxCard.Value.GetDescription(),
                        ReferenceNumber = r.Request.Order.CustomerReferenceNumber ?? string.Empty,
                        PreliminaryCost = r.Request.PriceRows.Sum(p => p.TotalPrice),
                        InterpreterLocation = ((InterpreterLocation)r.Request.InterpreterLocation.Value).GetDescription(),
                        AllowExceedingTravelCost = !r.Request.Order.AllowExceedingTravelCost.HasValue ? string.Empty : isBroker ? EnumHelper.Parent<AllowExceedingTravelCost, TrueFalse>(r.Request.Order.AllowExceedingTravelCost.Value).GetDescription() : r.Request.Order.AllowExceedingTravelCost.Value.GetDescription()
                    });
        }

        public static IEnumerable<ReportComplaintRow> GetComplaintsExcelFileRows(IEnumerable<Complaint> listItems, ReportType reportType)
        {
            var isBroker = EnumHelper.Parent<ReportType, ReportGroup>(reportType) == ReportGroup.BrokerReport;

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
                        CustomerUnitName = c.Request.Order.CustomerUnit?.Name ?? string.Empty,
                        Department = c.Request.Order.UnitName ?? string.Empty,
                        ReferenceNumber = c.Request.Order.CustomerReferenceNumber ?? string.Empty,
                        InterpreterLocation = ((InterpreterLocation)c.Request.InterpreterLocation.Value).GetDescription(),
                        AllowExceedingTravelCost = !c.Request.Order.AllowExceedingTravelCost.HasValue ? string.Empty : isBroker ? EnumHelper.Parent<AllowExceedingTravelCost, TrueFalse>(c.Request.Order.AllowExceedingTravelCost.Value).GetDescription() : c.Request.Order.AllowExceedingTravelCost.Value.GetDescription()
                    });
        }

        #endregion
    }
}
