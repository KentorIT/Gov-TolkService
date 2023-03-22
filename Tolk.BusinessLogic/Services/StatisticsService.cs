using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

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
            return GetWeeklyStatistics(orders.Where(o => o < breakDate).Count(), orders.Where(o => o >= breakDate).Count(), "Bokningar");
        }

        public WeeklyStatisticsModel GetWeeklyDeliveredOrderStatistics(DateTimeOffset breakDate)
        {
            var deliveredOrders = GetDeliveredOrders();
            return GetWeeklyStatistics(
                deliveredOrders.Where(o => o.CalculatedEndAt >= StartDate && o.CalculatedEndAt < breakDate).Count(),
                deliveredOrders.Where(o => o.CalculatedEndAt >= breakDate && o.CalculatedEndAt < _clock.SwedenNow).Count(), "Utförda uppdrag");
        }

        public WeeklyStatisticsModel GetWeeklyRequisitionStatistics(DateTimeOffset breakDate)
        {
            var requisitions = GetRequisitions(StartDate, _clock.SwedenNow);
            return GetWeeklyStatistics(requisitions.Where(r => r < breakDate).Count(), requisitions.Where(r => r >= breakDate).Count(), "Rekvisitioner");
        }

        public WeeklyStatisticsModel GetWeeklyComplaintStatistics(DateTimeOffset breakDate)
        {
            var complaints = GetComplaints(StartDate, _clock.SwedenNow);
            return GetWeeklyStatistics(complaints.Where(c => c < breakDate).Count(), complaints.Where(c => c >= breakDate).Count(), "Reklamationer");
        }

        public WeeklyStatisticsModel GetWeeklyUserLogins(DateTimeOffset breakDate)
        {
            var userLogins = GetUserLogins(StartDate, _clock.SwedenNow);
            int lastWeek = userLogins.Where(u => u.LoggedAt < breakDate).Select(u => u.UserId).Distinct().Count();
            int thisWeek = userLogins.Where(u => u.LoggedAt >= breakDate).Select(u => u.UserId).Distinct().Count();
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

        private List<DateTimeOffset> GetOrders(DateTimeOffset start, DateTimeOffset end)
            => _dbContext.Orders.Where(o => o.CreatedAt >= start && o.CreatedAt < end).Select(o => o.CreatedAt).ToList();


        private List<DeliveryEndAtDto> GetDeliveredOrders()
            => _dbContext.Requests.Where(r => (r.Status == RequestStatus.Delivered || r.Status == RequestStatus.Approved))
            .Select(r => new DeliveryEndAtDto
            {
                EndAt = r.Order.EndAt,
                RespondedStartAt = r.RespondedStartAt,
                ExpectedLength = r.Order.ExpectedLength
            }).ToList();

        private List<DateTimeOffset> GetRequisitions(DateTimeOffset start, DateTimeOffset end)
            => _dbContext.Requisitions.Where(r => r.CreatedAt >= start && r.CreatedAt < end
                    && !r.ReplacedByRequisitionId.HasValue).Select(r => r.CreatedAt).ToList();

        private List<DateTimeOffset> GetComplaints(DateTimeOffset start, DateTimeOffset end)
            => _dbContext.Complaints.Where(c => c.CreatedAt >= start && c.CreatedAt < end).Select(c => c.CreatedAt).ToList();

        private List<UserLoginDto> GetUserLogins(DateTimeOffset start, DateTimeOffset end)
            => _dbContext.UserLoginLogEntries.Where(u => u.LoggedInAt >= start
                && u.LoggedInAt < end).Select(l => new UserLoginDto { UserId = l.UserId, LoggedAt = l.LoggedInAt }).ToList();

        private List<UserLoginDto> GetNewUsers(DateTimeOffset start, DateTimeOffset end)
            => _dbContext.UserAuditLogEntries.Where(u => u.LoggedAt >= start
                && u.LoggedAt < end && u.UserChangeType == UserChangeType.Created).Select(u => new UserLoginDto { UserId = u.UserId, LoggedAt = u.LoggedAt }).ToList();

        public static WeeklyStatisticsModel GetWeeklyStatistics(int lastWeek, int thisWeek, string name)
        {
            decimal diff = (lastWeek == 0 || thisWeek == 0) ? 0 : (Convert.ToDecimal(thisWeek) - Convert.ToDecimal(lastWeek)) * 100 / lastWeek;
            return new WeeklyStatisticsModel
            {
                NoOfItems = thisWeek,
                DiffPercentage = Math.Round(Math.Abs(diff), 1),
                ChangeType = diff == 0 ? lastWeek == thisWeek ? StatisticsChangeType.Unchanged : lastWeek == 0 ? StatisticsChangeType.NANoDataLastWeek : StatisticsChangeType.NANoDataLastWeek : thisWeek > lastWeek ? StatisticsChangeType.Increasing : StatisticsChangeType.Decreasing,
                Name = name
            };
        }

        #endregion

        #region Dashboard Order Statistics

        public IEnumerable<OrderStatisticsModel> GetOrderStatistics()
        {
            yield return GetOrderRegionStatistics();
            yield return GetOrderLanguageStatistics();
            yield return GetOrderCustomerStatistics();
        }

        public OrderStatisticsModel GetOrderRegionStatistics()
        {
            return GetOrderStats("Mest beställda län", _dbContext.Orders.Select(o => new GroupingDto { OrderId = o.OrderId, Name = o.Region.Name }).ToList().GroupBy(o => o.Name));
        }

        public OrderStatisticsModel GetOrderLanguageStatistics()
        {
            return GetOrderStats("Mest beställda språk", _dbContext.Orders.Select(o => new GroupingDto { OrderId = o.OrderId, Name = o.Language.Name }).ToList().GroupBy(o => o.Name));
        }

        public OrderStatisticsModel GetOrderCustomerStatistics()
        {
            return GetOrderStats("Myndigheter", _dbContext.Orders.Select(o => new GroupingDto { OrderId = o.OrderId, Name = o.CustomerOrganisation.Name }).ToList().GroupBy(o => o.Name));
        }

        private static OrderStatisticsModel GetOrderStats(string name, IEnumerable<IGrouping<string, GroupingDto>> orders)
        {
            return new OrderStatisticsModel
            {
                Name = name,
                TotalListItems = orders
                .OrderByDescending(o => o.Count()).Select(n => new OrderStatisticsListItemModel
                {
                    Name = n.Key,
                    NoOfItems = n.Count(),
                    PercentageValueToDisplay = Math.Round((double)n.Count() * 100 / orders.Sum(o => o.Count()), 1)
                })
            };
        }

        public int TotalNoOfOrders => _dbContext.Orders.Count();

        #endregion

        #region Reports

        public int GetNoOfRequestsForBroker(DateTimeOffset start, DateTimeOffset end, int brokerId)
        {
            return _dbContext.Requests.GetRequestOrdersForBrokerReport(start.Date, end.Date, brokerId).Distinct().Count();
        }

        public ReportOrderModel GetDeliveredRequestsForBroker(DateTimeOffset start, DateTimeOffset end, int brokerId)
        {
            var requestOrders = _dbContext.Requests.GetDeliveredRequestsWithOrders(start.Date, end.Date, _clock.SwedenNow.DateTime, null, null, brokerId);
            var orderIds = requestOrders.Select(r => r.OrderId).Distinct().ToList();
            var requisitions = _dbContext.Requisitions.GetRequisitionsForDeliveredOrders(start.Date, end.Date, _clock.SwedenNow.DateTime, null, null, brokerId);
            var complaints = _dbContext.Complaints.GetComplaintsForDeliveredOrders(start.Date, end.Date, _clock.SwedenNow.DateTime, null, null, brokerId);
            var interpreterLocations = _dbContext.OrderInterpreterLocation.GetInterpreterLocationsByOrderIds(orderIds);
            var orderRequirements = _dbContext.OrderRequirements.GetOrderRequirementsByOrderIds(orderIds);
            var orderRequirementAnswers = _dbContext.OrderRequirementRequestAnswer.GetRequirementAnswersForDeliveredOrders(start.Date, end.Date, _clock.SwedenNow.DateTime, null, null, brokerId);
            var competenceRequirements = _dbContext.OrderCompetenceRequirements.GetOrderCompetencesByOrderIds(orderIds);
            var requestPricerows = _dbContext.RequestPriceRows.GetRequestPriceRowsForDeliveredOrders(start.Date, end.Date, _clock.SwedenNow.DateTime, null, null, brokerId);
            return ReportOrderModel.GetModelFromOrders(requestOrders, requisitions, complaints, interpreterLocations, orderRequirements, orderRequirementAnswers, competenceRequirements, requestPricerows, true, true);
        }

        public ReportOrderModel GetRequestsForBroker(DateTimeOffset start, DateTimeOffset end, int brokerId)
        {
            var requestOrders = _dbContext.Requests.GetRequestOrdersForBrokerReport(start.Date, end.Date, brokerId);
            var orderIds = requestOrders.Select(r => r.OrderId).Distinct().ToList();
            var requisitions = _dbContext.Requisitions.GetRequisitionsForBrokerReport(start.Date, end.Date, brokerId);
            var complaints = _dbContext.Complaints.GetComplaintsForBrokerReport(start.Date, end.Date, brokerId);
            var interpreterLocations = _dbContext.OrderInterpreterLocation.GetInterpreterLocationsByOrderIds(orderIds);
            var orderRequirements = _dbContext.OrderRequirements.GetOrderRequirementsByOrderIds(orderIds);
            var orderRequirementAnswers = _dbContext.OrderRequirementRequestAnswer.GetRequirementAnswersForBrokerReport(start.Date, end.Date, brokerId);
            var competenceRequirements = _dbContext.OrderCompetenceRequirements.GetOrderCompetencesByOrderIds(orderIds);
            return ReportOrderModel.GetModelFromOrders(requestOrders, requisitions, complaints, interpreterLocations, orderRequirements, orderRequirementAnswers, competenceRequirements, null, true, false);
        }

        public int GetNoOfRequisitions(DateTimeOffset start, DateTimeOffset end, int? organisationId, IEnumerable<int> localAdminCustomerUnits, int? brokerId)
        {
            return _dbContext.Requisitions.GetRequisitionsForReports(start.Date, end.Date, organisationId, localAdminCustomerUnits, brokerId).Count();
        }

        public int GetNoOfComplaints(DateTimeOffset start, DateTimeOffset end, int? organisationId, IEnumerable<int> localAdminCustomerUnits, int? brokerId)
        {
            return _dbContext.Complaints.GetComplaintsForReports(start.Date, end.Date, organisationId, localAdminCustomerUnits, brokerId).Count();
        }

        public int GetNoOfOrders(DateTimeOffset start, DateTimeOffset end, int? organisationId, IEnumerable<int> localAdminCustomerUnits = null)
        {
            return _dbContext.Requests.GetRequestsOrdersForReport(start.Date, end.Date, organisationId, localAdminCustomerUnits).Select(r => r.OrderId).Distinct().Count();
        }

        public ReportOrderModel GetDeliveredOrders(DateTimeOffset start, DateTimeOffset end, int? organisationId, IEnumerable<int> localAdminCustomerUnits = null)
        {
            var requestOrders = _dbContext.Requests.GetDeliveredRequestsWithOrders(start.Date, end.Date, _clock.SwedenNow.DateTime, organisationId, localAdminCustomerUnits);
            var requisitions = _dbContext.Requisitions.GetRequisitionsForDeliveredOrders(start.Date, end.Date, _clock.SwedenNow.DateTime, organisationId, localAdminCustomerUnits);
            var complaints = _dbContext.Complaints.GetComplaintsForDeliveredOrders(start.Date, end.Date, _clock.SwedenNow.DateTime, organisationId, localAdminCustomerUnits);
            var interpreterLocations = _dbContext.OrderInterpreterLocation.GetInterpreterLocationsForDeliveredOrders(start.Date, end.Date, _clock.SwedenNow.DateTime, organisationId, localAdminCustomerUnits);
            var orderRequirements = _dbContext.OrderRequirements.GetOrderRequirementsForDeliveredOrders(start.Date, end.Date, _clock.SwedenNow.DateTime, organisationId, localAdminCustomerUnits);
            var orderRequirementAnswers = _dbContext.OrderRequirementRequestAnswer.GetRequirementAnswersForDeliveredOrders(start.Date, end.Date, _clock.SwedenNow.DateTime, organisationId, localAdminCustomerUnits);
            var competenceRequirements = _dbContext.OrderCompetenceRequirements.GetOrderCompetencesForDeliveredOrders(start.Date, end.Date, _clock.SwedenNow.DateTime, organisationId, localAdminCustomerUnits);
            var requestPricerows = _dbContext.RequestPriceRows.GetRequestPriceRowsForDeliveredOrders(start.Date, end.Date, _clock.SwedenNow.DateTime, organisationId, localAdminCustomerUnits);
            return ReportOrderModel.GetModelFromOrders(requestOrders, requisitions, complaints, interpreterLocations, orderRequirements, orderRequirementAnswers, competenceRequirements, requestPricerows, false, true);
        }

        public ReportOrderModel GetOrders(DateTimeOffset start, DateTimeOffset end, int? organisationId, IEnumerable<int> localAdminCustomerUnits = null)
        {
            var requestOrders = _dbContext.Requests.GetRequestsOrdersForReport(start.Date, end.Date, organisationId, localAdminCustomerUnits);
            var requisitions = _dbContext.Requisitions.GetRequisitionsForOrdersForReport(start.Date, end.Date, organisationId, localAdminCustomerUnits);
            var complaints = _dbContext.Complaints.GetComplaintsForOrdersForReport(start.Date, end.Date, organisationId, localAdminCustomerUnits);
            var interpreterLocations = _dbContext.OrderInterpreterLocation.GetInterpreterLocationsForOrdersForReport(start.Date, end.Date, organisationId, localAdminCustomerUnits);
            var orderRequirements = _dbContext.OrderRequirements.GetOrderRequirementsForOrdersForReport(start.Date, end.Date, organisationId, localAdminCustomerUnits);
            var orderRequirementAnswers = _dbContext.OrderRequirementRequestAnswer.GetRequirementAnswersForOrdersForReport(start.Date, end.Date, organisationId, localAdminCustomerUnits);
            var competenceRequirements = _dbContext.OrderCompetenceRequirements.GetOrderCompetencesForOrdersForReport(start.Date, end.Date, organisationId, localAdminCustomerUnits);
            return ReportOrderModel.GetModelFromOrders(requestOrders, requisitions, complaints, interpreterLocations, orderRequirements, orderRequirementAnswers, competenceRequirements, null, false, false);
        }

        public int GetNoOfDeliveredOrders(DateTimeOffset start, DateTimeOffset end, int? organisationId, IEnumerable<int> localAdminCustomerUnits = null, int? brokerId = null)
        {
            return _dbContext.Requests.GetDeliveredRequestsWithOrders(start.Date, end.Date, _clock.SwedenNow.DateTime, organisationId, localAdminCustomerUnits, brokerId).Select(r => r.OrderId).Distinct().Count();
        }

        public ReportRequistionModel GetRequisitions(DateTimeOffset start, DateTimeOffset end, int? organisationId, IEnumerable<int> localAdminCustomerUnits = null, int? brokerId = null)
        {
            var requisitions = _dbContext.Requisitions.GetRequisitionsForReports(start.Date, end.Date, organisationId, localAdminCustomerUnits, brokerId);
            var requisitionPricerows = _dbContext.RequisitionPriceRows.GetRequisitionPriceRowsForRequisitionReport(start.Date, end.Date, organisationId, localAdminCustomerUnits, brokerId);
            var mealbreaks = _dbContext.MealBreaks.GetMealBreaksForReport(start.Date, end.Date, organisationId, localAdminCustomerUnits, brokerId);
            var requestIds = requisitions.Select(r => r.RequestId).ToList();
            var requestPricerows = _dbContext.RequestPriceRows.GetRequestPriceRowsForRequisitionReport(requestIds);

            return ReportRequistionModel.GetModelFromRequisitions(requisitions, mealbreaks, requisitionPricerows, requestPricerows, brokerId.HasValue);
        }

        public ReportComplaintModel GetComplaints(DateTimeOffset start, DateTimeOffset end, int? organisationId, IEnumerable<int> localAdminCustomerUnits = null, int? brokerId = null)
        {
            var complaints = _dbContext.Complaints.GetComplaintsForReports(start.Date, end.Date, organisationId, localAdminCustomerUnits, brokerId);
            return ReportComplaintModel.GetModelFromComplaints(complaints, brokerId.HasValue);
        }

        public IEnumerable<ReportRow> GetOrdersByStoredProcedure(DateTimeOffset start, DateTimeOffset end, bool onlyDelivered, int? brokerId, int? userId, int? organisationId)
        {
            var connection = _dbContext.Database.GetDbConnection();
            connection.Open();
            var getOrders = connection.CreateCommand();
            getOrders.CommandText = brokerId.HasValue ?
                    $"EXEC GetOrderRequestsForExcelReport @dateFrom = '{start.Date}', @dateTo = '{end.Date}', @onlyDelivered = '{onlyDelivered}', @brokerId = '{brokerId}'" :
                    organisationId == null ? $"EXEC GetOrdersForExcelReport @dateFrom = '{start.Date}', @dateTo = '{end.Date}', @userId = '{userId}', @onlyDelivered = '{onlyDelivered}'" :
                    $"EXEC GetOrdersForExcelReport @dateFrom = '{start.Date}', @dateTo = '{end.Date}',@userId = '{userId}', @onlyDelivered = '{onlyDelivered}', @customerId = '{organisationId}'";
            using var reader = getOrders.ExecuteReader();
            return ReadReportRows(reader, brokerId.HasValue);
        }

        private List<ReportOrderRow> ReadReportRows(DbDataReader reader, bool isBroker)
        {
            List<ReportOrderRow> orderRows = new List<ReportOrderRow>();
            while (reader.Read())
            {
                ReportOrderRow row = new ReportOrderRow
                {
                    OrderNumber = reader.GetString(reader.GetOrdinal("BokningsId")),
                    ReportDate = reader.GetString(reader.GetOrdinal("Rapportdatum")),
                    Language = reader.GetString(reader.GetOrdinal("Språk")),
                    Region = reader.GetString(reader.GetOrdinal("Län")),
                    AssignmentType = reader.GetString(reader.GetOrdinal("Uppdragstyp")),
                    InterpreterCompetenceLevelAsString = reader.GetString(reader.GetOrdinal("Tolkens kompetensnivå")),
                    InterpreterId = reader.GetString(reader.GetOrdinal("Kammarkollegiets tolknr")),
                    InterpreterLocation = reader.GetString(reader.GetOrdinal("Inställelsesätt")),
                    AssignmentDate = reader.GetString(reader.GetOrdinal("Tid för uppdrag")),
                    ReferenceNumber = reader.GetString(reader.GetOrdinal("Myndighetens ärendenummer")),
                    AllowExceedingTravelCost = reader.GetString(reader.GetOrdinal("Accepterar restid")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    Dialect = reader.GetString(reader.GetOrdinal("Dialekt")),
                    DialectIsRequirementAsString = reader.GetString(reader.GetOrdinal("Dialekt är krav")),
                    FulfilledDialectRequirementAsString = reader.GetString(reader.GetOrdinal("Uppfyllt krav/önskemål om dialekt")),
                    OrderedInterpreterLocation1 = reader.GetString(reader.GetOrdinal("Inställelsesätt 1:a hand")),
                    OrderedInterpreterLocation2 = reader.GetString(reader.GetOrdinal("Inställelsesätt 2:a hand")),
                    OrderedInterpreterLocation3 = reader.GetString(reader.GetOrdinal("Inställelsesätt 3:e hand")),
                    CompetenceLevelDesired1 = reader.GetString(reader.GetOrdinal("Önskad kompetensnivå 1:a hand")),
                    CompetenceLevelDesired2 = reader.GetString(reader.GetOrdinal("Önskad kompetensnivå 2:a hand")),
                    CompetenceLevelRequired1 = reader.GetString(reader.GetOrdinal("Krav på kompetensnivå")),
                    CompetenceLevelRequired2 = reader.GetString(reader.GetOrdinal("Ytterligare krav på kompetensnivå")),
                    OrderRequirements = reader.GetInt32(reader.GetOrdinal("Antal övriga krav")),
                    FulfilledOrderRequirements = reader.GetInt32(reader.GetOrdinal("Antal uppfyllda övriga krav")),
                    OrderDesiredRequirements = reader.GetInt32(reader.GetOrdinal("Antal övriga önskemål")),
                    FulfilledOrderDesiredRequirements = reader.GetInt32(reader.GetOrdinal("Antal uppfyllda övriga önskemål")),
                    BrokerName = reader.GetString(reader.GetOrdinal("Förmedling")),
                    ReportPersonToDisplay = reader.IsDBNull("Rapportperson") ? string.Empty : reader.GetString(reader.GetOrdinal("Rapportperson")),
                    CustomerName = reader.GetString(reader.GetOrdinal("Myndighet")),
                    HasRequisition = reader.GetBoolean(reader.GetOrdinal("Rekvisition finns")),
                    HasComplaint = reader.GetBoolean(reader.GetOrdinal("Reklamation finns")),
                    Price = reader.IsDBNull("Totalt pris") ? 0 : reader.GetDecimal(reader.GetOrdinal("Totalt pris")),
                    CustomerUnitName = isBroker ? string.Empty : reader.GetString(reader.GetOrdinal("Enhet")),
                    Department = isBroker ? string.Empty : reader.GetString(reader.GetOrdinal("Avdelning")),
                    InvoiceReference = isBroker ? string.Empty : reader.GetString(reader.GetOrdinal("Fakturareferens")),
                    OrderCreatorEmail = isBroker ? string.Empty : reader.GetString(reader.GetOrdinal("E-postadress")),
                    AgreementNumber = reader.GetString(reader.GetOrdinal("Avtalsnummer")),
                    FlexiblOrderAsString = reader.GetString(reader.GetOrdinal("Flexibel bokning")),
                };
                orderRows.Add(row);
            }
            return orderRows;
        }

        #endregion

        #region Generate Excel

        public static MemoryStream CreateExcelFile(IEnumerable<ReportRow> rows, ReportType reportType, bool useStoredProcedure)
        {
            using var workbook = new XLWorkbook();
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
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.InterpreterCompetenceLevelAsString ?? r.InterpreterCompetenceLevel.GetDescription());
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Kammarkollegiets tolknr";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.InterpreterId);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Inställelsesätt";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.InterpreterLocation);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Tid för uppdrag";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.AssignmentDate);
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
            else if (rows.FirstOrDefault() is ReportComplaintRow)
            {
                CreateColumnsForComplaint(rowsWorksheet, (rows as IEnumerable<ReportComplaintRow>).Select(r => r), ref columnLetter, reportType);
            }
            else if (rows.FirstOrDefault() is ReportOrderRow)
            {
                CreateColumnsForOrder(rowsWorksheet, (rows as IEnumerable<ReportOrderRow>).Select(r => r), useStoredProcedure, ref columnLetter);
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
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Avtalsnummer";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.AgreementNumber);
            switch (reportType)
            {
                case ReportType.RequestsForBrokers:
                case ReportType.OrdersForCustomer:
                case ReportType.OrdersForSystemAdministrator:
                case ReportType.DeliveredOrdersBrokers:
                case ReportType.DeliveredOrdersCustomer:
                case ReportType.DeliveredOrdersSystemAdministrator:
                    rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Flexibel bokning";
                    rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.FlexiblOrderAsString);
                    break;
            }

            rowsWorksheet.Row(1).Style.Font.Bold = true;
            MemoryStream memoryStream = new();
            workbook.SaveAs(memoryStream);
            memoryStream.Flush();
            memoryStream.Position = 0;
            return memoryStream;
        }

        private static void CreateColumnsForOrder(IXLWorksheet rowsWorksheet, IEnumerable<ReportOrderRow> rows, bool useStoredProcedure, ref char columnLetter)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Dialekt";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.Dialect);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Dialekt är krav";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = useStoredProcedure ? rows.Select(r => r.DialectIsRequirementAsString) : rows.Select(r => string.IsNullOrWhiteSpace(r.Dialect) ? string.Empty : r.DialectIsRequirement ? "Ja" : "Nej");
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Uppfyllt krav/önskemål om dialekt";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = useStoredProcedure ? rows.Select(r => r.FulfilledDialectRequirementAsString) : rows.Select(r => string.IsNullOrWhiteSpace(r.Dialect) ? string.Empty : r.FulfilledDialectRequirement ? "Ja" : "Nej");
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

        private static void CreateColumnsForCustomer(IXLWorksheet rowsWorksheet, IEnumerable<ReportRow> rows, ref char columnLetter, bool isOrder = false)
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

        private static void CreateColumnsForOrderCustomer(IXLWorksheet rowsWorksheet, IEnumerable<ReportOrderRow> rows, ref char columnLetter)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Fakturareferens";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.InvoiceReference);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Beställd av";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.ReportPersonToDisplay);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "E-postadress beställare";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.OrderCreatorEmail);
        }

        private static void CreateColumnsForBroker(IXLWorksheet rowsWorksheet, IEnumerable<ReportRow> rows, ref char columnLetter, bool isRequest = false)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Myndighet";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CustomerName);
            if (isRequest)
            {
                CreateColumnsForBrokerRequest(rowsWorksheet, (rows as IEnumerable<ReportOrderRow>).Select(r => r), ref columnLetter);
            }
        }

        private static void CreateColumnsForBrokerRequest(IXLWorksheet rowsWorksheet, IEnumerable<ReportOrderRow> rows, ref char columnLetter)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Besvarad av";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.ReportPersonToDisplay);
        }

        private static void CreateColumnsForSystemAdministrator(IXLWorksheet rowsWorksheet, IEnumerable<ReportRow> rows, ref char columnLetter)
        {
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Myndighet";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CustomerName);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Förmedling";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.BrokerName);
        }

        private static void CreateColumnsForRequisition(IXLWorksheet rowsWorksheet, IEnumerable<ReportRequisitionRow> rows, ref char columnLetter, ReportType reportType)
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
            rowsWorksheet.Column(columnLetter.ToSwedishString()).Style.NumberFormat.Format = "#,##0.00";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.Outlay);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Bilersättning (km)";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.CarCompensation);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Traktamente";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.PerDiem ?? string.Empty);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Total summa (SEK)";
            rowsWorksheet.Column(columnLetter.ToSwedishString()).Style.NumberFormat.Format = "#,##0.00";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.Price);
            rowsWorksheet.Cell(GetColumnName(columnLetter, 1)).Value = "Belopp enligt bekräftelse (SEK)";
            rowsWorksheet.Column(columnLetter.ToSwedishString()).Style.NumberFormat.Format = "#,##0.00";
            rowsWorksheet.Cell(GetColumnName(columnLetter++, 2)).Value = rows.Select(r => r.PreliminaryCost);
        }

        private static void CreateColumnsForComplaint(IXLWorksheet rowsWorksheet, IEnumerable<ReportComplaintRow> rows, ref char columnLetter, ReportType reportType)
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

        public static IEnumerable<ReportRow> GetOrderExcelFileRows(ReportOrderModel reportOrder, ReportType reportType)
        {
            var isBroker = (reportType == ReportType.DeliveredOrdersBrokers || reportType == ReportType.RequestsForBrokers);
            var selectedData = reportOrder?.OrderRequests;
            if (reportType == ReportType.OrdersForCustomer || reportType == ReportType.OrdersForSystemAdministrator)
            {
                var activeRequestIds = reportOrder.OrderRequests.GroupBy(r => r.OrderId).Select(r => r.Max(c => c.RequestId));
                selectedData = reportOrder.OrderRequests.Where(r => activeRequestIds.Contains(r.RequestId));
            }

            return selectedData
                    .Select(o => new ReportOrderRow
                    {
                        OrderNumber = o.OrderNumber,
                        ReportDate = o.ReportDate,
                        BrokerName = o.BrokerName,
                        Language = o.Language,
                        Region = o.Region,
                        AssignmentType = o.AssignmentType.GetDescription(),
                        InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)o.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        InterpreterId = o.InterpreterId,
                        ReportPersonToDisplay = o.ReportPerson,
                        AssignmentDate = o.AssignmentDate,
                        Status = isBroker ? o.RequestStatus.GetDescription() : o.OrderStatus.GetDescription(),
                        ReferenceNumber = o.ReferenceNumber,
                        Department = o.Department,
                        CustomerUnitName = o.CustomerUnitName,
                        HasRequisition = reportOrder.HasRequisitions.Contains(o.RequestId),
                        HasComplaint = reportOrder.HasComplaints.Contains(o.RequestId),
                        CustomerName = o.CustomerName,
                        OrderCreatorEmail = o.OrderCreatorEmail,
                        InvoiceReference = o.InvoiceReference,
                        Price = reportOrder.Prices?.SingleOrDefault(r => r.RequestId == o.RequestId).Price ?? 0,
                        Dialect = reportOrder.Requirements.Where(r => r.OrderId == o.OrderId && r.RequirementType == RequirementType.Dialect).FirstOrDefault()?.Description ?? string.Empty,
                        DialectIsRequirement = reportOrder.Requirements.Where(r => r.OrderId == o.OrderId && r.RequirementType == RequirementType.Dialect).FirstOrDefault()?.IsRequired ?? false,
                        FulfilledDialectRequirement = reportOrder.Requirements.Where(r => r.OrderId == o.OrderId && r.RequirementType == RequirementType.Dialect && reportOrder.RequirementAnswers.Any(ra => ra.OrderRequirementId == r.OrderRequirementId && ra.CanSatisfyRequirement && o.RequestId == ra.RequestId)).FirstOrDefault() != null,
                        OrderedInterpreterLocation1 = reportOrder.InterpreterLocations.Where(i => i.OrderId == o.OrderId && i.Rank == 1).FirstOrDefault()?.InterpreterLocation.GetDescription() ?? string.Empty,
                        OrderedInterpreterLocation2 = reportOrder.InterpreterLocations.Where(i => i.OrderId == o.OrderId && i.Rank == 2).FirstOrDefault()?.InterpreterLocation.GetDescription() ?? string.Empty,
                        OrderedInterpreterLocation3 = reportOrder.InterpreterLocations.Where(i => i.OrderId == o.OrderId && i.Rank == 3).FirstOrDefault()?.InterpreterLocation.GetDescription() ?? string.Empty,
                        InterpreterLocation = o.InterpreterLocation.HasValue ? ((InterpreterLocation)o.InterpreterLocation.Value).GetDescription() : string.Empty,
                        AllowExceedingTravelCost = o.AllowExceedingTravelCost.HasValue ? isBroker ? EnumHelper.Parent<AllowExceedingTravelCost, TrueFalse>(o.AllowExceedingTravelCost.Value).GetDescription() : o.AllowExceedingTravelCost.Value.GetDescription() : string.Empty,
                        CompetenceLevelDesired1 = (o.LanguageHasAuthorizedInterpreter && !o.SpecificCompetenceLevelRequired && reportOrder.Competences.Any(ro => ro.OrderId == o.OrderId)) ? reportOrder.Competences.Where(c => c.OrderId == o.OrderId && c.Rank == 1).FirstOrDefault()?.CompetenceLevel.GetDescription() ?? string.Empty : string.Empty,
                        CompetenceLevelDesired2 = (o.LanguageHasAuthorizedInterpreter && !o.SpecificCompetenceLevelRequired && reportOrder.Competences.Any(ro => ro.OrderId == o.OrderId)) ? reportOrder.Competences.Where(c => c.OrderId == o.OrderId && c.Rank == 2).FirstOrDefault()?.CompetenceLevel.GetDescription() ?? string.Empty : string.Empty,
                        CompetenceLevelRequired1 = (o.LanguageHasAuthorizedInterpreter && o.SpecificCompetenceLevelRequired && reportOrder.Competences.Any(ro => ro.OrderId == o.OrderId)) ? reportOrder.Competences.Where(ro => ro.OrderId == o.OrderId).First().CompetenceLevel.GetDescription() : string.Empty,
                        CompetenceLevelRequired2 = (o.LanguageHasAuthorizedInterpreter && o.SpecificCompetenceLevelRequired && reportOrder.Competences.Any(ro => ro.OrderId == o.OrderId) && reportOrder.Competences.Where(ro => ro.OrderId == o.OrderId).Count() > 1) ? reportOrder.Competences.Where(ro => ro.OrderId == o.OrderId).Last().CompetenceLevel.GetDescription() : string.Empty,
                        OrderRequirements = reportOrder.Requirements.Where(r => r.OrderId == o.OrderId && r.RequirementType != RequirementType.Dialect && r.IsRequired).Count(),
                        OrderDesiredRequirements = reportOrder.Requirements.Where(r => r.OrderId == o.OrderId && r.RequirementType != RequirementType.Dialect && !r.IsRequired).Count(),
                        FulfilledOrderDesiredRequirements = reportOrder.Requirements.Where(r => r.RequirementType != RequirementType.Dialect && !r.IsRequired && reportOrder.RequirementAnswers.Any(ra => ra.OrderRequirementId == r.OrderRequirementId && ra.CanSatisfyRequirement && ra.RequestId == o.RequestId)).Count(),
                        FulfilledOrderRequirements = reportOrder.Requirements.Where(r => r.RequirementType != RequirementType.Dialect && r.IsRequired && reportOrder.RequirementAnswers.Any(ra => ra.OrderRequirementId == r.OrderRequirementId && ra.CanSatisfyRequirement && ra.RequestId == o.RequestId)).Count(),
                        AgreementNumber = o.AgreementNumber
                    }).ToList().OrderBy(row => row.OrderNumber);
        }

        public static IEnumerable<ReportRequisitionRow> GetRequisitionsExcelFileRows(ReportRequistionModel requistionModel, ReportType reportType)
        {
            var isBroker = EnumHelper.Parent<ReportType, ReportGroup>(reportType) == ReportGroup.BrokerReport;
            NullCheckHelper.ArgumentCheckNull(requistionModel, nameof(GetRequisitionsExcelFileRows), nameof(StatisticsService));
            return requistionModel.Requisitions
                    .Select(r => new ReportRequisitionRow
                    {
                        OrderNumber = r.OrderNumber,
                        ReportDate = r.ReportDate,
                        BrokerName = r.BrokerName,
                        Language = r.Language,
                        Region = r.Region,
                        InterpreterId = r.InterpreterId,
                        ReportPersonToDisplay = r.ReportPerson,
                        AssignmentDate = r.AssignmentDate,
                        Status = r.RequisitionStatus.GetDescription(),
                        CustomerName = r.CustomerName,
                        AssignmentType = r.AssignmentType.GetDescription(),
                        InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)r.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        HasMealbreaks = requistionModel.HasMealbreaks.Contains(r.RequisitionId),
                        WaisteTime = r.WaisteTime,
                        WaisteTimeIWH = r.WaisteTimeIWH,
                        Price = requistionModel.RequisitionPrices?.SingleOrDefault(pr => pr.RequisitionId == r.RequisitionId).Price ?? 0,
                        Outlay = requistionModel.RequisitionPrices?.SingleOrDefault(pr => pr.RequisitionId == r.RequisitionId).Outlay ?? 0,
                        PreliminaryCost = requistionModel.RequestPrices?.SingleOrDefault(pr => pr.RequestId == r.RequestId).Price ?? 0,
                        CarCompensation = r.CarCompensation,
                        PerDiem = r.PerDiem,
                        Department = r.Department,
                        CustomerUnitName = r.CustomerUnitName,
                        TaxCard = r.TaxCard.HasValue ? r.TaxCard.Value.GetDescription() : string.Empty,
                        ReferenceNumber = r.ReferenceNumber,
                        InterpreterLocation = r.InterpreterLocation.HasValue ? ((InterpreterLocation)r.InterpreterLocation.Value).GetDescription() : string.Empty,
                        AllowExceedingTravelCost = r.AllowExceedingTravelCost.HasValue ? isBroker ? EnumHelper.Parent<AllowExceedingTravelCost, TrueFalse>(r.AllowExceedingTravelCost.Value).GetDescription() : r.AllowExceedingTravelCost.Value.GetDescription() : string.Empty,
                        AgreementNumber = r.AgreementNumber
                    }).ToList().OrderBy(row => row.OrderNumber);
        }

        public static IEnumerable<ReportComplaintRow> GetComplaintsExcelFileRows(ReportComplaintModel complaintModel, ReportType reportType)
        {
            var isBroker = EnumHelper.Parent<ReportType, ReportGroup>(reportType) == ReportGroup.BrokerReport;
            NullCheckHelper.ArgumentCheckNull(complaintModel, nameof(GetComplaintsExcelFileRows), nameof(StatisticsService));
            return complaintModel.Complaints
                    .Select(c => new ReportComplaintRow
                    {
                        OrderNumber = c.OrderNumber,
                        ReportDate = c.ReportDate,
                        BrokerName = c.BrokerName,
                        Language = c.Language,
                        Region = c.Region,
                        Status = c.ComplaintStatus.GetDescription(),
                        InterpreterId = c.InterpreterId,
                        AssignmentDate = c.AssignmentDate,
                        CustomerName = c.CustomerName,
                        AssignmentType = c.AssignmentType.GetDescription(),
                        InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)c.CompetenceLevel ?? CompetenceAndSpecialistLevel.NoInterpreter,
                        ComplaintType = c.ComplaintType.GetDescription(),
                        CustomerUnitName = c.CustomerUnitName,
                        ReportPersonToDisplay = c.ReportPerson,
                        Department = c.Department,
                        ReferenceNumber = c.ReferenceNumber,
                        InterpreterLocation = c.InterpreterLocation.HasValue ? ((InterpreterLocation)c.InterpreterLocation.Value).GetDescription() : string.Empty,
                        AllowExceedingTravelCost = c.AllowExceedingTravelCost.HasValue ? isBroker ? EnumHelper.Parent<AllowExceedingTravelCost, TrueFalse>(c.AllowExceedingTravelCost.Value).GetDescription() : c.AllowExceedingTravelCost.Value.GetDescription() : string.Empty,
                        AgreementNumber = c.AgreementNumber
                    }).ToList().OrderBy(row => row.OrderNumber);
        }

        #endregion
    }
}